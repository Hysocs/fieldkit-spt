
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private bool LootCategoryHasVisibleItems(LootCategory category)
        {
            for (int i = 0; i < category.Items.Count; i++)
            {
                if (ShouldListLootItem(category.Items[i]))
                    return true;
            }
            for (int i = 0; i < category.Children.Count; i++)
            {
                if (LootCategoryHasVisibleItems(category.Children[i]))
                    return true;
            }
            return false;
        }

        private bool ShouldListLootItem(LootCatalogItem item)
        {
            if (_lootSelectedOnly &&
                !_lootSelectedItems.Contains(item.Id))
                return false;
            return LootTextMatches(item.Name) ||
                   LootTextMatches(item.Id);
        }

        private bool LootTextMatches(string value)
        {
            return string.IsNullOrWhiteSpace(_lootSearch) ||
                   (!string.IsNullOrEmpty(value) &&
                    value.IndexOf(
                        _lootSearch,
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private int SelectedLootCount(LootCategory category)
        {
            return category == null ? 0 : category.SelectedItems;
        }

        private void RefreshLootSelectionCounts()
        {
            if (!_lootSelectionCountsDirty)
                return;

            for (int i = 0; i < _lootRoots.Count; i++)
                CountSelectedLootItems(_lootRoots[i]);
            _lootSelectionCountsDirty = false;
        }

        private int CountSelectedLootItems(LootCategory category)
        {
            int count = 0;
            for (int i = 0; i < category.Items.Count; i++)
            {
                if (_lootSelectedItems.Contains(category.Items[i].Id))
                    count++;
            }
            for (int i = 0; i < category.Children.Count; i++)
                count += CountSelectedLootItems(category.Children[i]);
            category.SelectedItems = count;
            return count;
        }

        private void SetLootCategorySelected(
            LootCategory category,
            bool selected)
        {
            for (int i = 0; i < category.Items.Count; i++)
            {
                if (selected)
                    _lootSelectedItems.Add(category.Items[i].Id);
                else
                    _lootSelectedItems.Remove(category.Items[i].Id);
            }
            for (int i = 0; i < category.Children.Count; i++)
                SetLootCategorySelected(category.Children[i], selected);
        }

        private float GetLootPrice(string id, float fallback)
        {
            float flea;
            return !string.IsNullOrEmpty(id) &&
                   _lootPrices.TryGetValue(id, out flea) &&
                   flea > 0f
                ? flea
                : fallback;
        }

        private bool IsLootPriceMatch(
            string templateId,
            float price)
        {
            return _lootPriceRangeEnabled.Value &&
                   (!_lootPriceRangeSelectedOnly.Value ||
                    _lootSelectedItems.Contains(templateId)) &&
                   price >= _lootPriceMinimum.Value &&
                   price <= _lootPriceMaximum.Value;
        }

        private Color GetLootEspColor(bool priceMatch)
        {
            return priceMatch
                ? ParseVisualColor(
                    _lootEspPriceColor.Value,
                    new Color(0.98f, 0.80f, 0.08f, 1f))
                : ParseVisualColor(
                    _lootEspColor.Value,
                    new Color(0.13f, 0.83f, 0.93f, 1f));
        }

        private Color GetContainerEspColor()
        {
            return ParseVisualColor(
                _lootContainerEspColor.Value,
                new Color(0.98f, 0.57f, 0.24f, 1f));
        }

        private Color GetLowValueColor()
        {
            return ParseVisualColor(
                _lootLowValueColor.Value,
                new Color(0.58f, 0.64f, 0.72f, 1f));
        }

        private Color GetHighValueColor()
        {
            return ParseVisualColor(
                _lootHighValueColor.Value,
                new Color(0.94f, 0.27f, 0.27f, 1f));
        }

        private Color GetLowMidValueColor()
        {
            return ParseVisualColor(
                _lootLowMidValueColor.Value,
                new Color(0.22f, 0.74f, 0.97f, 1f));
        }

        private Color GetMidValueColor()
        {
            return ParseVisualColor(
                _lootMidValueColor.Value,
                new Color(0.13f, 0.77f, 0.37f, 1f));
        }

        private Color GetHighMidValueColor()
        {
            return ParseVisualColor(
                _lootHighMidValueColor.Value,
                new Color(0.98f, 0.80f, 0.08f, 1f));
        }

        private Color GetQuestItemColor()
        {
            return ParseVisualColor(
                _lootQuestItemColor.Value,
                new Color(0.75f, 0.52f, 0.99f, 1f));
        }

        private Color GetLootValueColor(
            float price,
            bool priceMatch,
            bool questItem)
        {
            if (questItem)
                return GetQuestItemColor();
            if (!_lootValueGradient.Value ||
                price <= 0f ||
                _lootEspMaximumPrice <= _lootEspMinimumPrice)
                return GetLootEspColor(priceMatch);

            float minimumLog =
                Mathf.Log10(_lootEspMinimumPrice + 1f);
            float maximumLog =
                Mathf.Log10(_lootEspMaximumPrice + 1f);
            float valueLog = Mathf.Log10(price + 1f);
            float ratio = Mathf.InverseLerp(
                minimumLog, maximumLog, valueLog);
            if (ratio < 0.25f)
                return Color.Lerp(
                    GetLowValueColor(),
                    GetLowMidValueColor(),
                    ratio * 4f);
            if (ratio < 0.5f)
                return Color.Lerp(
                    GetLowMidValueColor(),
                    GetMidValueColor(),
                    (ratio - 0.25f) * 4f);
            if (ratio < 0.75f)
                return Color.Lerp(
                    GetMidValueColor(),
                    GetHighMidValueColor(),
                    (ratio - 0.5f) * 4f);
            return Color.Lerp(
                GetHighMidValueColor(),
                GetHighValueColor(),
                (ratio - 0.75f) * 4f);
        }

    }
}
