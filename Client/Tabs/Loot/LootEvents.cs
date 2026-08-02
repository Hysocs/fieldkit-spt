
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private static readonly HashSet<Type> PatchedLootCollectionTypes =
            new HashSet<Type>();
        private static bool _lootContainerLifecyclePatched;

        private void AttachLootWorldEvents()
        {
            if (_world == null)
                return;

            InstallLootCollectionMutationPatches();
            InstallLootContainerLifecyclePatches();
            DiscoverLootContainers();
            InvalidateLootCaches();
        }

        private void OnLootWorldStarted()
        {
            DiscoverLootContainers();
            _lootWorldCacheDirty = true;
        }

        private void InstallLootCollectionMutationPatches()
        {
            if (_harmony == null ||
                _world == null ||
                _world.LootItems == null)
                return;

            Type collectionType = _world.LootItems.GetType();
            if (!PatchedLootCollectionTypes.Add(collectionType))
                return;

            HarmonyMethod changed = new HarmonyMethod(
                AccessTools.Method(
                    typeof(Plugin),
                    nameof(OnLootCollectionMutated)));
            MethodInfo add = AccessTools.Method(
                collectionType,
                "Add",
                new[] { typeof(int), typeof(LootItem) });
            MethodInfo addOrReplace = AccessTools.Method(
                collectionType,
                "AddOrReplace",
                new[] { typeof(int), typeof(LootItem) });
            MethodInfo remove = AccessTools.Method(
                collectionType,
                "Remove",
                new[] { typeof(int) });
            MethodInfo clear = AccessTools.Method(
                collectionType,
                "Clear",
                Type.EmptyTypes);

            if (add != null)
                _harmony.Patch(add, postfix: changed);
            if (addOrReplace != null)
                _harmony.Patch(addOrReplace, postfix: changed);
            if (remove != null)
                _harmony.Patch(remove, postfix: changed);
            if (clear != null)
                _harmony.Patch(clear, postfix: changed);
        }

        private static void OnLootCollectionMutated()
        {
            if (_instance != null)
            {
                _instance._lootWorldCacheDirty = true;
                _instance._lootChamDiscoveryDirty = true;
                _instance._corpseChamDiscoveryDirty = true;
                _instance._perfLootInvalidations++;
            }
        }

        private void InstallLootContainerLifecyclePatches()
        {
            if (_harmony == null || _lootContainerLifecyclePatched)
                return;

            HarmonyMethod initialized = new HarmonyMethod(
                AccessTools.Method(
                    typeof(Plugin),
                    nameof(OnLootContainerInitialized)));
            MethodInfo init = AccessTools.Method(
                typeof(LootableContainer),
                "Init",
                new[] { typeof(TraderControllerClass) });
            if (init != null)
                _harmony.Patch(init, postfix: initialized);

            _lootContainerLifecyclePatched = true;
        }

        private static void OnLootContainerInitialized(
            LootableContainer __instance)
        {
            if (_instance != null)
                _instance.RegisterLootContainer(__instance);
        }

        private void OnWorldLootItemDestroyed(
            IKillableLootItem loot)
        {
            _lootWorldCacheDirty = true;
            _containerCacheDirty = true;
            _lootChamDiscoveryDirty = true;
            _perfLootInvalidations++;
        }

        private void DiscoverLootContainers()
        {
            DetachLootContainerEvents();
            _lootContainers.Clear();

            LootableContainer[] found =
                UnityEngine.Object.FindObjectsOfType<LootableContainer>();
            for (int i = 0; i < found.Length; i++)
            {
                LootableContainer container = found[i];
                if (container == null)
                    continue;

                RegisterLootContainer(container);
            }

            _containerCacheDirty = true;
        }

        private void RegisterLootContainer(
            LootableContainer container)
        {
            if (container == null)
                return;

            if (!_lootContainers.Contains(container))
                _lootContainers.Add(container);

            TraderControllerClass owner = container.ItemOwner;
            if (owner == null ||
                _lootContainerOwners.Contains(owner))
                return;

            owner.AddItemEvent += OnContainerItemAdded;
            owner.RemoveItemEvent += OnContainerItemRemoved;
            owner.RefreshItemEvent += OnContainerItemRefreshed;
            _lootContainerOwners.Add(owner);
            _containerCacheDirty = true;
        }

        private void DetachLootContainerEvents()
        {
            for (int i = 0; i < _lootContainerOwners.Count; i++)
            {
                TraderControllerClass owner = _lootContainerOwners[i];
                if (owner == null)
                    continue;

                owner.AddItemEvent -= OnContainerItemAdded;
                owner.RemoveItemEvent -= OnContainerItemRemoved;
                owner.RefreshItemEvent -= OnContainerItemRefreshed;
            }
            _lootContainerOwners.Clear();
        }

        private void OnContainerItemAdded(AddItemEventArgs args)
        {
            _containerCacheDirty = true;
            _perfContainerInvalidations++;
        }

        private void OnContainerItemRemoved(RemoveItemEventArgs args)
        {
            _containerCacheDirty = true;
            _perfContainerInvalidations++;
        }

        private void OnContainerItemRefreshed(RefreshItemEventArgs args)
        {
            _containerCacheDirty = true;
            _perfContainerInvalidations++;
        }
    }
}
