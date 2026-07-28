
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void BeginLootEspEntryBuild(
            Vector3 localPosition)
        {
            _lootWorldCacheDirty = false;
            _lootEntryBuildActive = true;
            _lootEntryBuildCursor = 0;
            _lootEspEntries.Clear();
            _lootEspMinimumPrice = float.MaxValue;
            _lootEspMaximumPrice = 0f;

            RefreshLooseWorldLootItems();
            _looseWorldLootItems.Sort(
                (left, right) =>
                    ((left == null
                            ? float.MaxValue
                            : (left.transform.position -
                               localPosition).sqrMagnitude))
                    .CompareTo(
                        right == null
                            ? float.MaxValue
                            : (right.transform.position -
                               localPosition).sqrMagnitude));
        }

        private void AdvanceLootEspEntryBuild(
            Vector3 localPosition)
        {
            if (!_lootEntryBuildActive)
                return;

            const int itemBudgetPerFrame = 10;
            int stop = Mathf.Min(
                _lootEntryBuildCursor + itemBudgetPerFrame,
                _looseWorldLootItems.Count);
            for (; _lootEntryBuildCursor < stop;
                 _lootEntryBuildCursor++)
            {
                LootItem loot =
                    _looseWorldLootItems[_lootEntryBuildCursor];
                if (!IsLooseWorldLoot(loot) || loot.Item == null)
                    continue;

                string id = loot.Item.TemplateId;
                LootCatalogItem item;
                _lootCatalogItems.TryGetValue(id, out item);
                float price = GetLootPrice(
                    id, item == null ? 0f : item.BasePrice);
                bool priceMatch = IsLootPriceMatch(id, price);
                if (!_lootSelectedItems.Contains(id) && !priceMatch)
                    continue;

                List<Renderer> renderers = LootRenderersField == null
                    ? null
                    : LootRenderersField.GetValue(loot)
                        as List<Renderer>;
                _lootEspEntries.Add(new LootEspEntry
                {
                    Loot = loot,
                    Item = item,
                    Price = price,
                    PriceMatch = priceMatch,
                    Renderers = renderers,
                    MarkerPosition = GetLootMarkerWorldPosition(
                        loot, renderers),
                    IsQuestItem = item != null
                        ? item.IsQuestItem
                        : loot.Item.QuestItem
                });
                if (price > 0f)
                {
                    _lootEspMinimumPrice = Mathf.Min(
                        _lootEspMinimumPrice, price);
                    _lootEspMaximumPrice = Mathf.Max(
                        _lootEspMaximumPrice, price);
                }
            }

            if (_lootEntryBuildCursor <
                _looseWorldLootItems.Count)
                return;

            _lootEntryBuildActive = false;
            if (_lootEspMinimumPrice == float.MaxValue)
                _lootEspMinimumPrice = 0f;

            long perfStarted = PerfTimestamp();
            try
            {
                BuildLootEspClusters(localPosition);
            }
            finally
            {
                RecordPerf(
                    perfStarted,
                    ref _perfCacheBuildTicks,
                    ref _perfCacheBuildCalls,
                    ref _perfCacheBuildMaxTicks);
            }
        }

        private void RefreshLooseWorldLootItems()
        {
            _looseWorldLootItems.Clear();
            if (_world == null || _world.LootItems == null)
                return;

            int count = _world.LootItems.Count;
            for (int i = 0; i < count; i++)
            {
                LootItem loot = _world.LootItems.GetByIndex(i);
                if (IsLooseWorldLoot(loot))
                    _looseWorldLootItems.Add(loot);
            }
        }

    }
}
