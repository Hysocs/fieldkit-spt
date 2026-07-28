
namespace FieldKit
{
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private void UpdateChams()
        {
            UpdateVegetationCulling();

            if (!_chamsEnabled.Value ||
                _world == null ||
                _localPlayer == null)
            {
                if (_chamsActive)
                {
                    RestoreAllChams();
                    _chamsActive = false;
                }

                return;
            }

            if (Time.unscaledTime < _nextChamUpdate)
                return;

            _chamsActive = true;
            _nextChamUpdate = Time.unscaledTime + 1f / 15f;
            EnsureChamMaterials();

            float maxDistanceSq =
                _chamsMaxDistance.Value * _chamsMaxDistance.Value;
            Vector3 localPosition = _localPlayer.Transform.position;

            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                Player player = target.Player;

                if (!_chamsCharacters.Value ||
                    player == null ||
                    !target.IsAlive ||
                    target.Root == null ||
                    !ShouldShowRoleChams(target) ||
                    (target.Root.position - localPosition).sqrMagnitude >
                    maxDistanceSq)
                {
                    RestoreTargetChams(target);
                    continue;
                }

                ApplyTargetChams(target);
            }

            UpdateWorldChams();
        }

        private void EnsureChamMaterials()
        {
            if (_pmcChamMaterials == null)
            {
                _pmcChamMaterials = new ChamMaterialSet(
                    "PMC");
                _scavChamMaterials = new ChamMaterialSet(
                    "Scav");
                _bossChamMaterials = new ChamMaterialSet(
                    "Boss");
            }

            _lastPmcChamVisible = _pmcChamColor.Value;
            _lastPmcChamHidden = _pmcChamOccludedColor.Value;
            _lastScavChamVisible = _scavChamColor.Value;
            _lastScavChamHidden = _scavChamOccludedColor.Value;
            _lastBossChamVisible = _bossChamColor.Value;
            _lastBossChamHidden = _bossChamOccludedColor.Value;
            _lastChamOpacity = _chamsOpacity.Value;
            _pmcChamMaterials.Update(
                GetChamColor(EspKind.Pmc),
                GetChamColor(EspKind.Pmc, true),
                _chamsOpacity.Value);
            _scavChamMaterials.Update(
                GetChamColor(EspKind.Scav),
                GetChamColor(EspKind.Scav, true),
                _chamsOpacity.Value);
            _bossChamMaterials.Update(
                GetChamColor(EspKind.Boss),
                GetChamColor(EspKind.Boss, true),
                _chamsOpacity.Value);

            for (int i = 0; i < _espRoles.Count; i++)
            {
                EspRoleSettings role = _espRoles[i];
                if (role.ChamMaterials == null)
                    continue;
                EnsureRoleChamMaterials(role);
            }
        }

        private void EnsureRoleChamMaterials(EspRoleSettings role)
        {
            if (role.ChamMaterials == null)
                role.ChamMaterials = new ChamMaterialSet(role.Label);
            if (role.LastChamVisible ==
                    role.ChamVisibleColor.Value &&
                role.LastChamHidden ==
                    role.ChamHiddenColor.Value &&
                Mathf.Approximately(
                    role.LastChamOpacity,
                    _chamsOpacity.Value))
                return;
            role.LastChamVisible = role.ChamVisibleColor.Value;
            role.LastChamHidden = role.ChamHiddenColor.Value;
            role.LastChamOpacity = _chamsOpacity.Value;
            role.ChamMaterials.Update(
                GetRoleChamColor(role, false),
                GetRoleChamColor(role, true),
                _chamsOpacity.Value);
        }

        private void ApplyTargetChams(Target target)
        {
            if (_chamsPerLimbVisibility.Value)
            {
                RestoreTargetMaterialChams(target);
                ApplyTargetLimbChams(target);
                return;
            }

            DestroyTargetLimbChams(target);
            EnsureChamRenderers(target);

            if (target.ChamRenderers == null ||
                target.ChamOriginalMaterials == null)
                return;

            bool visible =
                !_visibilityCheck.Value ||
                !target.HasVisibility ||
                target.IsVisible;

            for (int i = 0; i < target.ChamRenderers.Length; i++)
            {
                Renderer renderer = target.ChamRenderers[i];

                if (renderer == null)
                    continue;

                if (!target.ChamApplied[i] ||
                    target.ChamAppliedVisible[i] != visible)
                {
                    renderer.sharedMaterials = visible
                        ? target.ChamVisibleMaterials[i]
                        : target.ChamOccludedMaterials[i];
                    renderer.allowOcclusionWhenDynamic = false;
                    target.ChamApplied[i] = true;
                    target.ChamAppliedVisible[i] = visible;
                    target.ChamsActive = true;
                }
            }
        }

        private void EnsureChamRenderers(Target target)
        {
            if (target.ChamRenderers != null &&
                AreRenderersValid(target.ChamRenderers))
                return;

            RestoreTargetChams(target);
            _bodyRenderers.Clear();

            try
            {
                if (target.Player == null ||
                    target.Player.PlayerBody == null)
                    return;

                target.Player.PlayerBody.GetBodyRenderersNonAlloc(
                    _bodyRenderers);
            }
            catch
            {
                return;
            }

            int count = 0;

            for (int i = 0; i < _bodyRenderers.Count; i++)
            {
                Renderer[] renderers = _bodyRenderers[i].Renderers;

                if (renderers != null)
                    count += renderers.Length;
            }

            Renderer[] next = new Renderer[count];
            int write = 0;

            for (int i = 0; i < _bodyRenderers.Count; i++)
            {
                Renderer[] renderers = _bodyRenderers[i].Renderers;

                if (renderers == null)
                    continue;

                for (int j = 0; j < renderers.Length; j++)
                    next[write++] = renderers[j];
            }

            target.ChamRenderers = next;
            target.ChamOriginalMaterials = new Material[next.Length][];
            target.ChamVisibleMaterials = new Material[next.Length][];
            target.ChamOccludedMaterials = new Material[next.Length][];
            target.ChamApplied = new bool[next.Length];
            target.ChamAppliedVisible = new bool[next.Length];
            target.ChamOriginalOcclusion = new bool[next.Length];
            EspRoleSettings roleSettings =
                GetRoleSettings(target.RoleKey);
            if (roleSettings != null)
                EnsureRoleChamMaterials(roleSettings);
            ChamMaterialSet materials =
                roleSettings != null
                    ? roleSettings.ChamMaterials
                    : GetChamMaterials(target.Kind);

            for (int i = 0; i < next.Length; i++)
            {
                Renderer renderer = next[i];

                if (renderer == null)
                    continue;

                Material[] originals = renderer.sharedMaterials;
                int slots = Mathf.Max(1, originals.Length);
                target.ChamOriginalMaterials[i] = originals;
                target.ChamOriginalOcclusion[i] =
                    renderer.allowOcclusionWhenDynamic;
                target.ChamVisibleMaterials[i] =
                    FilledMaterials(slots, materials.Visible);
                target.ChamOccludedMaterials[i] =
                    FilledMaterials(slots, materials.Occluded);
            }
        }

        private static bool AreRenderersValid(Renderer[] renderers)
        {
            if (renderers.Length == 0)
                return false;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    return false;
            }

            return true;
        }

        private static Material[] FilledMaterials(
            int count,
            Material material)
        {
            Material[] result = new Material[count];
            for (int i = 0; i < count; i++)
                result[i] = material;

            return result;
        }

        private static void RestoreTargetMaterialChams(Target target)
        {
            if (!target.ChamsActive ||
                target.ChamRenderers == null ||
                target.ChamApplied == null)
                return;

            for (int i = 0; i < target.ChamRenderers.Length; i++)
            {
                Renderer renderer = target.ChamRenderers[i];
                if (renderer == null || !target.ChamApplied[i])
                    continue;

                Material current = renderer.sharedMaterial;
                Material visible = target.ChamVisibleMaterials[i][0];
                Material occluded = target.ChamOccludedMaterials[i][0];
                if (current == visible || current == occluded)
                {
                    renderer.sharedMaterials =
                        target.ChamOriginalMaterials[i];
                }

                renderer.allowOcclusionWhenDynamic =
                    target.ChamOriginalOcclusion[i];
                target.ChamApplied[i] = false;
            }

            target.ChamsActive = false;
        }

        private static void RestoreTargetChams(Target target)
        {
            RestoreTargetMaterialChams(target);
            DestroyTargetLimbChams(target);
        }

        private void RestoreAllChams()
        {
            for (int i = 0; i < _targets.Count; i++)
                RestoreTargetChams(_targets[i]);

            RestoreWorldChams();
        }

        private void UpdateWorldChams()
        {
            EnsureWorldChamMaterials();

            bool settingsChanged =
                _lastCorpseChamsEnabled != _chamsCorpses.Value ||
                _lastLootChamsEnabled != _chamsLoot.Value ||
                !Mathf.Approximately(
                    _lastLootChamDistance,
                    _lootRenderDistance.Value);
            if (settingsChanged)
            {
                _lastCorpseChamsEnabled = _chamsCorpses.Value;
                _lastLootChamsEnabled = _chamsLoot.Value;
                _lastLootChamDistance = _lootRenderDistance.Value;
                _worldChamPassDirty = true;
                if (_chamsCorpses.Value)
                    _corpseChamDiscoveryDirty = true;
                if (_chamsLoot.Value)
                    _lootChamDiscoveryDirty = true;
            }

            if (_chamsCorpses.Value &&
                _corpseChamDiscoveryDirty)
            {
                _corpseChamDiscoveryDirty = false;
                _perfChamDiscoveryPasses++;
                ScanCorpseChamRenderers();
            }

            if (_chamsLoot.Value &&
                _lootChamDiscoveryDirty)
            {
                _lootChamDiscoveryDirty = false;
                _perfChamDiscoveryPasses++;
                ScanLootChamRenderers();
            }

            if (_chamsLoot.Value && _localPlayer != null)
            {
                Vector3 position = _localPlayer.Transform.position;
                if (!_hasWorldChamPassPosition ||
                    (position - _lastWorldChamPassPosition)
                        .sqrMagnitude > 25f)
                {
                    _lastWorldChamPassPosition = position;
                    _hasWorldChamPassPosition = true;
                    _worldChamPassDirty = true;
                }
            }

            if (!_worldChamPassDirty)
                return;

            _staleWorldChamIds.Clear();
            foreach (KeyValuePair<int, WorldChamState> pair
                     in _worldChamStates)
            {
                WorldChamState state = pair.Value;
                if (state.Renderer == null)
                {
                    _staleWorldChamIds.Add(pair.Key);
                    continue;
                }

                if (state.Kind == WorldChamKind.Loot &&
                    !IsLooseLootRenderer(
                        state.LootSource, state.Renderer))
                {
                    RestoreWorldChamState(state);
                    _staleWorldChamIds.Add(pair.Key);
                    continue;
                }

                bool enabled = IsWorldChamEnabled(state.Kind);
                bool distanceCulled =
                    enabled &&
                    state.Kind == WorldChamKind.Loot &&
                    _localPlayer != null &&
                    (state.Renderer.bounds.center -
                     _localPlayer.Transform.position).sqrMagnitude >
                    _lootRenderDistance.Value *
                    _lootRenderDistance.Value;

                bool shouldApply = enabled && !distanceCulled;
                if (shouldApply && !state.Applied)
                {
                    state.Renderer.sharedMaterials =
                        state.ChamMaterials;
                    state.Applied = true;
                }
                else if (!shouldApply && state.Applied)
                {
                    RestoreWorldChamState(state);
                }
            }

            for (int i = 0; i < _staleWorldChamIds.Count; i++)
            {
                RestoreWorldChamState(
                    _worldChamStates[_staleWorldChamIds[i]]);
                _worldChamStates.Remove(_staleWorldChamIds[i]);
            }

            _worldChamPassDirty = false;
        }

        private void EnsureWorldChamMaterials()
        {
            bool created = false;
            if (_worldChamMaterials == null)
            {
                _worldChamMaterials = new WorldChamMaterialSet[
                    (int)WorldChamKind.Count];
                for (int i = 0; i < _worldChamMaterials.Length; i++)
                {
                    _worldChamMaterials[i] = new WorldChamMaterialSet(
                        ((WorldChamKind)i).ToString());
                }
                created = true;
            }

            if (!created &&
                _lastCorpseChamColor == _chamsCorpseColor.Value &&
                _lastLootChamColor == _chamsLootColor.Value)
                return;

            _lastCorpseChamColor = _chamsCorpseColor.Value;
            _lastLootChamColor = _chamsLootColor.Value;
            for (int i = 0; i < _worldChamMaterials.Length; i++)
            {
                WorldChamKind kind = (WorldChamKind)i;
                _worldChamMaterials[i].Update(GetWorldChamColor(kind));
            }
        }

        private void ScanCorpseChamRenderers()
        {
            _seenCorpseIds.Clear();
            if (_chamsCorpses.Value &&
                _world != null &&
                _world.LootItems != null)
            {
                int lootCount = _world.LootItems.Count;
                for (int i = 0; i < lootCount; i++)
                {
                    EFT.Interactive.Corpse corpse =
                        _world.LootItems.GetByIndex(i)
                            as EFT.Interactive.Corpse;
                    if (corpse == null ||
                        !corpse.gameObject.activeInHierarchy)
                        continue;

                    int corpseId = corpse.GetInstanceID();
                    _seenCorpseIds.Add(corpseId);
                    if (!_knownCorpseIds.Add(corpseId))
                        continue;

                    Renderer[] corpseRenderers =
                        corpse.GetComponentsInChildren<Renderer>(true);
                    for (int j = 0; j < corpseRenderers.Length; j++)
                    {
                        AddWorldChamRenderer(
                            corpseRenderers[j],
                            WorldChamKind.Corpse,
                            corpse);
                    }
                }
            }

            _knownCorpseIds.IntersectWith(_seenCorpseIds);

            _staleWorldChamIds.Clear();
            foreach (KeyValuePair<int, WorldChamState> pair
                     in _worldChamStates)
            {
                WorldChamState state = pair.Value;
                if (state.Kind == WorldChamKind.Corpse &&
                    (!_chamsCorpses.Value ||
                     state.LootSource == null ||
                     !_seenCorpseIds.Contains(
                         state.LootSource.GetInstanceID())))
                    _staleWorldChamIds.Add(pair.Key);
            }

            for (int i = 0; i < _staleWorldChamIds.Count; i++)
            {
                RestoreWorldChamState(
                    _worldChamStates[_staleWorldChamIds[i]]);
                _worldChamStates.Remove(_staleWorldChamIds[i]);
            }

            if (_staleWorldChamIds.Count > 0)
                _worldChamPassDirty = true;
        }

        private void ScanLootChamRenderers()
        {
            _seenLootIds.Clear();
            if (_chamsLoot.Value &&
                _world != null &&
                _world.LootItems != null)
            {
                int lootCount = _world.LootItems.Count;
                for (int i = 0; i < lootCount; i++)
                {
                    EFT.Interactive.LootItem loot =
                        _world.LootItems.GetByIndex(i);
                    if (!IsLooseWorldLoot(loot))
                        continue;

                    int lootId = loot.GetInstanceID();
                    _seenLootIds.Add(lootId);
                    if (!_knownLootIds.Add(lootId))
                        continue;

                    List<Renderer> renderers =
                        LootRenderersField == null
                            ? null
                            : LootRenderersField.GetValue(loot)
                                as List<Renderer>;
                    if (renderers == null)
                        continue;

                    for (int j = 0; j < renderers.Count; j++)
                    {
                        Renderer renderer = renderers[j];
                        if (!IsLooseLootRenderer(loot, renderer))
                            continue;

                        AddWorldChamRenderer(
                            renderer, WorldChamKind.Loot, loot);
                    }
                }
            }

            _staleWorldChamIds.Clear();
            foreach (KeyValuePair<int, WorldChamState> pair
                     in _worldChamStates)
            {
                WorldChamState state = pair.Value;
                if (state.Kind != WorldChamKind.Loot)
                    continue;

                if (state.LootSource != null &&
                    _seenLootIds.Contains(
                        state.LootSource.GetInstanceID()))
                    continue;

                _staleWorldChamIds.Add(pair.Key);
            }

            for (int i = 0; i < _staleWorldChamIds.Count; i++)
            {
                RestoreWorldChamState(
                    _worldChamStates[_staleWorldChamIds[i]]);
                _worldChamStates.Remove(_staleWorldChamIds[i]);
            }

            if (_staleWorldChamIds.Count > 0)
                _worldChamPassDirty = true;

            _knownLootIds.IntersectWith(_seenLootIds);
        }

        private static bool IsLooseWorldLoot(
            EFT.Interactive.LootItem loot)
        {
            return loot != null &&
                   !(loot is EFT.Interactive.Corpse) &&
                   loot.isActiveAndEnabled &&
                   loot.gameObject.activeInHierarchy &&
                   loot.Item != null &&
                   loot.ItemOwner != null &&
                   loot.GetComponentInParent<Player>() == null;
        }

        private static bool IsLooseLootRenderer(
            EFT.Interactive.LootItem loot,
            Renderer renderer)
        {
            return loot != null &&
                   renderer != null &&
                   loot.isActiveAndEnabled &&
                   loot.gameObject.activeInHierarchy &&
                   renderer.gameObject.activeInHierarchy &&
                   (renderer.transform == loot.transform ||
                    renderer.transform.IsChildOf(loot.transform)) &&
                   renderer.GetComponentInParent<Player>() == null;
        }

        private void AddWorldChamRenderer(
            Renderer renderer,
            WorldChamKind kind,
            EFT.Interactive.LootItem lootSource = null)
        {
            if (renderer == null ||
                (!(renderer is MeshRenderer) &&
                 !(renderer is SkinnedMeshRenderer)))
                return;

            if (kind == WorldChamKind.Loot &&
                renderer.GetComponentInParent<Player>() != null)
                return;

            int id = renderer.GetInstanceID();
            if (_worldChamStates.ContainsKey(id))
                return;

            Material[] originals = renderer.sharedMaterials;
            _worldChamStates.Add(id, new WorldChamState
            {
                Renderer = renderer,
                Kind = kind,
                LootSource = lootSource,
                OriginalMaterials = originals,
                ChamMaterials = FilledMaterials(
                    Mathf.Max(1, originals.Length),
                    _worldChamMaterials[(int)kind].Material)
            });
            _worldChamPassDirty = true;
        }

        private void RestoreWorldChamState(WorldChamState state)
        {
            if (state.Renderer == null || !state.Applied)
                return;

            if (state.Renderer.sharedMaterial ==
                state.ChamMaterials[0])
            {
                state.Renderer.sharedMaterials =
                    state.OriginalMaterials;
            }

            state.Applied = false;
        }

        private void UpdateVegetationCulling()
        {
            if (!_cullGrass.Value)
            {
                RestoreVegetationCulling();
                return;
            }

            if (Time.unscaledTime >= _nextVegetationManagerScan)
            {
                bool pending = DisableVegetationManagers();
                _nextVegetationManagerScan =
                    Time.unscaledTime +
                    (pending ? 2f : 30f);
            }
        }

        private bool DisableVegetationManagers()
        {
            bool pending = false;
            GPUInstancerDetailManager[] managers =
                UnityEngine.Object.FindObjectsOfType<
                    GPUInstancerDetailManager>();
            for (int i = 0; i < managers.Length; i++)
            {
                GPUInstancerDetailManager manager = managers[i];
                if (manager == null)
                    continue;
                if (!manager.isInitialized)
                {
                    pending = true;
                    continue;
                }
                int id = manager.GetInstanceID();
                if (_knownVegetationManagers.Add(id))
                {
                    _vegetationManagerStates.Add(
                        new VegetationManagerState
                    {
                        Manager = manager,
                        WasEnabled = manager.enabled
                    });
                }
                if (manager.enabled)
                    manager.enabled = false;
            }
            return pending;
        }

        private void RestoreWorldChams()
        {
            foreach (WorldChamState state
                     in _worldChamStates.Values)
                RestoreWorldChamState(state);
        }

        private void RestoreVegetationCulling()
        {
            for (int i = 0; i < _vegetationManagerStates.Count; i++)
            {
                VegetationManagerState state =
                    _vegetationManagerStates[i];
                if (state.Manager == null)
                    continue;
                state.Manager.enabled = state.WasEnabled;
            }
            _vegetationManagerStates.Clear();
            _knownVegetationManagers.Clear();
            _nextVegetationManagerScan = 0f;
        }

        private ChamMaterialSet GetChamMaterials(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return _pmcChamMaterials;
                case EspKind.Scav:
                    return _scavChamMaterials;
                case EspKind.Boss:
                    return _bossChamMaterials;
                default:
                    return null;
            }
        }

        private void DisposeChams()
        {
            RestoreAllChams();
            RestoreVegetationCulling();
            _chamsActive = false;

            if (_pmcChamMaterials != null)
            {
                _pmcChamMaterials.Dispose();
                _scavChamMaterials.Dispose();
                _bossChamMaterials.Dispose();
                _pmcChamMaterials = null;
                _scavChamMaterials = null;
                _bossChamMaterials = null;
            }

            for (int i = 0; i < _espRoles.Count; i++)
            {
                EspRoleSettings role = _espRoles[i];
                if (role.ChamMaterials == null)
                    continue;
                role.ChamMaterials.Dispose();
                role.ChamMaterials = null;
                role.LastChamVisible = null;
                role.LastChamHidden = null;
                role.LastChamOpacity = -1f;
            }

            if (_worldChamMaterials != null)
            {
                for (int i = 0; i < _worldChamMaterials.Length; i++)
                    _worldChamMaterials[i].Dispose();

                _worldChamMaterials = null;
            }

            _worldChamStates.Clear();
            _seenLootIds.Clear();
            _knownLootIds.Clear();
            _seenCorpseIds.Clear();
            _knownCorpseIds.Clear();
            _staleWorldChamIds.Clear();
            _vegetationManagerStates.Clear();
            _knownVegetationManagers.Clear();
        }

    }
}
