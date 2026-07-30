
namespace FieldKit
{
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private void RefreshWorld()
        {
            GameWorld nextWorld = Singleton<GameWorld>.Instance;

            if (nextWorld != _world)
                AttachWorld(nextWorld);

            if (_world == null)
                return;

            if (_localPlayer != _world.MainPlayer)
                AttachLocalPlayer(_world.MainPlayer);

            RefreshCamera();

            if (_scopeRefreshRequested)
            {
                RefreshScopeOverlays();
                _scopeRefreshRequested = false;
            }
        }

        private void AttachWorld(GameWorld world)
        {
            DetachWorld();
            _world = world;
            _camera = null;
            _nextExtractionRefresh = 0f;
            _scopeRefreshRequested = true;

            if (_world == null)
                return;

            _world.OnPersonAdd += OnPersonAdded;
            _world.OnLateUpdate += OnWorldLateUpdate;
            _world.OnLootItemDestroyed += OnWorldLootItemDestroyed;
            _world.AfterGameStarted += OnLootWorldStarted;
            GameWorld.OnDispose += OnWorldDisposed;
            AttachLootWorldEvents();
            AttachLocalPlayer(_world.MainPlayer);

            if (_world.RegisteredPlayers == null)
                return;

            foreach (IPlayer person in _world.RegisteredPlayers)
                AddTarget(person as Player);
        }

        private void DetachWorld()
        {
            CancelPendingEntitySpawn();
            ReleaseAllFieldKitSpawnCapacity();
            ReleaseFriendlyAi();
            ReleaseEntityAiOverrides();
            _extractionPoints.Clear();
            _usableExtractionIds.Clear();
            _nextExtractionRefresh = 0f;
            ClearLootEspCaches();
            RestoreWorldChams();
            RestoreVegetationCulling();
            _worldChamStates.Clear();
            _seenLootIds.Clear();
            _knownLootIds.Clear();
            _seenCorpseIds.Clear();
            _knownCorpseIds.Clear();
            _staleWorldChamIds.Clear();
            _vegetationManagerStates.Clear();
            _knownVegetationManagers.Clear();
            _corpseChamDiscoveryDirty = true;
            _lootChamDiscoveryDirty = true;
            _worldChamPassDirty = true;
            _lastCorpseChamsEnabled = false;
            _lastLootChamsEnabled = false;
            _lastLootChamDistance = -1f;
            _hasWorldChamPassPosition = false;
            _nextVegetationManagerScan = 0f;

            if (_world != null)
            {
                _world.OnPersonAdd -= OnPersonAdded;
                _world.OnLateUpdate -= OnWorldLateUpdate;
                _world.OnLootItemDestroyed -= OnWorldLootItemDestroyed;
                _world.AfterGameStarted -= OnLootWorldStarted;
                GameWorld.OnDispose -= OnWorldDisposed;
            }

            AttachLocalPlayer(null);

            for (int i = _targets.Count - 1; i >= 0; i--)
                RemoveTargetAt(i);

            ReleaseNoWeightOverride();
            DestroyScopeOverlays();
            _world = null;
            _camera = null;
        }

        private void AttachLocalPlayer(Player player)
        {
            if (_localPlayer != player)
            {
                CloseLivingAiInventory();
                RestoreContainerSearchOverride();
                ClearWeaponActionSpeed();
                ClearAdsSpeed();
                ClearProtectedMagazine();
                RefreshEquippedWeapon(null, null, true);
            }

            RestoreDisabledPlayerColliders();

            if (_localPlayer != null)
            {
                _localPlayer.OnSightChangedEvent -= OnSightChanged;
                _localPlayer.OnSmoothSightChange -= OnSmoothSightChanged;
                _localPlayer.OnHandsControllerChanged -= OnHandsControllerChanged;
            }

            _localPlayer = player;
            _scopeRefreshRequested = true;
            _localPlayerColliderIds.Clear();
            _transparentVisibilityColliderIds.Clear();
            _opaqueVisibilityColliderIds.Clear();

            if (_localPlayer == null)
                return;

            CachePlayerColliders(
                _localPlayer, _localPlayerColliderIds);
            _localPlayer.OnSightChangedEvent += OnSightChanged;
            _localPlayer.OnSmoothSightChange += OnSmoothSightChanged;
            _localPlayer.OnHandsControllerChanged += OnHandsControllerChanged;

            ICharacterController characterController =
                _localPlayer.MovementContext == null
                    ? null
                    : _localPlayer.MovementContext.CharacterController;
            LogSource.LogInfo(
                "Local character controller: " +
                (characterController == null
                    ? "not initialized"
                    : characterController.GetType().FullName));
        }

        private void OnPersonAdded(IPlayer person)
        {
            AddTarget(person as Player);
        }

        private void AddTarget(Player player)
        {
            if (player == null || player.IsYourPlayer)
                return;

            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i].Player == player)
                    return;
            }

            EspKind kind;
            Color color;

            if (!Classify(player, out kind, out color))
                return;

            string roleKey = GetRoleKey(player);
            EspRoleSettings roleSettings = GetRoleSettings(roleKey);
            string name = "Unknown";

            try
            {
                if (player.Profile != null &&
                    !string.IsNullOrEmpty(player.Profile.Nickname))
                    name = player.Profile.Nickname;
            }
            catch { }

            Target target = new Target
            {
                Player = player,
                Root = Original(player.Transform),
                HealthController = player.HealthController,
                Kind = kind,
                RoleKey = roleKey,
                RoleLabel = roleSettings == null
                    ? KindName(kind)
                    : roleSettings.Label,
                Color = roleSettings == null
                    ? color
                    : ParseVisualColor(
                        roleSettings.VisibleColor.Value,
                        roleSettings.DefaultVisible),
                DisplayColor = roleSettings == null
                    ? color
                    : ParseVisualColor(
                        roleSettings.VisibleColor.Value,
                        roleSettings.DefaultVisible),
                Name = name,
                CachedTextPrefix =
                    (roleSettings == null
                        ? KindName(kind)
                        : roleSettings.Label) + " | " + name,
                HealthRatio = 1f,
                HealthDirty = true,
                IsAlive = true
            };

            AttachTargetHealthEvents(target);

            CacheBones(target);
            CachePlayerColliders(player, target.ColliderIds);
            player.OnIPlayerDeadOrUnspawn += OnPlayerRemoved;
            _targets.Add(target);
        }

        private static void CachePlayerColliders(
            Player player,
            HashSet<int> destination)
        {
            destination.Clear();
            if (player == null)
                return;

            Collider[] colliders =
                player.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    destination.Add(colliders[i].GetInstanceID());
            }
        }

        private static void AttachTargetHealthEvents(Target target)
        {
            if (target == null ||
                target.HealthController == null ||
                target.HealthChangedHandler != null)
                return;

            target.HealthChangedHandler =
                (part, amount, damage) => target.HealthDirty = true;
            target.HealthController.HealthChangedEvent +=
                target.HealthChangedHandler;
            target.HealthDirty = true;
        }

        private void EnsureTargetRuntimeCache(Target target)
        {
            if (target == null || target.Player == null)
                return;

            if (target.Root == null)
                target.Root = Original(target.Player.Transform);

            if (target.HealthController == null)
            {
                target.HealthController =
                    target.Player.HealthController;
                AttachTargetHealthEvents(target);
            }

            float now = Time.unscaledTime;
            if (now < target.NextRuntimeRefresh)
                return;

            target.NextRuntimeRefresh =
                now + 1f +
                (target.Player.GetInstanceID() & 3) * 0.05f;
            CachePlayerColliders(target.Player, target.ColliderIds);
            RefreshTargetRole(target);
        }

        private void RefreshTargetRole(Target target)
        {
            string roleKey = GetRoleKey(target.Player);
            if (string.Equals(
                    roleKey,
                    target.RoleKey,
                    StringComparison.OrdinalIgnoreCase))
                return;

            EspKind kind;
            Color fallback;
            if (!Classify(target.Player, out kind, out fallback))
                return;

            EspRoleSettings settings = GetRoleSettings(roleKey);
            target.Kind = kind;
            target.RoleKey = roleKey;
            target.RoleLabel = settings == null
                ? KindName(kind)
                : settings.Label;
            target.Color = settings == null
                ? fallback
                : ParseVisualColor(
                    settings.VisibleColor.Value,
                    settings.DefaultVisible);
            target.DisplayColor = target.Color;
            target.CachedTextPrefix =
                target.RoleLabel + " | " + target.Name;
            target.NextTextUpdate = 0f;
            target.NextVisibilityUpdate = 0f;
            target.NextScopeVisibilityUpdate = 0f;
        }

        private void OnPlayerRemoved(IPlayer person)
        {
            Player removed = person as Player;
            _corpseChamDiscoveryDirty = true;
            HandleLivingAiRemoved(removed);
            HandleEntityAiRemoved(removed);
            HandleFieldKitSpawnRemoved(removed);

            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                if (_targets[i].Player == removed)
                    RemoveTargetAt(i);
            }
        }

        private void RemoveTargetAt(int index)
        {
            Target target = _targets[index];

            if (target.Player != null)
                target.Player.OnIPlayerDeadOrUnspawn -= OnPlayerRemoved;
            if (target.HealthController != null &&
                target.HealthChangedHandler != null)
            {
                target.HealthController.HealthChangedEvent -=
                    target.HealthChangedHandler;
            }

            target.IsAlive = false;
            RestoreTargetChams(target);
            _targets.RemoveAt(index);
        }

        private void OnWorldLateUpdate(float deltaTime)
        {
            long perfStarted = PerfTimestamp();
            try
            {
                OnWorldLateUpdateCore(deltaTime);
            }
            finally
            {
                RecordPerf(
                    perfStarted,
                    ref _perfWorldTicks,
                    ref _perfWorldCalls,
                    ref _perfWorldMaxTicks);
            }
        }

        private void OnWorldLateUpdateCore(float deltaTime)
        {
            if (!_enabled.Value && !_chamsEnabled.Value)
                return;

            float now = Time.unscaledTime;
            Target visibilityFocus = FindVisibilityFocusTarget();
            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                EnsureTargetRuntimeCache(target);
                bool usedByEsp =
                    _enabled.Value && ShouldShow(target);
                bool usedByChams =
                    _chamsEnabled.Value &&
                    _chamsCharacters.Value &&
                    ShouldShowRoleChams(target);
                if (!usedByEsp && !usedByChams)
                {
                    target.IsOnMainScreen = false;
                    target.HasVisibility = false;
                    target.HasPerBoneVisibility = false;
                    continue;
                }
                if (now < target.NextScreenCheck)
                    continue;

                target.NextScreenCheck =
                    now + 1f / 30f +
                    (target.Player.GetInstanceID() & 3) * 0.0015f;

                bool wasOnScreen = target.IsOnMainScreen;
                float centerDistance;
                target.IsOnMainScreen = TryGetMainScreenDistance(
                    target, out centerDistance);

                if (!target.IsOnMainScreen)
                {
                    target.HasPerBoneVisibility = false;
                    continue;
                }

                if (!wasOnScreen)
                    target.NextVisibilityUpdate = 0f;

                UpdateVisibility(
                    target,
                    ReferenceEquals(target, visibilityFocus),
                    now,
                    usedByChams &&
                    _chamsPerLimbVisibility.Value);
                target.DisplayColor = GetDisplayColor(target);

                if (usedByEsp && target.HealthDirty)
                {
                    target.HealthDirty = false;

                    try
                    {
                        float healthRatio;

                        if (TryGetHealthRatio(target, out healthRatio))
                            target.HealthRatio = healthRatio;
                    }
                    catch
                    {
                        target.HealthDirty = true;
                    }
                }
            }
        }

        private Target FindVisibilityFocusTarget()
        {
            if (!_visibilityCheck.Value || _camera == null)
                return null;

            Target closest = null;
            float closestCenterDistanceSq = float.MaxValue;
            Vector3 cameraPosition = _camera.transform.position;
            float maxDistanceSq = _maxDistance.Value * _maxDistance.Value;
            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                if (target == null ||
                    target.Player == null ||
                    !target.IsAlive ||
                    !ShouldShow(target) ||
                    target.Root == null ||
                    (target.Root.position - cameraPosition).sqrMagnitude >
                    maxDistanceSq)
                    continue;

                float centerDistance;
                if (!TryGetMainScreenDistance(target, out centerDistance))
                    continue;

                float centerDistanceSq = centerDistance * centerDistance;
                if (centerDistanceSq >= closestCenterDistanceSq)
                    continue;

                closest = target;
                closestCenterDistanceSq = centerDistanceSq;
            }

            return closest;
        }

        private static bool TryGetHealthRatio(
            Target target,
            out float ratio)
        {
            ratio = 0f;

            if (target == null || target.HealthController == null)
                return false;

            float current = 0f;
            float maximum = 0f;

            for (int i = 0; i < HealthParts.Length; i++)
            {
                ValueStruct health = target.HealthController
                    .GetBodyPartHealth(HealthParts[i], false);
                current += Mathf.Max(0f, health.Current);
                maximum += Mathf.Max(0f, health.Maximum);
            }

            if (maximum <= 0f ||
                float.IsNaN(current) ||
                float.IsInfinity(current))
                return false;

            ratio = Mathf.Clamp01(current / maximum);
            return true;
        }

        private void OnWorldDisposed()
        {
            DetachWorld();
        }

        private void OnSightChanged(SightComponent sight)
        {
            InvalidateScope();
        }

        private void OnSmoothSightChanged(
            SightComponent sight,
            ESmoothScopeState state)
        {
            InvalidateScope();
        }

        private void OnHandsControllerChanged(
            Player.AbstractHandsController previous,
            Player.AbstractHandsController current)
        {
            RefreshHandsWeapon(current);
            InvalidateScope();
        }

        private void InvalidateScope()
        {
            _scopeRefreshRequested = true;

            for (int i = 0; i < _targets.Count; i++)
                _targets[i].NextVisibilityUpdate = 0f;
        }

    }
}
