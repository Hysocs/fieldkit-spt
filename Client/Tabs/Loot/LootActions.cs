
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private async void SpawnLootItemAtFeet(
            LootCatalogItem catalogItem,
            int amount)
        {
            if (_world == null || _localPlayer == null)
            {
                ShowLootActionMessage(
                    "Spawn item at feet is only available during a raid.",
                    true);
                return;
            }

            if (!_lootItemActionsInProgress.Add(catalogItem.Id))
                return;

            try
            {
                Item firstItem =
                    CreateLootCatalogItem(catalogItem.Id);
                await EnsureLootItemResourcesLoaded(firstItem);

                Vector3 forward = _localPlayer.Transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f)
                    forward = Vector3.forward;
                else
                    forward.Normalize();

                Vector3 groundProbe =
                    _localPlayer.Transform.position +
                    forward * 0.55f +
                    Vector3.up * 1.5f;
                Vector3 spawnPosition =
                    _localPlayer.Transform.position +
                    forward * 0.55f +
                    Vector3.up * 0.12f;
                RaycastHit groundHit;
                if (Physics.Raycast(
                    groundProbe,
                    Vector3.down,
                    out groundHit,
                    4f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    spawnPosition =
                        groundHit.point + Vector3.up * 0.12f;
                }

                for (int index = 0; index < amount; index++)
                {
                    Item item = index == 0
                        ? firstItem
                        : CreateLootCatalogItem(catalogItem.Id);
                    Vector3 offset = new Vector3(
                        (index % 5) * 0.08f,
                        (index / 25) * 0.04f,
                        ((index / 5) % 5) * 0.08f);
                    EFT.Interactive.LootItem spawned = _world.ThrowItem(
                        item,
                        _localPlayer,
                        spawnPosition + offset,
                        Quaternion.Euler(
                            0f,
                            _localPlayer.Transform.eulerAngles.y,
                            0f),
                        Vector3.zero,
                        Vector3.zero,
                        true,
                        true,
                        0f);
                    if (spawned == null)
                        throw new InvalidOperationException(
                            "EFT did not create a loose-loot object.");
                }

                ShowLootActionMessage(
                    "Spawned " + amount + "x " +
                    catalogItem.Name + " at your feet.");
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not spawn loot item " + catalogItem.Id +
                    ": " + exception);
                ShowLootActionMessage(
                    "Could not spawn " + catalogItem.Name +
                    ". This item may not have a world prefab.",
                    true);
            }
            finally
            {
                _lootItemActionsInProgress.Remove(catalogItem.Id);
            }
        }

        private static async Task EnsureLootItemResourcesLoaded(
            Item item)
        {
            PoolManagerClass poolManager =
                Singleton<PoolManagerClass>.Instance;
            if (poolManager == null ||
                item == null ||
                item.Template == null)
            {
                throw new InvalidOperationException(
                    "EFT's item asset pool is not ready.");
            }

            ResourceKey[] resources =
                item.Template.AllResources
                    .Where(resource =>
                        resource != ResourceKey.EmptyResourceKey)
                    .Distinct()
                    .ToArray();
            if (resources.Length == 0)
                return;

            await poolManager.LoadBundlesAndCreatePools(
                PoolManagerClass.PoolsCategory.Raid,
                PoolManagerClass.AssemblyType.Online,
                resources,
                JobPriorityClass.Immediate,
                null,
                CancellationToken.None);
        }

        private async void AddLootItemToInventory(
            LootCatalogItem catalogItem,
            int amount)
        {
            if (!_lootItemActionsInProgress.Add(catalogItem.Id))
                return;

            try
            {
                LootServerAddResult result =
                    await RequestServerStashAdd(
                        catalogItem.Id,
                        amount);
                if (result.Success)
                {
                    ShowLootActionMessage(
                        amount + "x " + catalogItem.Name +
                        " was sent to your messages.");
                    return;
                }

                ShowLootActionMessage(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Could not deliver " + catalogItem.Name + "."
                        : result.Message,
                    true);
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not add loot item " + catalogItem.Id +
                    ": " + exception);
                ShowLootActionMessage(
                    "Could not add " + catalogItem.Name +
                    ". The server delivery request failed.",
                    true);
            }
            finally
            {
                _lootItemActionsInProgress.Remove(catalogItem.Id);
            }
        }

        private async void AddLootItemDirect(
            LootCatalogItem catalogItem,
            int amount,
            bool carriedInventory)
        {
            if (!_lootItemActionsInProgress.Add(catalogItem.Id))
                return;

            try
            {
                InventoryController controller =
                    GetLiveInventoryController();
                if (controller == null)
                    throw new InvalidOperationException(
                        "The live inventory is not ready.");

                int added = 0;
                for (int index = 0; index < amount; index++)
                {
                    bool success = carriedInventory
                        ? await TryAddLootItemToCarriedInventory(
                            catalogItem.Id,
                            controller)
                        : await TryAddLootItemToStash(
                            catalogItem.Id,
                            controller);
                    if (!success)
                        break;
                    added++;
                }

                if (added == amount)
                {
                    controller.ReportProfileUpdate();
                    ShowLootActionMessage(
                        "Added " + amount + "x " +
                        catalogItem.Name +
                        (carriedInventory
                            ? " to your inventory."
                            : " to your stash."));
                    return;
                }

                ShowLootActionMessage(
                    added == 0
                        ? "Could not place " + catalogItem.Name +
                          ". There may not be enough room."
                        : "Added " + added + " of " + amount + " " +
                          catalogItem.Name +
                          "; no more items would fit.",
                    true);
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not directly add loot item " +
                    catalogItem.Id + ": " + exception);
                ShowLootActionMessage(
                    "Could not directly add " + catalogItem.Name +
                    ". You can still use Send in mail.",
                    true);
            }
            finally
            {
                _lootItemActionsInProgress.Remove(catalogItem.Id);
            }
        }

        private InventoryController GetLiveInventoryController()
        {
            ItemUiContext itemUiContext = ItemUiContext.Instance;
            if (itemUiContext != null &&
                ItemUiInventoryControllerField != null)
            {
                InventoryController uiController =
                    ItemUiInventoryControllerField.GetValue(itemUiContext)
                        as InventoryController;
                if (uiController != null)
                    return uiController;
            }

            if (_localPlayer != null &&
                _localPlayer.InventoryController != null)
            {
                return _localPlayer.InventoryController;
            }

            TarkovApplication application =
                Singleton<TarkovApplication>.Instance;
            if (application == null)
            {
                application =
                    UnityEngine.Object
                        .FindObjectOfType<TarkovApplication>();
            }
            if (application == null || MainMenuControllerField == null)
                return null;

            MainMenuControllerClass mainMenuController =
                MainMenuControllerField.GetValue(application)
                    as MainMenuControllerClass;
            return mainMenuController == null
                ? null
                : mainMenuController.InventoryController;
        }

        private async Task<bool> TryAddLootItemToStash(
            string templateId,
            InventoryController controller)
        {
            StashItemClass stash = controller.Inventory == null
                ? null
                : controller.Inventory.Stash;
            if (stash == null)
                return false;

            return await TryPlaceNewLootItem(
                templateId,
                new CompoundItem[] { stash },
                controller);
        }

        private async Task<bool> TryAddLootItemToCarriedInventory(
            string templateId,
            InventoryController controller)
        {
            InventoryEquipment equipment = controller.Inventory == null
                ? null
                : controller.Inventory.Equipment;
            if (equipment == null)
                return false;

            List<CompoundItem> targets = new List<CompoundItem>(4);
            AddLootInventoryTarget(
                targets, equipment, EquipmentSlot.Backpack);
            AddLootInventoryTarget(
                targets, equipment, EquipmentSlot.TacticalVest);
            AddLootInventoryTarget(
                targets, equipment, EquipmentSlot.Pockets);
            AddLootInventoryTarget(
                targets, equipment, EquipmentSlot.SecuredContainer);
            if (targets.Count == 0)
                return false;

            return await TryPlaceNewLootItem(
                templateId,
                targets,
                controller);
        }

        private static void AddLootInventoryTarget(
            List<CompoundItem> targets,
            InventoryEquipment equipment,
            EquipmentSlot slotName)
        {
            Slot slot = equipment.GetSlot(slotName);
            CompoundItem target =
                slot == null ? null : slot.ContainedItem as CompoundItem;
            if (target != null)
                targets.Add(target);
        }

        private async Task<bool> TryPlaceNewLootItem(
            string templateId,
            IEnumerable<CompoundItem> targets,
            InventoryController controller)
        {
            Item item = CreateLootCatalogItem(templateId);
            item.CurrentAddress = controller.CreateItemAddress();
            GStruct154<GInterface424> placement =
                InteractionsHandlerClass.QuickFindAppropriatePlace(
                    item,
                    controller,
                    targets,
                    InteractionsHandlerClass.EMoveItemOrder.TryTransfer |
                    InteractionsHandlerClass.EMoveItemOrder
                        .PrioritizeTargetsOrder |
                    InteractionsHandlerClass.EMoveItemOrder
                        .IgnoreItemParent,
                    true);
            if (placement.Failed)
                return false;

            LootServerAddResult prepareResult =
                await RequestServerPrepareItem(
                    templateId,
                    item.Id.ToString());
            if (!prepareResult.Success)
            {
                if (!string.IsNullOrWhiteSpace(prepareResult.Message))
                    LogSource.LogWarning(prepareResult.Message);
                return false;
            }

            GStruct153 operation = placement;
            IResult result =
                await controller.TryRunNetworkTransaction(
                    operation,
                    null);
            if (result == null || result.Failed)
            {
                await RequestServerCancelPreparedItem(
                    item.Id.ToString());
                if (result != null &&
                    !string.IsNullOrWhiteSpace(result.Error))
                {
                    LogSource.LogWarning(
                        "Inventory placement failed: " + result.Error);
                }
                return false;
            }

            return true;
        }

        private Item CreateLootCatalogItem(string templateId)
        {
            ItemFactoryClass itemFactory = _lootItemFactory ??
                Singleton<ItemFactoryClass>.Instance;
            if (itemFactory == null)
                throw new InvalidOperationException(
                    "The EFT item factory is not ready.");

            return itemFactory.CreateItem(
                MongoID.Generate(false).ToString(),
                templateId,
                null);
        }

        private static async Task<LootServerAddResult>
            RequestServerStashAdd(string templateId, int amount)
        {
            string request = JsonConvert.SerializeObject(
                new { TemplateId = templateId, Amount = amount });
            string json = await RequestHandler.PostJsonAsync(
                "/fieldkit/inventory/add",
                request);
            JToken root = JToken.Parse(json);
            JToken data = root["data"] ?? root;
            return new LootServerAddResult
            {
                Success = data.Value<bool?>("success") ??
                          data.Value<bool?>("Success") ??
                          false,
                NoSpace = data.Value<bool?>("noSpace") ??
                          data.Value<bool?>("NoSpace") ??
                          false,
                Message = data.Value<string>("message") ??
                          data.Value<string>("Message")
            };
        }

        private static async Task<LootServerAddResult>
            RequestServerPrepareItem(
                string templateId,
                string itemId)
        {
            string request = JsonConvert.SerializeObject(
                new { TemplateId = templateId, ItemId = itemId });
            return ParseLootServerResult(
                await RequestHandler.PostJsonAsync(
                    "/fieldkit/inventory/prepare",
                    request));
        }

        private static async Task<LootServerAddResult>
            RequestServerCancelPreparedItem(string itemId)
        {
            string request = JsonConvert.SerializeObject(
                new { ItemId = itemId });
            return ParseLootServerResult(
                await RequestHandler.PostJsonAsync(
                    "/fieldkit/inventory/cancel",
                    request));
        }

        private static LootServerAddResult ParseLootServerResult(
            string json)
        {
            JToken root = JToken.Parse(json);
            JToken data = root["data"] ?? root;
            return new LootServerAddResult
            {
                Success = data.Value<bool?>("success") ??
                          data.Value<bool?>("Success") ??
                          false,
                NoSpace = data.Value<bool?>("noSpace") ??
                          data.Value<bool?>("NoSpace") ??
                          false,
                Message = data.Value<string>("message") ??
                          data.Value<string>("Message")
            };
        }

        private static void ShowLootActionMessage(
            string message,
            bool warning = false)
        {
            try
            {
                if (warning)
                {
                    NotificationManagerClass.DisplayWarningNotification(
                        message,
                        ENotificationDurationType.Long);
                }
                else
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        message,
                        ENotificationDurationType.Default,
                        ENotificationIconType.Note,
                        null);
                }
            }
            catch
            {
                if (warning)
                    LogSource.LogWarning(message);
                else
                    LogSource.LogInfo(message);
            }
        }

        private sealed class LootServerAddResult
        {
            public bool Success;
            public bool NoSpace;
            public string Message;
        }

    }
}
