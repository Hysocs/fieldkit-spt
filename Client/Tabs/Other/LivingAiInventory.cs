
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private static void AddLivingAiInteraction(
            GamePlayerOwner __instance)
        {
            Plugin plugin = _instance;
            if (plugin == null ||
                plugin._lootLivingAi == null ||
                !plugin._lootLivingAi.Value ||
                __instance == null ||
                __instance.Player == null ||
                !ReferenceEquals(
                    __instance.Player,
                    plugin._localPlayer))
                return;

            Player target = __instance.Player.InteractablePlayer;
            if (!plugin.IsValidLivingAiTarget(target))
                return;

            string targetName = plugin.GetLivingAiName(target);
            ActionsReturnClass interactionState =
                new ActionsReturnClass
                {
                    Actions = new List<ActionsTypesClass>
                    {
                        new ActionsTypesClass
                        {
                            Name = "Search",
                            TargetName = targetName,
                            Action = () =>
                                plugin.OpenLivingAiInventory(
                                    __instance,
                                    target)
                        }
                    }
                };

            interactionState.InitSelected();
            __instance.AvailableInteractionState.Value =
                interactionState;
        }

        private bool IsValidLivingAiTarget(Player target)
        {
            if (target == null ||
                target.IsYourPlayer ||
                target.AIData == null ||
                !target.AIData.IsAI ||
                target.InventoryController == null ||
                target.InventoryController.Inventory == null ||
                target.InventoryController.Inventory.Equipment == null)
                return false;

            try
            {
                return target.HealthController != null &&
                       target.HealthController.IsAlive;
            }
            catch
            {
                return false;
            }
        }

        private string GetLivingAiName(Player target)
        {
            try
            {
                if (target != null &&
                    target.Profile != null &&
                    !string.IsNullOrEmpty(
                        target.Profile.Nickname))
                    return target.Profile.Nickname;
            }
            catch { }

            return "Living AI";
        }

        private void OpenLivingAiInventory(
            GamePlayerOwner owner,
            Player target)
        {
            if (!_lootLivingAi.Value ||
                owner == null ||
                owner.Player == null ||
                !ReferenceEquals(owner.Player, _localPlayer) ||
                !IsValidLivingAiTarget(target))
                return;

            _livingLootOwner = owner;
            _livingLootTarget = target;
            _livingLootOpening = true;
            if (_holdLivingAiStill.Value)
                PauseLivingLootTarget();
            owner.Player.SaveInteractionRayInfo();
            ContinueOpeningLivingAiInventory();
        }

        private void ContinueOpeningLivingAiInventory()
        {
            Player target = _livingLootTarget;
            GamePlayerOwner owner = _livingLootOwner;
            if (!_livingLootOpening ||
                owner == null ||
                owner.Player == null ||
                target == null ||
                !IsValidLivingAiTarget(target))
            {
                ClearLivingAiLootSession();
                return;
            }

            InventoryEquipment equipment =
                target.InventoryController.Inventory.Equipment;
            _livingLootOpening = false;
            if (!CreateLivingAiLootProxy(
                target,
                equipment))
            {
                ClearLivingAiLootSession();
                return;
            }

            owner.Player.Interact(
                _livingLootProxyOwner,
                result =>
                {
                    if (result != null && result.Failed)
                    {
                        LogSource.LogWarning(
                            "Living-AI loot interaction failed: " +
                            result.Error);
                        ClearLivingAiLootSession();
                        return;
                    }

                    if (!IsValidLivingAiTarget(target))
                    {
                        ClearLivingAiLootSession();
                        return;
                    }

                    owner.ShowInventoryScreenLoot(
                        equipment,
                        ClearLivingAiLootSession,
                        false);
                });
        }

        private static void ProtectLivingAiHandsItem(
            Player.PlayerInventoryController __instance,
            Item __0,
            ref GStruct155 __result)
        {
            Plugin plugin = _instance;
            if (plugin == null ||
                plugin._livingLootTarget == null ||
                __0 == null ||
                __result.Failed)
                return;

            try
            {
                if (plugin._localPlayer == null ||
                    !ReferenceEquals(
                        __instance,
                        plugin._localPlayer.InventoryController))
                    return;

                Item handsItem =
                    plugin._livingLootTarget
                        .InventoryController
                        .ItemInHands;
                if (handsItem != null &&
                    ReferenceEquals(__0, handsItem))
                {
                    __result = new GStruct155(
                        new GClass1522(
                            "The AI's active hands item is visual-only and cannot be moved while held."));
                }
            }
            catch { }
        }

        private bool CreateLivingAiLootProxy(
            Player target,
            InventoryEquipment equipment)
        {
            if (target == null ||
                equipment == null ||
                target.InventoryController == null)
                return false;

            try
            {
                _livingLootEquipment = equipment;
                _livingLootOriginalRootAddress =
                    equipment.CurrentAddress;
                _livingLootOriginalController =
                    target.InventoryController;
                _livingLootOriginalControllerLocked =
                    target.InventoryController.Locked;

                _livingLootProxyOwner =
                    new TraderControllerClass(
                        equipment,
                        target.ProfileId,
                        GetLivingAiName(target),
                        false,
                        EOwnerType.Profile);
                _livingLootProxyOwner.AddItemEvent +=
                    ForwardLivingAiItemAdded;
                _livingLootProxyOwner.RemoveItemEvent +=
                    ForwardLivingAiItemRemoved;

                equipment.CurrentAddress =
                    _livingLootProxyOwner.CreateItemAddress();
                target.InventoryController.Locked = true;
                return true;
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not create living-AI loot owner: " +
                    exception.Message);
                RestoreLivingAiLootProxy();
                return false;
            }
        }

        private void ForwardLivingAiItemAdded(
            GEventArgs2 eventArgs)
        {
            try
            {
                if (_livingLootOriginalController != null)
                    _livingLootOriginalController.RaiseAddEvent(
                        eventArgs);
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not forward living-AI item addition: " +
                    exception.Message);
            }
        }

        private void ForwardLivingAiItemRemoved(
            GEventArgs3 eventArgs)
        {
            try
            {
                if (_livingLootOriginalController != null)
                    _livingLootOriginalController.RaiseRemoveEvent(
                        eventArgs);

            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not forward living-AI item removal: " +
                    exception.Message);
            }
        }

        private void HandleLivingAiRemoved(Player removed)
        {
            if (removed != null &&
                ReferenceEquals(removed, _livingLootTarget))
                CloseLivingAiInventory();
        }

        private void CloseLivingAiInventory()
        {
            GamePlayerOwner owner = _livingLootOwner;
            ClearLivingAiLootSession();

            if (owner == null ||
                owner.Player == null ||
                !owner.Player.IsInventoryOpened)
                return;

            try
            {
                owner.CloseInventoryIfOpen();
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not close living-AI inventory: " +
                    exception.Message);
            }
        }

        private void ClearLivingAiLootSession()
        {
            RestoreLivingAiLootProxy();
            ResumeLivingLootTarget();
            _livingLootTarget = null;
            _livingLootOwner = null;

            if (_localPlayer != null)
                _localPlayer.ForceInteractionsChanged();
        }

        private void RestoreLivingAiLootProxy()
        {
            TraderControllerClass proxyOwner =
                _livingLootProxyOwner;
            InventoryEquipment equipment =
                _livingLootEquipment;
            ItemAddress originalAddress =
                _livingLootOriginalRootAddress;
            InventoryController originalController =
                _livingLootOriginalController;
            bool originalLocked =
                _livingLootOriginalControllerLocked;

            _livingLootProxyOwner = null;
            _livingLootEquipment = null;
            _livingLootOriginalRootAddress = null;
            _livingLootOriginalController = null;
            _livingLootOriginalControllerLocked = false;
            _livingLootOpening = false;

            if (proxyOwner != null)
            {
                try
                {
                    proxyOwner.AddItemEvent -=
                        ForwardLivingAiItemAdded;
                    proxyOwner.RemoveItemEvent -=
                        ForwardLivingAiItemRemoved;
                }
                catch { }
            }

            try
            {
                if (equipment != null &&
                    proxyOwner != null &&
                    ReferenceEquals(
                        equipment.Owner,
                        proxyOwner))
                {
                    equipment.CurrentAddress =
                        originalAddress;
                }

                if (originalController != null)
                    originalController.Locked =
                        originalLocked;
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not restore living-AI loot owner: " +
                    exception.Message);
            }
        }

    }
}
