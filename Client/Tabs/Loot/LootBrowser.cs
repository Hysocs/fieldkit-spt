
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void DrawLootMenu()
        {
            _lootMenuSection = GUILayout.Toolbar(
                _lootMenuSection,
                new[] { "Visuals", "Item List" });
            GUILayout.Space(6f);

            if (_lootMenuSection == 0)
            {
                _lootSettingsScroll = BeginVerticalScrollView(
                    _lootSettingsScroll,
                    GUILayout.Height(
                        Mathf.Max(300f, MenuHeight - 150f)));
                DrawLootEspControls();
                GUILayout.Space(12f);
                GUILayout.EndScrollView();
                return;
            }

            DrawLootItemBrowser();
        }

        private void DrawLootItemBrowser()
        {
            RefreshLootSelectionCounts();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", GUILayout.Width(48f));
            _lootSearch = GUILayout.TextField(
                _lootSearch ?? "", GUILayout.ExpandWidth(true));
            _lootSelectedOnly = GUILayout.Toggle(
                _lootSelectedOnly, " Selected only", GUILayout.Width(105f));
            if (GUILayout.Button("Clear", GUILayout.Width(55f)))
                _lootSearch = "";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                string.Format(
                    "{0:N0} items | {1:N0} selected | {2:N0} prices",
                    _lootItemCount,
                    _lootSelectedItems.Count,
                    _lootPrices.Count),
                GUILayout.ExpandWidth(true));
            if (_lootPriceRefreshRequested && _lootPrices.Count == 0)
                GUILayout.Label("Loading...", GUILayout.Width(65f));
            if (GUILayout.Button("Refresh names", GUILayout.Width(105f)))
                RefreshLootCatalogNames();
            if (GUILayout.Button("Refresh prices", GUILayout.Width(105f)))
                RefreshLootPrices();
            if (GUILayout.Button("Clear all", GUILayout.Width(75f)))
            {
                _lootSelectedItems.Clear();
                _lootSelectionCountsDirty = true;
                SaveLootSelections();
            }
            GUILayout.EndHorizontal();

            if (_lootRoots.Count == 0)
            {
                GUILayout.Label("Waiting for the EFT item catalog...");
                return;
            }

            if (Event.current.type == EventType.Repaint)
            {
                Rect lastHeaderRect = GUILayoutUtility.GetLastRect();
                if (lastHeaderRect.yMax > 0f)
                {
                    _lootListViewportHeight = Mathf.Max(
                        120f,
                        _menuRect.height - lastHeaderRect.yMax - 28f);
                }
            }

            _selectedLootRoot = Mathf.Clamp(
                _selectedLootRoot, -1, _lootRoots.Count - 1);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.Width(
                    Mathf.Clamp(
                        MenuWidth * 0.27f,
                        185f,
                        255f)),
                GUILayout.ExpandHeight(true));
            GUILayout.Label("Categories", _sectionTitleStyle);
            _lootCategoryScroll = BeginVerticalScrollView(
                _lootCategoryScroll,
                GUILayout.Height(_lootListViewportHeight));
            DrawAllLootRootButton();
            for (int i = 0; i < _lootRoots.Count; i++)
                DrawLootRootButton(i);
            GUILayout.Space(12f);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.ExpandHeight(true));
            if (_selectedLootRoot < 0)
            {
                GUILayout.Label(
                    "ALL (" + _lootItemCount + ")",
                    _sectionTitleStyle);
            }
            else
            {
                LootCategory selected =
                    _lootRoots[_selectedLootRoot];
                GUILayout.Label(
                    selected.Name + " (" +
                    selected.TotalItems + ")",
                    _sectionTitleStyle);
            }
            _lootScroll = BeginVerticalScrollView(
                _lootScroll,
                GUILayout.Height(_lootListViewportHeight));
            if (_selectedLootRoot < 0)
            {
                for (int i = 0; i < _lootRoots.Count; i++)
                    DrawLootCategory(_lootRoots[i], 0);
            }
            else
            {
                DrawLootCategory(
                    _lootRoots[_selectedLootRoot], 0, true);
            }
            GUILayout.Space(18f);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawLootEspControls()
        {
            BeginCategoryColumns();
            DrawLootRenderInfoCard();
            GUILayout.Space(8f);
            DrawLootDisplayCard();
            GUILayout.Space(8f);
            DrawLootGroupingCard();
            GUILayout.Space(8f);
            DrawLootPriceCard();

            NextCategoryColumn();
            DrawLootRangeCard();
            GUILayout.Space(8f);
            DrawLootTypographyCard();
            GUILayout.Space(8f);
            DrawLootItemColorCard();
            GUILayout.Space(8f);
            DrawLootValuePaletteCard();
            EndCategoryColumns();
        }

        private void DrawLootDisplayCard()
        {
            BeginCategoryPanel("Display");
            DrawOptionToggle(_lootEspEnabled, " Enable loot ESP");
            DrawOptionToggle(_lootEspBoxes, " X marker");
            DrawOptionToggle(_lootEspNames, " Item name");
            DrawOptionToggle(_lootEspDistance, " Distance");
            DrawOptionToggle(_lootEspPrices, " Price");
            DrawLootToggleColor(
                _lootContainerEsp,
                "Items inside containers",
                _lootContainerEspColor,
                GetContainerEspColor(),
                new Color(0.98f, 0.57f, 0.24f, 1f),
                "Container loot ESP");
            if (_lootContainerEsp.Value)
            {
                GUILayout.Label(
                    _lootContainers.Count + " containers | " +
                    _containerEspEntries.Count + " matching");
            }
            if (DrawResetGroupButton())
            {
                _lootEspEnabled.Value = false;
                _lootEspBoxes.Value = false;
                _lootEspNames.Value = false;
                _lootEspDistance.Value = false;
                _lootEspPrices.Value = false;
                _lootContainerEsp.Value = false;
            }
            EndCategoryPanel();
        }

        private void DrawLootRangeCard()
        {
            BeginCategoryPanel("Render Range");
            DrawOptionSlider(
                "Loose loot", _lootEspCullDistance, 10f, 1000f, "0m");
            GUI.enabled = _lootContainerEsp.Value;
            DrawOptionSlider(
                "Containers", _lootContainerCullDistance,
                10f, 1000f, "0m");
            GUI.enabled = true;
            if (DrawResetGroupButton())
            {
                _lootEspCullDistance.Value = 50f;
                _lootContainerCullDistance.Value = 25f;
            }
            EndCategoryPanel();
        }

        private void DrawLootGroupingCard()
        {
            BeginCategoryPanel("World Grouping");
            DrawOptionToggle(
                _lootProximityGrouping, " Group nearby distant loot");
            GUI.enabled = _lootProximityGrouping.Value;
            DrawLootFloatSlider(
                "Group beyond", _lootGroupingDistance, 0f, 300f, "0m");
            DrawLootFloatSlider(
                "Group cull", _lootGroupCullDistance, 25f, 1000f, "0m");
            DrawLootFloatSlider(
                "Radius", _lootProximityRadius, 0.5f, 25f, "0.0m");
            DrawLootFloatSlider(
                "Vertical tolerance",
                _lootProximityHeight, 0.25f, 25f, "0.00m");
            GUI.enabled = true;
            if (DrawResetGroupButton())
            {
                _lootProximityGrouping.Value = false;
                _lootGroupingDistance.Value = 35f;
                _lootGroupCullDistance.Value = 75f;
                _lootProximityRadius.Value = 5f;
                _lootProximityHeight.Value = 2f;
            }
            EndCategoryPanel();
        }

        private void DrawLootTypographyCard()
        {
            BeginCategoryPanel("Text & Labels");
            DrawLootFontSlider("Item font size", _lootItemFontSize);
            DrawLootFontSlider(
                "Container font size", _lootContainerFontSize);
            DrawLootFontSlider("Group font size", _lootGroupFontSize);
            if (DrawResetGroupButton())
            {
                _lootItemFontSize.Value = 12;
                _lootContainerFontSize.Value = 12;
                _lootGroupFontSize.Value = 10;
            }
            EndCategoryPanel();
        }

        private void DrawLootPriceCard()
        {
            BeginCategoryPanel("Always Show by Price");
            DrawLootToggleColor(
                _lootPriceRangeEnabled,
                "Include items in range",
                _lootEspPriceColor,
                GetLootEspColor(true),
                new Color(0.98f, 0.80f, 0.08f, 1f),
                "Price-range loot ESP");
            GUI.enabled = _lootPriceRangeEnabled.Value;
            DrawOptionToggle(
                _lootPriceRangeSelectedOnly,
                " Selected Item List only");
            float minimum = Mathf.Clamp(
                _lootPriceMinimum.Value, 0f, LootPriceMaximumLimit);
            float maximum = Mathf.Clamp(
                _lootPriceMaximum.Value,
                minimum,
                LootPriceMaximumLimit);
            GUILayout.Label(
                FormatLootPrice(minimum) + " — " +
                FormatLootPrice(maximum));
            DrawDoubleEndedSlider(
                ref minimum, ref maximum,
                0f, LootPriceMaximumLimit);
            _lootPriceMinimum.Value = minimum;
            _lootPriceMaximum.Value = maximum;
            GUI.enabled = true;
            if (DrawResetGroupButton())
            {
                _lootPriceRangeEnabled.Value = false;
                _lootPriceRangeSelectedOnly.Value = true;
                _lootPriceMinimum.Value = 100000f;
                _lootPriceMaximum.Value =
                    LootPriceMaximumLimit;
            }
            EndCategoryPanel();
        }

        private void DrawLootItemColorCard()
        {
            BeginCategoryPanel("Item Colors");
            DrawLootColorRow(
                "Selected items", _lootEspColor,
                GetLootEspColor(false),
                new Color(0.13f, 0.83f, 0.93f, 1f),
                "Selected loot ESP");
            DrawLootColorRow(
                "Quest items", _lootQuestItemColor,
                GetQuestItemColor(),
                new Color(0.75f, 0.52f, 0.99f, 1f),
                "Quest-item loot ESP");
            DrawOptionToggle(
                _lootValueGradient, " Use value-tier colors");
            if (DrawResetGroupButton())
            {
                _lootEspColor.Value = "#22D3EEFF";
                _lootQuestItemColor.Value = "#C084FCFF";
                _lootValueGradient.Value = false;
            }
            EndCategoryPanel();
        }

        private void DrawLootRenderInfoCard()
        {
            BeginCategoryPanel("What Gets Rendered", false);
            GUILayout.Label(
                "Loose items render only after their item templates are selected in the Item List.");
            GUILayout.Label(
                "The optional price range can add matches according to its selected-list setting.");
            EndCategoryPanel();
        }

        private void DrawLootValuePaletteCard()
        {
            BeginCategoryPanel("Value Palette");
            GUI.enabled = _lootValueGradient.Value;
            DrawLootColorRow(
                "Low", _lootLowValueColor, GetLowValueColor(),
                new Color(0.58f, 0.64f, 0.72f, 1f),
                "Low-value loot ESP");
            DrawLootColorRow(
                "Low-mid", _lootLowMidValueColor, GetLowMidValueColor(),
                new Color(0.22f, 0.74f, 0.97f, 1f),
                "Low-mid-value loot ESP");
            DrawLootColorRow(
                "Mid", _lootMidValueColor, GetMidValueColor(),
                new Color(0.13f, 0.77f, 0.37f, 1f),
                "Mid-value loot ESP");
            DrawLootColorRow(
                "High-mid", _lootHighMidValueColor, GetHighMidValueColor(),
                new Color(0.98f, 0.80f, 0.08f, 1f),
                "High-mid-value loot ESP");
            DrawLootColorRow(
                "High", _lootHighValueColor, GetHighValueColor(),
                new Color(0.94f, 0.27f, 0.27f, 1f),
                "High-value loot ESP");
            GUI.enabled = true;
            if (DrawResetGroupButton())
            {
                _lootLowValueColor.Value = "#94A3B8FF";
                _lootLowMidValueColor.Value = "#38BDF8FF";
                _lootMidValueColor.Value = "#22C55EFF";
                _lootHighMidValueColor.Value = "#FACC15FF";
                _lootHighValueColor.Value = "#EF4444FF";
            }
            EndCategoryPanel();
        }

        private void DrawLootToggleColor(
            ConfigEntry<bool> toggle,
            string label,
            ConfigEntry<string> color,
            Color current,
            Color fallback,
            string pickerTitle)
        {
            GUILayout.BeginHorizontal();
            DrawOptionToggleLabel(
                toggle, " " + label, GUILayout.ExpandWidth(true));
            DrawColorSquare(color, current, fallback, pickerTitle);
            DrawOptionHotkey(toggle);
            GUILayout.EndHorizontal();
        }

        private void DrawLootColorRow(
            string label,
            ConfigEntry<string> color,
            Color current,
            Color fallback,
            string pickerTitle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.ExpandWidth(true));
            DrawColorSquare(color, current, fallback, pickerTitle);
            DrawHotkeyColumnSpacer();
            GUILayout.EndHorizontal();
        }

        private void DrawLootFloatSlider(
            string label,
            ConfigEntry<float> setting,
            float minimum,
            float maximum,
            string format)
        {
            DrawOptionSlider(
                label, setting, minimum, maximum, format);
        }

        private void DrawLootFontSlider(
            string label,
            ConfigEntry<int> setting)
        {
            string description = OptionDescription(setting);
            GUILayout.Label(
                new GUIContent(
                    label + ": " + setting.Value,
                    description));
            Rect labelRect = GUILayoutUtility.GetLastRect();
            setting.Value = Mathf.RoundToInt(
                GUILayout.HorizontalSlider(
                    setting.Value, 8f, 24f));
            Rect sliderRect = GUILayoutUtility.GetLastRect();
            GUI.Label(
                Rect.MinMaxRect(
                    Mathf.Min(labelRect.xMin, sliderRect.xMin),
                    labelRect.yMin,
                    Mathf.Max(labelRect.xMax, sliderRect.xMax),
                    sliderRect.yMax),
                new GUIContent("", description));
        }

        private void RefreshLootPrices()
        {
            try
            {
                if (_lootRagfair != null)
                {
                    _lootRagfair.RefreshItemPrices();
                    _lootPriceRefreshRequested = true;
                }
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Flea price refresh failed: " + exception.Message);
            }
        }

        private void RefreshLootCatalogNames()
        {
            try
            {
                if (_lootHandbook != null &&
                    _lootItemFactory != null)
                {
                    _lootNameRefreshAttempts = 0;
                    BuildLootCatalog(
                        _lootHandbook,
                        _lootItemFactory);
                    _selectedLootRoot = Mathf.Clamp(
                        _selectedLootRoot,
                        -1,
                        Mathf.Max(0, _lootRoots.Count - 1));
                    LogSource.LogInfo(
                        "Loot catalog names refreshed.");
                    return;
                }

                _lootHandbook = null;
                _lootItemFactory = null;
                EnsureLootCatalog();
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Loot catalog name refresh failed: " +
                    exception.Message);
            }
        }

        private void DrawLootRootButton(int index)
        {
            LootCategory category = _lootRoots[index];
            bool selected = index == _selectedLootRoot;
            if (_lootCategoryButtonStyle == null)
            {
                _lootCategoryButtonStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                        wordWrap = false
                    };
            }

            if (GUILayout.Button(
                (selected ? "> " : "") + category.Name +
                " (" + SelectedLootCount(category) + "/" +
                category.TotalItems + ")",
                _lootCategoryButtonStyle))
            {
                _selectedLootRoot = index;
                _lootScroll = Vector2.zero;
            }
        }

        private void DrawAllLootRootButton()
        {
            if (_lootCategoryButtonStyle == null)
            {
                _lootCategoryButtonStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                        wordWrap = false
                    };
            }

            bool selected = _selectedLootRoot < 0;
            if (GUILayout.Button(
                (selected ? "> " : "") + "ALL (" +
                _lootSelectedItems.Count + "/" +
                _lootItemCount + ")",
                _lootCategoryButtonStyle))
            {
                _selectedLootRoot = -1;
                _lootScroll = Vector2.zero;
            }
        }

        private bool DrawLootCategory(
            LootCategory category,
            int depth,
            bool forceExpanded = false)
        {
            bool filterActive = !string.IsNullOrWhiteSpace(_lootSearch) ||
                                _lootSelectedOnly;
            if (filterActive && !LootCategoryHasVisibleItems(category))
                return false;

            int selected = SelectedLootCount(category);
            bool all = category.TotalItems > 0 &&
                       selected == category.TotalItems;
            bool any = selected > 0;
            bool expanded = forceExpanded ||
                            category.Expanded ||
                            filterActive;

            if (_lootFoldoutButtonStyle == null)
            {
                _lootFoldoutButtonStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fixedWidth = 32f,
                        fixedHeight = 28f,
                        fontSize = 18,
                        fontStyle = FontStyle.Bold,
                        padding = new RectOffset(0, 0, 0, 1),
                        margin = new RectOffset(2, 4, 1, 1)
                    };
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(depth * 13f);
            if (GUILayout.Button(
                expanded ? "\u25BC" : "\u25B6",
                _lootFoldoutButtonStyle))
                category.Expanded = !category.Expanded;
            bool toggled = GUILayout.Toggle(
                all,
                any && !all ? " Some" : " All",
                GUILayout.Width(62f));
            if (toggled != all)
            {
                SetLootCategorySelected(category, toggled);
                _lootSelectionCountsDirty = true;
                SaveLootSelections();
                selected = toggled ? category.TotalItems : 0;
            }
            GUILayout.Label(
                category.Name + " (" + selected + "/" +
                category.TotalItems + ")");
            GUILayout.EndHorizontal();

            if (!expanded)
                return true;

            for (int i = 0; i < category.Children.Count; i++)
                DrawLootCategory(category.Children[i], depth + 1);
            for (int i = 0; i < category.Items.Count; i++)
            {
                LootCatalogItem item = category.Items[i];
                if (ShouldListLootItem(item))
                    DrawLootItem(item, depth + 1);
            }
            return true;
        }

        private void DrawLootItem(LootCatalogItem item, int depth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(depth * 13f + 25f);
            bool oldValue = _lootSelectedItems.Contains(item.Id);
            bool value = GUILayout.Toggle(
                oldValue,
                (item.IsQuestItem ? "[Quest] " : "") + item.Name,
                GUILayout.ExpandWidth(true));
            if (value != oldValue)
            {
                if (value)
                    _lootSelectedItems.Add(item.Id);
                else
                    _lootSelectedItems.Remove(item.Id);
                _lootSelectionCountsDirty = true;
                SaveLootSelections();
            }
            float price = GetLootPrice(item.Id, item.BasePrice);
            GUILayout.Label(
                price > 0f ? FormatLootPrice(price) : "—",
                GUILayout.Width(92f));
            bool actionInProgress =
                _lootItemActionsInProgress.Contains(item.Id);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !actionInProgress;
            if (GUILayout.Button(
                actionInProgress ? "Working..." : "Spawn / Get",
                GUILayout.Width(100f)))
            {
                OpenLootQuantityPopup(item);
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
        }

        private void OpenLootQuantityPopup(LootCatalogItem item)
        {
            _lootQuantityItem = item;
            _lootQuantityText = "1";
            _lootQuantityPopupRect.x = Mathf.Clamp(
                _menuRect.center.x -
                _lootQuantityPopupRect.width * 0.5f,
                0f,
                Mathf.Max(
                    0f,
                    Screen.width - _lootQuantityPopupRect.width));
            _lootQuantityPopupRect.y = Mathf.Clamp(
                _menuRect.center.y -
                _lootQuantityPopupRect.height * 0.5f,
                0f,
                Mathf.Max(
                    0f,
                    Screen.height - _lootQuantityPopupRect.height));
        }

        private void DrawLootQuantityPopup()
        {
            if (_lootQuantityItem == null)
                return;

            _lootQuantityPopupRect = GUI.ModalWindow(
                731910,
                _lootQuantityPopupRect,
                DrawLootQuantityPopupContents,
                "Spawn / Get item");
        }

        private void DrawLootQuantityPopupContents(int windowId)
        {
            GUILayout.Label(_lootQuantityItem.Name);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Amount", GUILayout.Width(58f));
            _lootQuantityText = GUILayout.TextField(
                _lootQuantityText ?? "1",
                4,
                GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            int amount;
            bool valid = int.TryParse(_lootQuantityText, out amount) &&
                         amount >= 1 &&
                         amount <= 100;
            if (!valid)
                GUILayout.Label("Enter a number from 1 to 100.");

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && valid;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn at feet"))
            {
                LootCatalogItem item = _lootQuantityItem;
                _lootQuantityItem = null;
                SpawnLootItemAtFeet(item, amount);
            }
            if (GUILayout.Button("Add to stash"))
                ConfirmDirectLootDelivery(amount, false);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add to inventory"))
                ConfirmDirectLootDelivery(amount, true);
            if (GUILayout.Button("Send in mail"))
            {
                LootCatalogItem item = _lootQuantityItem;
                _lootQuantityItem = null;
                AddLootItemToInventory(item, amount);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = previousEnabled;

            if (GUILayout.Button("Cancel"))
                _lootQuantityItem = null;
        }

        private void ConfirmDirectLootDelivery(
            int amount,
            bool carriedInventory)
        {
            LootCatalogItem item = _lootQuantityItem;
            _lootQuantityItem = null;
            AddLootItemDirect(item, amount, carriedInventory);
        }

    }
}
