namespace FieldKit
{
    public sealed partial class Plugin
    {
        private BotSpawner GetEntityBotSpawner(
            out BotsController controller)
        {
            controller = null;
            try
            {
                IBotGame botGame =
                    Singleton<IBotGame>.Instance;
                controller =
                    botGame == null
                        ? null
                        : botGame.BotsController;
                return controller == null
                    ? null
                    : controller.GetSpawner();
            }
            catch
            {
                return null;
            }
        }

        private void RefreshSpawnableAiCatalog(bool force = false)
        {
            if (!force &&
                Time.unscaledTime <
                    _nextSpawnEntityCatalogRefresh)
                return;

            _nextSpawnEntityCatalogRefresh =
                Time.unscaledTime + 2f;
            _spawnableAiEntries.Clear();

            BotsController controller;
            BotSpawner spawner =
                GetEntityBotSpawner(out controller);
            if (spawner == null || controller == null)
                return;

            string botTypesPath = System.IO.Path.Combine(
                BepInEx.Paths.GameRootPath,
                "SPT",
                "SPT_Data",
                "database",
                "bots",
                "types");
            HashSet<string> serverTypes =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
            try
            {
                if (System.IO.Directory.Exists(botTypesPath))
                {
                    string[] files =
                        System.IO.Directory.GetFiles(
                            botTypesPath,
                            "*.json");
                    for (int i = 0; i < files.Length; i++)
                    {
                        serverTypes.Add(
                            System.IO.Path.GetFileNameWithoutExtension(
                                files[i]));
                    }
                }
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not read SPT bot types: " +
                    exception.Message);
            }

            foreach (string serverType in serverTypes)
            {
                WildSpawnType role;
                if (!Enum.TryParse(
                        serverType,
                        true,
                        out role) ||
                    IsUnsafeDebugSpawnRole(role))
                    continue;

                _spawnableAiEntries.Add(
                    new SpawnableAiEntry
                    {
                        Role = role,
                        Name = role.ToString(),
                        Group = SpawnRoleGroupName(role)
                    });
            }

            _spawnableAiEntries.Sort(
                (left, right) =>
                {
                    int group = string.Compare(
                        left.Group,
                        right.Group,
                        StringComparison.OrdinalIgnoreCase);
                    return group != 0
                        ? group
                        : string.Compare(
                            left.Name,
                            right.Name,
                            StringComparison.OrdinalIgnoreCase);
                });
        }

        private static bool IsUnsafeDebugSpawnRole(
            WildSpawnType role)
        {
            return role == WildSpawnType.test ||
                   role == WildSpawnType.bossTest ||
                   role == WildSpawnType.followerTest ||
                   role == WildSpawnType.shooterBTR;
        }

        private static string SpawnRoleGroupName(
            WildSpawnType role)
        {
            string name = role.ToString();
            if (IsOrdinaryScavRole(role))
                return "Scav";
            if (name.StartsWith("boss", StringComparison.OrdinalIgnoreCase))
                return "Boss";
            if (name.StartsWith("follower", StringComparison.OrdinalIgnoreCase))
                return "Follower";
            if (name.StartsWith("sect", StringComparison.OrdinalIgnoreCase))
                return "Cultist";
            if (name.StartsWith("infected", StringComparison.OrdinalIgnoreCase))
                return "Infected";
            if (name.IndexOf("pmc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                role == WildSpawnType.exUsec)
                return "Raider / Rogue";
            return "Special";
        }

        private bool TryGetRequestedEntitySpawnPosition(
            out Vector3 position)
        {
            position = Vector3.zero;
            if (_camera == null || _localPlayer == null)
                return false;

            Ray ray = _camera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                EntityGroundHits,
                500f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = EntityGroundHits[i];
                Collider collider = hit.collider;
                if (collider == null ||
                    hit.distance <= 0.05f ||
                    collider.GetComponentInParent<Player>() != null)
                    continue;

                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    position = hit.point;
                }
            }
            Array.Clear(EntityGroundHits, 0, hitCount);

            if (nearest < float.MaxValue)
                return true;

            Vector3 forward = _localPlayer.Transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            position =
                _localPlayer.Position +
                forward.normalized * 3f;
            return true;
        }

        private static bool TryGetNearbyEntityNavMeshPosition(
            Vector3 requestedPosition,
            out Vector3 navMeshPosition)
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(
                    requestedPosition,
                    out hit,
                    12f,
                    UnityEngine.AI.NavMesh.AllAreas))
            {
                navMeshPosition = hit.position;
                return true;
            }

            navMeshPosition = requestedPosition;
            return false;
        }

        private async void SpawnEntityAtLocation(
            SpawnableAiEntry entry)
        {
            if (entry == null || _spawnEntityInProgress)
                return;

            Vector3 requestedPosition;
            if (!TryGetRequestedEntitySpawnPosition(
                    out requestedPosition))
            {
                _spawnEntityStatus =
                    "A raid and active camera are required.";
                return;
            }

            BotsController controller;
            BotSpawner spawner =
                GetEntityBotSpawner(out controller);
            if (spawner == null ||
                controller == null ||
                spawner.GameEnd ||
                !spawner.IsProfilesLoaded)
            {
                _spawnEntityStatus =
                    "The raid bot spawner is not ready.";
                return;
            }

            bool aiDisabled = _spawnEntityAiDisabled;
            bool ignoreNavMesh = _spawnEntityIgnoreNavMesh;
            Vector3 nearbyNavMeshPosition;
            bool hasNearbyNavMesh =
                TryGetNearbyEntityNavMeshPosition(
                    requestedPosition,
                    out nearbyNavMeshPosition);
            Vector3 spawnPosition =
                hasNearbyNavMesh
                    ? nearbyNavMeshPosition
                    : requestedPosition;
            if (!ignoreNavMesh && !hasNearbyNavMesh)
            {
                _spawnEntityStatus =
                    "No NavMesh was found within 12m of the aimed location.";
                return;
            }
            if (ignoreNavMesh)
            {
                spawnPosition = requestedPosition;
                bool safelyOnNavMesh =
                    hasNearbyNavMesh &&
                    Vector3.Distance(
                        requestedPosition,
                        nearbyNavMeshPosition) <= 0.35f;
                if (!safelyOnNavMesh && !aiDisabled)
                {
                    aiDisabled = true;
                    LogSource.LogWarning(
                        "Forced off-mesh entity spawn will start with AI disabled to prevent EFT relocating it.");
                }
            }
            int generation = ++_entitySpawnGeneration;
            _lastSpawnedEntityBot = null;
            CancellationTokenSource cancellation =
                new CancellationTokenSource();
            _entitySpawnCancellation = cancellation;

            _spawnEntityInProgress = true;
            _spawnEntityStatus =
                "Requesting a natural " + entry.Name +
                " profile from SPT...";

            bool capacityReserved = false;
            try
            {
                BotSpawnParams spawnParams =
                    new BotSpawnParams
                    {
                        Id_spawn =
                            "fieldkit-" +
                            Guid.NewGuid().ToString("N")
                    };
                List<Profile> profiles =
                    await RequestSpawnEntityProfiles(
                        entry.Role,
                        BotDifficulty.normal,
                        generation,
                        cancellation.Token);
                if (generation != _entitySpawnGeneration)
                    return;
                Profile profile =
                    profiles == null
                        ? null
                        : profiles.FirstOrDefault();
                if (profile == null)
                    throw new InvalidOperationException(
                        "SPT did not generate a bot profile.");
                if (profile.Info == null ||
                    profile.Info.Settings == null)
                    throw new InvalidOperationException(
                        "SPT generated a profile without bot settings.");
                EPlayerSide generatedSide =
                    profile.Info.Side;
                WildSpawnType generatedRole =
                    profile.Info.Settings.Role;
                BotDifficulty generatedDifficulty =
                    profile.Info.Settings.BotDifficulty;
                BotProfileDataClass profileData =
                    new BotProfileDataClass(
                        generatedSide,
                        generatedRole,
                        generatedDifficulty,
                        5f,
                        spawnParams,
                        false);
                PoolManagerClass poolManager =
                    Singleton<PoolManagerClass>.Instance;
                if (poolManager == null)
                    throw new InvalidOperationException(
                        "EFT's runtime asset pool is unavailable.");

                EFT.ResourceKey[] resourceKeys =
                    profile.GetAllPrefabPaths(false)
                        .Where(resource =>
                            resource !=
                            EFT.ResourceKey.EmptyResourceKey)
                        .Distinct()
                        .ToArray();
                LogSource.LogInfo(
                    "Prepared FieldKit profile " + profile.Id +
                    ": side=" + generatedSide +
                    ", role=" + generatedRole +
                    ", difficulty=" + generatedDifficulty +
                    ", resources=" + resourceKeys.Length + ".");
                _spawnEntityStatus =
                    "Loading " + entry.Name +
                    " runtime assets (" +
                    resourceKeys.Length + ")...";

                EFT.ResourceKey[] resourcesToExpand =
                    GetUncachedEntityResources(resourceKeys);
                if (resourcesToExpand.Length > 0)
                {
                    _spawnEntityStatus =
                        "Loading " + entry.Name +
                        " resources (" +
                        resourcesToExpand.Length + " new, " +
                        (resourceKeys.Length -
                         resourcesToExpand.Length) +
                        " cached)...";
                    await LoadSingleBotResourcePools(
                        poolManager,
                        resourcesToExpand,
                        cancellation.Token);
                    CacheExpandedEntityResources(
                        resourcesToExpand);
                }
                else
                {
                    _spawnEntityStatus =
                        "Using cached " + entry.Name +
                        " resources...";
                    LogSource.LogInfo(
                        "All " + resourceKeys.Length +
                        " resources are warm for " +
                        profile.Id + ".");
                }
                cancellation.Token.ThrowIfCancellationRequested();
                if (generation != _entitySpawnGeneration)
                    return;

                BotCreationDataClass creationData =
                    BotCreationDataClass.CreateWithoutProfile(
                        profileData);
                creationData.AddProfile(profile);
                LogSource.LogInfo(
                    "EFT finished runtime assets for " +
                    profile.Id + "; entering balanced activation.");
                BotZone zone;
                EFT.Game.Spawning.ISpawnPoint spawnPoint;
                if (!TrySelectEntitySpawnPoint(
                        spawner,
                        controller,
                        creationData,
                        spawnPosition,
                        out zone,
                        out spawnPoint))
                    throw new InvalidOperationException(
                        "No compatible EFT AI zone has a spawn marker.");

                TaskCompletionSource<BotOwner> createdSource =
                    new TaskCompletionSource<BotOwner>();
                Action<BotOwner> createdCallback = botOwner =>
                {
                    createdSource.TrySetResult(botOwner);
                };
                _spawnEntityStatus =
                    "Activating " + entry.Name +
                    " at the requested location...";

                creationData.AddPosition(
                    spawnPosition,
                    spawnPoint.CorePointId);

                // This mirrors BotSpawner.DebugSpawnAnyway: one reservation
                // enters method_10 and method_11 releases it after complete
                // BotOwner registration. MaxBots receives one temporary slot
                // so scheduled raid waves keep their original capacity.
                ReserveFieldKitSpawnCapacity(spawner);
                capacityReserved = true;
                spawner.InSpawnProcess++;
                try
                {
                    spawner.method_10(
                        zone,
                        creationData,
                        createdCallback,
                        spawner.GetCancelToken());
                }
                catch
                {
                    spawner.InSpawnProcess =
                        Math.Max(0, spawner.InSpawnProcess - 1);
                    throw;
                }

                Task activationCancelled =
                    Task.Delay(
                        Timeout.Infinite,
                        cancellation.Token);
                Task activationCompleted =
                    await Task.WhenAny(
                        createdSource.Task,
                        activationCancelled);
                cancellation.Token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(
                        activationCompleted,
                        createdSource.Task))
                    throw new OperationCanceledException(
                        cancellation.Token);

                BotOwner spawnedBot = await createdSource.Task;
                if (generation != _entitySpawnGeneration)
                    return;
                if (spawnedBot == null)
                    throw new InvalidOperationException(
                        "EFT activation returned no bot.");

                RegisterFieldKitSpawn(
                    spawner,
                    spawnedBot,
                    resourceKeys);
                _lastSpawnedEntityBot = spawnedBot;
                _lastSpawnedEntitySerial++;
                capacityReserved = false;
                StartCoroutine(
                    FinalizeSpawnedEntity(
                        spawnedBot,
                        controller,
                        spawnPosition,
                        entry.Name,
                        aiDisabled,
                        ignoreNavMesh));
            }
            catch (OperationCanceledException)
            {
                _spawnEntityStatus =
                    "Spawn cancelled because the raid ended.";
                _spawnEntityInProgress = false;
            }
            catch (Exception exception)
            {
                _spawnEntityInProgress = false;
                _spawnEntityStatus =
                    "Spawn request failed: " +
                    GetSpawnExceptionMessage(exception);
                LogSource.LogWarning(
                    "Entity spawn request failed: " +
                    exception);
            }
            finally
            {
                if (capacityReserved)
                    ReleaseFieldKitSpawnCapacity(spawner);
                if (ReferenceEquals(
                        _entitySpawnCancellation,
                        cancellation))
                    _entitySpawnCancellation = null;
                cancellation.Dispose();
            }
        }

        private static async Task LoadSingleBotResourcePools(
            PoolManagerClass poolManager,
            ICollection<EFT.ResourceKey> resourceKeys,
            CancellationToken cancellationToken)
        {
            const BindingFlags InstanceMethods =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            MethodInfo getPoolsMethod =
                typeof(PoolManagerClass).GetMethod(
                    "method_0",
                    InstanceMethods,
                    null,
                    new[]
                    {
                        typeof(PoolManagerClass.PoolsCategory)
                    },
                    null);
            if (getPoolsMethod == null)
                throw new MissingMethodException(
                    "EFT pool-category accessor was not found.");

            object pools = getPoolsMethod.Invoke(
                poolManager,
                new object[]
                {
                    PoolManagerClass.PoolsCategory.Raid
                });
            if (pools == null)
                throw new InvalidOperationException(
                    "EFT's raid resource pool is unavailable.");

            MethodInfo convertMethod =
                pools.GetType().GetMethod(
                    "ConvertResourceInfo",
                    InstanceMethods);
            if (convertMethod == null)
                throw new MissingMethodException(
                    "EFT resource conversion method was not found.");

            object converted = convertMethod.Invoke(
                pools,
                new object[]
                {
                    resourceKeys
                });
            System.Collections.IList resourceInfo =
                converted as System.Collections.IList;
            if (resourceInfo == null)
                throw new InvalidOperationException(
                    "EFT did not produce resource-pool data.");

            // EFT normally expands each resource to a type-wide configured
            // target. Runtime entity creation needs one instance, not enough
            // free equipment for another whole wave.
            for (int i = 0; i < resourceInfo.Count; i++)
            {
                object info = resourceInfo[i];
                if (info == null)
                    continue;

                FieldInfo poolSizeField =
                    info.GetType().GetField(
                        "PoolSize",
                        InstanceMethods);
                if (poolSizeField == null)
                    throw new MissingFieldException(
                        "EFT resource pool size field was not found.");

                poolSizeField.SetValue(info, 1);
                resourceInfo[i] = info;
            }

            MethodInfo loadMethod =
                typeof(PoolManagerClass)
                    .GetMethods(InstanceMethods)
                    .FirstOrDefault(method =>
                    {
                        if (method.Name != "method_1" ||
                            method.ReturnType != typeof(Task))
                            return false;

                        ParameterInfo[] parameters =
                            method.GetParameters();
                        return parameters.Length == 6 &&
                               parameters[0].ParameterType ==
                                   pools.GetType() &&
                               parameters[1].ParameterType
                                   .IsInstanceOfType(converted);
                    });
            if (loadMethod == null)
                throw new MissingMethodException(
                    "EFT's direct resource-pool loader was not found.");

            LogSource.LogInfo(
                "Directly loading " + resourceInfo.Count +
                " one-instance resource pools.");
            Diz.Jobs.JobScheduler scheduler =
                Object.FindObjectOfType<
                    Diz.Jobs.JobScheduler>();
            bool enabledForceMode =
                scheduler != null &&
                !scheduler.IsForceModeEnabled;
            if (enabledForceMode)
            {
                LogSource.LogInfo(
                    "Enabling EFT job-scheduler force mode for direct entity creation.");
                scheduler.SetForceMode(true, 1f);
            }

            try
            {
                object result = loadMethod.Invoke(
                    poolManager,
                    new object[]
                    {
                        pools,
                        converted,
                        PoolManagerClass.AssemblyType.Online,
                        JobPriorityClass.Immediate,
                        null,
                        cancellationToken
                    });
                Task loadTask = result as Task;
                if (loadTask == null)
                    throw new InvalidOperationException(
                        "EFT's direct resource loader returned no task.");

                await loadTask;
            }
            finally
            {
                if (enabledForceMode &&
                    scheduler != null)
                {
                    scheduler.SetForceMode(false, -1f);
                    LogSource.LogInfo(
                        "Restored EFT job-scheduler force mode after direct entity creation.");
                }
            }
        }

        private async Task<List<Profile>>
            RequestSpawnEntityProfiles(
                WildSpawnType role,
                BotDifficulty difficulty,
                int generation,
                CancellationToken cancellationToken)
        {
            string request = JsonConvert.SerializeObject(
                new
                {
                    conditions = new[]
                    {
                        new
                        {
                            Role = role.ToString(),
                            Limit = 1,
                            Difficulty =
                                difficulty.ToString()
                        }
                    }
                });

            LogSource.LogInfo(
                "Requesting exactly one " + role +
                " descriptor from SPT.");
            Task<string> requestTask =
                RequestHandler.PostJsonAsync(
                    "/client/game/bot/generate",
                    request);
            Task completed = await Task.WhenAny(
                requestTask,
                Task.Delay(30000, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            if (generation != _entitySpawnGeneration)
                return null;
            if (!ReferenceEquals(completed, requestTask))
                throw new TimeoutException(
                    "SPT bot profile request timed out after 30 seconds.");

            string json = await requestTask;
            JToken root = JToken.Parse(json);
            int error = root.Value<int?>("err") ?? 0;
            if (error != 0)
                throw new InvalidOperationException(
                    root.Value<string>("errmsg") ??
                    "SPT bot generation failed.");

            JToken data = root["data"] ?? root;
            CompleteProfileDescriptorClass[] descriptors =
                JsonParserClass.ParseJsonTo<
                    CompleteProfileDescriptorClass[]>(
                    data.ToString(Formatting.None),
                    Array.Empty<JsonConverter>());
            CompleteProfileDescriptorClass descriptor =
                descriptors == null
                    ? null
                    : descriptors.FirstOrDefault(
                        value => value != null);
            List<Profile> profiles = descriptor == null
                ? null
                : new List<Profile>
                {
                    new Profile(descriptor)
                };
            LogSource.LogInfo(
                "SPT returned " +
                (descriptors == null
                    ? 0
                    : descriptors.Length) +
                " " + role +
                " descriptor(s); using exactly one.");
            return profiles;
        }

        private static bool TrySelectEntitySpawnPoint(
            BotSpawner spawner,
            BotsController controller,
            BotCreationDataClass creationData,
            Vector3 requestedPosition,
            out BotZone selectedZone,
            out EFT.Game.Spawning.ISpawnPoint selectedPoint)
        {
            selectedZone = null;
            selectedPoint = null;
            if (spawner == null ||
                controller == null ||
                creationData == null ||
                spawner.AllBotZones == null ||
                spawner.SpawnSystem == null)
                return false;

            float bestScore = float.MaxValue;
            for (int zoneIndex = 0;
                 zoneIndex < spawner.AllBotZones.Length;
                 zoneIndex++)
            {
                BotZone zone = spawner.AllBotZones[zoneIndex];
                if (zone == null ||
                    zone.SpawnPoints == null)
                    continue;

                bool compatible =
                    creationData.CanAtZoneByType(
                        zone,
                        controller.ZonesLeaveController);
                if (!compatible)
                    continue;

                EFT.Game.Spawning.ISpawnPoint[] points =
                    zone.SpawnPoints;
                for (int pointIndex = 0;
                     pointIndex < points.Length;
                     pointIndex++)
                {
                    EFT.Game.Spawning.ISpawnPoint point =
                        points[pointIndex];
                    if (point == null)
                        continue;

                    bool currentlyValid =
                        spawner.SpawnSystem.IsValidSpawn(
                            point,
                            creationData,
                            Time.time);
                    float distance = (
                        point.Position -
                        requestedPosition).sqrMagnitude;
                    // Prefer a currently valid marker, but the forced spawn branch
                    // may still use a temporarily blocked marker in this compatible
                    // zone to avoid EFT's delayed queue.
                    float score = distance +
                        (currentlyValid ? 0f : 10000000f);
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    selectedZone = zone;
                    selectedPoint = point;
                }
            }

            return selectedZone != null &&
                   selectedPoint != null;
        }

        private static string GetSpawnExceptionMessage(
            Exception exception)
        {
            if (exception == null)
                return "Unknown error.";

            Exception current = exception;
            while (current.InnerException != null &&
                   (current is TargetInvocationException ||
                    current is AggregateException))
            {
                current = current.InnerException;
            }

            return string.IsNullOrWhiteSpace(current.Message)
                ? current.GetType().Name
                : current.Message;
        }

        private System.Collections.IEnumerator
            FinalizeSpawnedEntity(
                BotOwner botOwner,
                BotsController controller,
                Vector3 requestedPosition,
                string displayName,
                bool aiDisabled,
                bool ignoreNavMesh)
        {
            // EFT's direct completion callback runs at the end of its bot
            // registration pass. Waiting one frame also lets movement,
            // animation, and GameWorld listeners observe the new player.
            yield return null;

            try
            {
                if (botOwner == null ||
                    botOwner.IsDead ||
                    controller == null)
                {
                    _spawnEntityStatus =
                        "The spawned bot was removed before setup.";
                    yield break;
                }

                if (ignoreNavMesh)
                {
                    botOwner.GetPlayer.Teleport(
                        requestedPosition,
                        false);
                }
                else
                {
                    // The activation position was already resolved against a
                    // nearby NavMesh polygon. A direct teleport preserves that
                    // local result; DevelopmentTeleportBot may search globally
                    // and relocate the bot to a distant map polygon.
                    botOwner.GetPlayer.Teleport(
                        requestedPosition,
                        false);
                }
                bool reachedRequestedArea =
                    Vector3.Distance(
                        botOwner.GetPlayer.Position,
                        requestedPosition) <= 21f;
                if (aiDisabled)
                    SetEntityAiEnabled(botOwner, false);

                _nextEntityListRefresh = 0f;
                _spawnEntityStatus = reachedRequestedArea ||
                                     ignoreNavMesh
                    ? "Spawned " + displayName +
                      " at the requested location."
                    : "Spawned " + displayName +
                      ", but EFT found no valid NavMesh near the requested location.";
            }
            catch (Exception exception)
            {
                _spawnEntityStatus =
                    "Bot spawned, but setup failed: " +
                    GetSpawnExceptionMessage(exception);
                LogSource.LogWarning(
                    "Could not finalize spawned entity: " +
                    exception);
            }
            finally
            {
                _spawnEntityInProgress = false;
            }
        }

        private void ReserveFieldKitSpawnCapacity(
            BotSpawner spawner)
        {
            if (spawner == null)
                return;

            if (_fieldKitCapacitySpawner != null &&
                _fieldKitCapacitySpawner != spawner)
                ReleaseAllFieldKitSpawnCapacity();

            _fieldKitCapacitySpawner = spawner;
            spawner.SetMaxBots(spawner.MaxBots + 1);
            _fieldKitCapacityHeadroom++;
            LogSource.LogInfo(
                "Reserved FieldKit bot headroom: max=" +
                spawner.MaxBots + ", fieldKit=" +
                _fieldKitCapacityHeadroom + ".");
        }

        private EFT.ResourceKey[] GetUncachedEntityResources(
            IEnumerable<EFT.ResourceKey> resources)
        {
            return resources.Where(resource =>
            {
                string key = EntityResourceCacheKey(resource);
                if (key == null)
                    return false;

                int capacity;
                _fieldKitResourceCapacity.TryGetValue(
                    key,
                    out capacity);
                int usage;
                _fieldKitResourceUsage.TryGetValue(
                    key,
                    out usage);
                return usage >= capacity;
            }).ToArray();
        }

        private static string EntityResourceCacheKey(
            EFT.ResourceKey resource)
        {
            return string.IsNullOrEmpty(resource.path)
                ? null
                : resource.path + "\n" +
                  (resource.rcid ?? "");
        }

        private void CacheExpandedEntityResources(
            IEnumerable<EFT.ResourceKey> resources)
        {
            foreach (EFT.ResourceKey resource in resources)
            {
                string key = EntityResourceCacheKey(resource);
                if (key == null)
                    continue;

                int capacity;
                _fieldKitResourceCapacity.TryGetValue(
                    key,
                    out capacity);
                _fieldKitResourceCapacity[key] =
                    capacity + 1;
            }
        }

        private void RegisterFieldKitSpawn(
            BotSpawner spawner,
            BotOwner botOwner,
            IEnumerable<EFT.ResourceKey> resources)
        {
            string profileId =
                botOwner == null ||
                botOwner.Profile == null
                    ? null
                    : botOwner.Profile.Id;
            if (string.IsNullOrEmpty(profileId))
            {
                ReleaseFieldKitSpawnCapacity(spawner);
                return;
            }

            _fieldKitSpawnProfileIds.Add(profileId);
            string[] resourcePaths =
                resources.Select(EntityResourceCacheKey)
                    .Where(key => key != null)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            _fieldKitSpawnResources[profileId] =
                resourcePaths;
            for (int i = 0;
                 i < resourcePaths.Length;
                 i++)
            {
                int usage;
                _fieldKitResourceUsage.TryGetValue(
                    resourcePaths[i],
                    out usage);
                _fieldKitResourceUsage[resourcePaths[i]] =
                    usage + 1;
            }
            LogSource.LogInfo(
                "Registered FieldKit bot " + profileId +
                ": alive=" + spawner.AllBotsCount +
                ", loading=" + spawner.BotCreator.BotsLoading +
                ", spawning=" + spawner.InSpawnProcess +
                ", max=" + spawner.MaxBots + ".");
        }

        private void HandleFieldKitSpawnRemoved(
            Player player)
        {
            string profileId =
                player == null ||
                player.Profile == null
                    ? null
                    : player.Profile.Id;
            if (string.IsNullOrEmpty(profileId) ||
                !_fieldKitSpawnProfileIds.Remove(profileId))
                return;

            string[] resourcePaths;
            if (_fieldKitSpawnResources.TryGetValue(
                    profileId,
                    out resourcePaths))
            {
                _fieldKitSpawnResources.Remove(profileId);
                for (int i = 0;
                     i < resourcePaths.Length;
                     i++)
                {
                    int usage;
                    if (!_fieldKitResourceUsage.TryGetValue(
                            resourcePaths[i],
                            out usage))
                        continue;

                    usage--;
                    if (usage <= 0)
                        _fieldKitResourceUsage.Remove(
                            resourcePaths[i]);
                    else
                        _fieldKitResourceUsage[
                            resourcePaths[i]] = usage;
                }
            }
            ReleaseFieldKitSpawnCapacity(
                _fieldKitCapacitySpawner);
        }

        private void ReleaseFieldKitSpawnCapacity(
            BotSpawner spawner)
        {
            if (_fieldKitCapacityHeadroom <= 0)
                return;

            _fieldKitCapacityHeadroom--;
            if (spawner != null && !spawner.GameEnd)
                spawner.SetMaxBots(
                    Math.Max(0, spawner.MaxBots - 1));
            if (_fieldKitCapacityHeadroom == 0)
                _fieldKitCapacitySpawner = null;
        }

        private void ReleaseAllFieldKitSpawnCapacity()
        {
            BotSpawner spawner = _fieldKitCapacitySpawner;
            int headroom = _fieldKitCapacityHeadroom;
            _fieldKitSpawnProfileIds.Clear();
            _fieldKitSpawnResources.Clear();
            _fieldKitResourceUsage.Clear();
            _fieldKitResourceCapacity.Clear();
            _fieldKitCapacityHeadroom = 0;
            _fieldKitCapacitySpawner = null;

            if (spawner != null &&
                !spawner.GameEnd &&
                headroom > 0)
            {
                spawner.SetMaxBots(
                    Math.Max(0, spawner.MaxBots - headroom));
            }
        }

        private void CancelPendingEntitySpawn()
        {
            _entitySpawnGeneration++;
            CancellationTokenSource cancellation =
                _entitySpawnCancellation;
            _entitySpawnCancellation = null;
            if (cancellation != null)
            {
                try
                {
                    cancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }
            _spawnEntityInProgress = false;
        }
    }
}
