
namespace FieldKit
{
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private void EnsureLootCatalog()
        {
            ItemUiContext context = ItemUiContext.Instance;

            if (context == null)
                return;

            HandbookClass handbook = context.Handbook;
            ItemFactoryClass itemFactory =
                Singleton<ItemFactoryClass>.Instance;
            RagFairClass ragfair =
                context.Session == null ? null : context.Session.RagFair;

            if (handbook != null &&
                itemFactory != null &&
                (handbook != _lootHandbook ||
                 itemFactory != _lootItemFactory))
            {
                _lootNameRefreshAttempts = 0;
                BuildLootCatalog(handbook, itemFactory);
            }
            else if (handbook != null &&
                     itemFactory != null &&
                     _menuOpen &&
                     _menuTab == 2 &&
                     _lootMenuSection == 1 &&
                     _lootCatalogHasUnresolvedNames &&
                     _lootNameRefreshAttempts < 10 &&
                     Time.unscaledTime >= _nextLootNameRefresh)
                BuildLootCatalog(handbook, itemFactory);

            if (ragfair == _lootRagfair)
                return;

            if (_lootRagfair != null)
                _lootRagfair.OnNodePriceUpdated -= OnLootPricesUpdated;

            _lootRagfair = ragfair;
            _lootPriceRefreshRequested = false;

            if (_lootRagfair == null)
                return;

            _lootRagfair.OnNodePriceUpdated += OnLootPricesUpdated;

            try
            {
                _lootRagfair.RefreshItemPrices();
                _lootPriceRefreshRequested = true;
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Flea price refresh failed: " + exception.Message);
            }
        }

        private void BuildLootCatalog(
            HandbookClass handbook,
            ItemFactoryClass itemFactory)
        {
            HashSet<string> expandedCategoryIds =
                new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _lootRoots.Count; i++)
            {
                CollectExpandedLootCategories(
                    _lootRoots[i],
                    expandedCategoryIds);
            }

            _lootHandbook = handbook;
            _lootItemFactory = itemFactory;
            _lootItemCount = 0;
            _lootHandbookItemCount = 0;
            _lootRoots.Clear();
            _lootCategories.Clear();
            _lootCatalogItems.Clear();
            InvalidateLootCaches();
            _lootCatalogHasUnresolvedNames = false;
            _nextLootNameRefresh = Time.unscaledTime + 2f;
            _lootNameRefreshAttempts++;

            HandbookData[] categories = handbook.Categories;

            if (categories != null)
            {
                for (int i = 0; i < categories.Length; i++)
                {
                    HandbookData data = categories[i];

                    if (data == null || string.IsNullOrEmpty(data.Id))
                        continue;

                    string categoryName = CatalogName(data);
                    if (IsUnresolvedLootName(categoryName))
                        _lootCatalogHasUnresolvedNames = true;

                    _lootCategories[data.Id] = new LootCategory
                    {
                        Id = data.Id,
                        ParentId = data.ParentId,
                        Name = categoryName,
                        Order = data.Order,
                        Expanded =
                            expandedCategoryIds.Contains(data.Id)
                    };
                }
            }

            foreach (LootCategory category in _lootCategories.Values)
            {
                LootCategory parent;

                if (!string.IsNullOrEmpty(category.ParentId) &&
                    _lootCategories.TryGetValue(
                        category.ParentId, out parent))
                    parent.Children.Add(category);
                else
                    _lootRoots.Add(category);
            }

            Dictionary<string, HandbookData> handbookItems =
                new Dictionary<string, HandbookData>(4096);
            HandbookData[] items = handbook.Items;

            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    HandbookData data = items[i];

                    if (data != null && !string.IsNullOrEmpty(data.Id))
                        handbookItems[data.Id] = data;
                }
            }

            LootCategory otherRoot = new LootCategory
            {
                Id = "internal-items",
                Name = "Other in-game items",
                Order = int.MaxValue,
                Expanded =
                    expandedCategoryIds.Contains("internal-items")
            };
            Dictionary<string, LootCategory> otherCategories =
                new Dictionary<string, LootCategory>(128);

            foreach (ItemTemplate template in itemFactory.ItemTemplates.Values)
            {
                if (template == null || template._type != NodeType.Item)
                    continue;

                string id = template.StringId;

                if (string.IsNullOrEmpty(id))
                    continue;

                HandbookData handbookData;
                LootCategory parent;
                float basePrice = template.CreditsPrice;

                if (handbookItems.TryGetValue(id, out handbookData) &&
                    _lootCategories.TryGetValue(
                        handbookData.ParentId, out parent))
                {
                    basePrice = handbookData.Price;
                    _lootHandbookItemCount++;
                }
                else
                {
                    ItemTemplate parentTemplate = template.Parent;
                    string parentId = parentTemplate == null
                        ? "uncategorized"
                        : parentTemplate.StringId;

                    if (string.IsNullOrEmpty(parentId))
                        parentId = "uncategorized";

                    if (!otherCategories.TryGetValue(
                        parentId, out parent))
                    {
                        string parentName = parentTemplate == null
                            ? "Uncategorized"
                            : CatalogName(parentTemplate);
                        if (IsUnresolvedLootName(parentName))
                            _lootCatalogHasUnresolvedNames = true;

                        parent = new LootCategory
                        {
                            Id = "internal:" + parentId,
                            ParentId = otherRoot.Id,
                            Name = parentName,
                            Order = otherCategories.Count,
                            Expanded = expandedCategoryIds.Contains(
                                "internal:" + parentId)
                        };
                        otherCategories[parentId] = parent;
                        otherRoot.Children.Add(parent);
                    }
                }

                string itemName = CatalogName(template);

                LootCatalogItem catalogItem = new LootCatalogItem
                {
                    Id = id,
                    Name = itemName,
                    BasePrice = basePrice,
                    IsQuestItem = template.QuestItem,
                    CanSellOnFlea = template.CanSellOnRagfair
                };
                parent.Items.Add(catalogItem);
                _lootCatalogItems[id] = catalogItem;
                _lootItemCount++;
            }

            if (otherRoot.TotalItems > 0 ||
                otherRoot.Children.Count > 0 ||
                otherRoot.Items.Count > 0)
                _lootRoots.Add(otherRoot);

            _lootRoots.Sort(CompareLootCategories);

            for (int i = 0; i < _lootRoots.Count; i++)
                SortLootCategory(_lootRoots[i]);
            _lootSelectionCountsDirty = true;
        }

        private static void CollectExpandedLootCategories(
            LootCategory category,
            HashSet<string> expandedCategoryIds)
        {
            if (category == null)
                return;

            if (category.Expanded &&
                !string.IsNullOrEmpty(category.Id))
            {
                expandedCategoryIds.Add(category.Id);
            }

            for (int i = 0; i < category.Children.Count; i++)
            {
                CollectExpandedLootCategories(
                    category.Children[i],
                    expandedCategoryIds);
            }
        }

        private static string CatalogName(ItemTemplate template)
        {
            string id = template.StringId;
            string[] keys =
            {
                template.NameLocalizationKey,
                id,
                id + " Name",
                template.ShortNameLocalizationKey,
                id + " ShortName"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                string localized = TryLocalize(keys[i]);

                if (IsResolvedCatalogName(localized, keys[i], id))
                    return localized;
            }

            if (!string.IsNullOrWhiteSpace(template.Name) &&
                !IsInteger(template.Name))
                return HumanizeInternalName(template.Name);

            if (!string.IsNullOrWhiteSpace(template._name) &&
                !IsInteger(template._name))
                return HumanizeInternalName(template._name);

            return "Unknown [" + id + "]";
        }

        private static string CatalogName(HandbookData data)
        {
            string localized = TryLocalize(data.Id);

            if (IsResolvedCatalogName(localized, data.Id, data))
                return localized;

            string nameKey = data.Id + " Name";
            localized = TryLocalize(nameKey);

            if (IsResolvedCatalogName(localized, nameKey, data))
                return localized;

            try
            {
                localized = data.Item == null
                    ? null
                    : EFT.LocalizationExtensions.LocalizedName(data.Item);

                if (IsResolvedCatalogName(localized, nameKey, data))
                    return localized;
            }
            catch { }

            string shortNameKey = data.Id + " ShortName";
            localized = TryLocalize(shortNameKey);

            if (IsResolvedCatalogName(localized, shortNameKey, data))
                return localized;

            localized = TryLocalize(data.Name);

            if (IsResolvedCatalogName(localized, data.Name, data))
                return localized;

            if (!string.IsNullOrWhiteSpace(data.Name) &&
                !IsInteger(data.Name))
                return data.Name;

            return "Unknown [" + data.Id + "]";
        }

        private static string HumanizeInternalName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            StringBuilder result = new StringBuilder(value.Length + 8);

            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];

                if (current == '_' || current == '-')
                {
                    if (result.Length > 0 &&
                        result[result.Length - 1] != ' ')
                        result.Append(' ');

                    continue;
                }

                if (i > 0 &&
                    char.IsUpper(current) &&
                    value[i - 1] != '_' &&
                    value[i - 1] != '-' &&
                    (char.IsLower(value[i - 1]) ||
                     char.IsDigit(value[i - 1]) ||
                     (i + 1 < value.Length &&
                      char.IsLower(value[i + 1]))))
                    result.Append(' ');

                result.Append(current);
            }

            return result.ToString().Trim();
        }

        private static string TryLocalize(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            try
            {
                return EFT.LocalizationExtensions.Localized(key, null);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsResolvedCatalogName(
            string value,
            string requestedKey,
            HandbookData data)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(
                    value, requestedKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    value, data.Id, StringComparison.OrdinalIgnoreCase) ||
                IsInteger(value))
                return false;

            return string.IsNullOrWhiteSpace(data.Name) ||
                !string.Equals(
                    value, data.Name, StringComparison.OrdinalIgnoreCase) ||
                !IsInteger(data.Name);
        }

        private static bool IsResolvedCatalogName(
            string value,
            string requestedKey,
            string id)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                !string.Equals(
                    value, requestedKey, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    value, id, StringComparison.OrdinalIgnoreCase) &&
                !IsInteger(value);
        }

        private static bool IsInteger(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            int index = value[0] == '-' || value[0] == '+' ? 1 : 0;

            if (index == value.Length)
                return false;

            for (; index < value.Length; index++)
            {
                if (!char.IsDigit(value[index]))
                    return false;
            }

            return true;
        }

        private static int CompareLootCategories(
            LootCategory left,
            LootCategory right)
        {
            int order = left.Order.CompareTo(right.Order);
            return order != 0
                ? order
                : string.Compare(
                    left.Name,
                    right.Name,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static void SortLootCategory(LootCategory category)
        {
            category.Children.Sort(CompareLootCategories);
            category.Items.Sort((left, right) => string.Compare(
                left.Name,
                right.Name,
                StringComparison.OrdinalIgnoreCase));
            category.TotalItems = category.Items.Count;

            for (int i = 0; i < category.Children.Count; i++)
            {
                SortLootCategory(category.Children[i]);
                category.TotalItems += category.Children[i].TotalItems;
            }
        }

        private void OnLootPricesUpdated(
            Dictionary<string, float> prices)
        {
            if (prices == null)
                return;

            _lootPriceRefreshRequested = false;

            foreach (KeyValuePair<string, float> pair in prices)
                _lootPrices[pair.Key] = pair.Value;

            InvalidateLootCaches();
        }

        private void DetachLootCatalog()
        {
            if (_lootRagfair != null)
                _lootRagfair.OnNodePriceUpdated -= OnLootPricesUpdated;

            _lootRagfair = null;
            _lootItemFactory = null;
        }

    }
}
