
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void DrawOtherMenu()
        {
            _otherMenuScroll = BeginVerticalScrollView(
                _otherMenuScroll);

            BeginCategoryColumns();
            BeginCategoryPanel("Menu Options");

            int menuFontIndex = FindEspFontIndex(
                _menuFontName.Value);
            int nextMenuFontIndex = DrawDropdown(
                "menu-font",
                menuFontIndex,
                EspFontNames,
                "Font used by the FieldKit menu.");
            if (nextMenuFontIndex != menuFontIndex)
                _menuFontName.Value =
                    EspFontNames[nextMenuFontIndex];

            float maximumUiScale = MaximumMenuScale;
            _pendingMenuUiScale = Mathf.Clamp(
                _pendingMenuUiScale,
                0.5f,
                maximumUiScale);
            GUILayout.Label(
                "UI scale: " +
                _pendingMenuUiScale.ToString("0.0") +
                "x (screen max " +
                maximumUiScale.ToString("0.00") + "x)");
            _pendingMenuUiScale = GUILayout.HorizontalSlider(
                _pendingMenuUiScale,
                0.5f,
                maximumUiScale);
            GUILayout.BeginHorizontal();
            GUI.enabled = !Mathf.Approximately(
                _menuUiScale.Value,
                _pendingMenuUiScale);
            if (GUILayout.Button("Apply UI scale"))
                _menuUiScale.Value = _pendingMenuUiScale;
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                "Primary color",
                GUILayout.ExpandWidth(true));
            DrawColorSquare(
                _guiPrimaryColor,
                ParseVisualColor(
                    _guiPrimaryColor.Value,
                    new Color32(120, 207, 245, 255)),
                new Color32(120, 207, 245, 255),
                "Menu primary color");
            DrawHotkeyColumnSpacer();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                "Open / close menu",
                GUILayout.ExpandWidth(true));
            DrawStandaloneHotkey(_menuKey);
            GUILayout.EndHorizontal();

            if (DrawResetGroupButton())
            {
                _guiPrimaryColor.Value = "#78CFF5FF";
                _menuFontName.Value = "Segoe UI";
                _menuUiScale.Value = 1f;
                _pendingMenuUiScale = 1f;
                _menuKey.Value =
                    new KeyboardShortcut(KeyCode.Insert);
            }

            EndCategoryPanel();
            GUILayout.Space(8f);
            BeginCategoryPanel("Raid & World", false);

            if (GUILayout.Button("Unlock all doors"))
                UnlockAllDoors();

            GUILayout.Label(_doorToolStatus);
            GUILayout.Label(
                "Keyed and keycard doors are included. Extraction doors are not changed.");

            EndCategoryPanel();
            GUILayout.Space(8f);
            BeginCategoryPanel("Living AI Loot");

            DrawOptionToggle(_lootLivingAi, " Loot Living AI");

            GUI.enabled = _lootLivingAi.Value;
            DrawOptionToggle(
                _holdLivingAiStill, " Hold AI still while looting");
            GUI.enabled = true;

            if (DrawResetGroupButton())
            {
                _lootLivingAi.Value = false;
                _holdLivingAiStill.Value = true;
            }

            EndCategoryPanel();
            NextCategoryColumn();
            BeginCategoryPanel("Vision Modes");

            bool previousThermalVision = _forceThermalVision.Value;
            bool thermalVision = DrawOptionToggle(
                _forceThermalVision, " Force Thermal Vision");
            if (thermalVision != previousThermalVision)
            {
                if (thermalVision)
                    _forceNightVision.Value = false;
            }
            if (_forceThermalVision.Value)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(18f);
                DrawOptionToggle(_cleanThermalVision, " Clean Image");
                GUILayout.EndHorizontal();
            }

            bool previousNightVision = _forceNightVision.Value;
            bool nightVision = DrawOptionToggle(
                _forceNightVision, " Force Night Vision");
            if (nightVision != previousNightVision)
            {
                if (nightVision)
                    _forceThermalVision.Value = false;
            }
            if (_forceNightVision.Value)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(18f);
                DrawOptionToggle(_cleanNightVision, " Clean Image");
                GUILayout.EndHorizontal();
                DrawOptionSlider(
                    "Bloom intensity", _nightVisionBloomAmount,
                    0f, 2f, "0.00x");
            }

            if (DrawResetGroupButton())
            {
                _forceThermalVision.Value = false;
                _forceNightVision.Value = false;
                _cleanThermalVision.Value = false;
                _cleanNightVision.Value = false;
                _nightVisionBloomAmount.Value = 1f;
            }

            EndCategoryPanel();
            GUILayout.Space(8f);
            BeginCategoryPanel("Face Shield Overlay");

            GUILayout.Label("Overlay texture");
            _forcedVisorMode.Value = (ForcedVisorMode)
                DrawDropdown(
                    "face-shield-overlay",
                    (int)_forcedVisorMode.Value,
                    VisorModeLabels,
                    OptionDescription(_forcedVisorMode));

            if (DrawResetGroupButton())
            {
                _forcedVisorMode.Value =
                    ForcedVisorMode.FollowEquipment;
                CloseDropdown();
            }

            EndCategoryPanel();
            GUILayout.Space(8f);
            BeginCategoryPanel("Performance", false);

            DrawOptionToggle(
                _showEntityInspector,
                " Show entity inspector");
            DrawOptionToggle(
                _showPerformanceTelemetry,
                " Show FieldKit metrics");
            if (_showPerformanceTelemetry.Value)
            {
                GUILayout.Label(
                    "FieldKit CPU: " +
                    _perfCorePercent.ToString("0.00") +
                    "% of one core");
                GUILayout.Label(
                    "Runtime update: " +
                    _perfUpdateMs.ToString("0.000") +
                    " ms (max " +
                    _perfUpdateMaxMs.ToString("0.00") +
                    ") @ " +
                    _perfUpdateRate.ToString("0") +
                    " Hz");
                GUILayout.Label(
                    "ESP render: " +
                    _perfEspMs.ToString("0.000") +
                    " ms (max " +
                    _perfEspMaxMs.ToString("0.00") +
                    ") @ " +
                    _perfEspRate.ToString("0") +
                    " Hz");
                GUILayout.Label(
                    "  Loot portion: " +
                    _perfLootMs.ToString("0.000") +
                    " ms (max " +
                    _perfLootMaxMs.ToString("0.00") +
                    ") @ " +
                    _perfLootRate.ToString("0") +
                    " Hz");
                GUILayout.Label(
                    "GUI pass: " +
                    _perfGuiMs.ToString("0.000") +
                    " ms (max " +
                    _perfGuiMaxMs.ToString("0.00") +
                    ") @ " +
                    _perfGuiRate.ToString("0") +
                    " calls/s");
                GUILayout.Label(
                    "World/visibility: " +
                    _perfWorldMs.ToString("0.000") +
                    " ms (max " +
                    _perfWorldMaxMs.ToString("0.00") + ")");
                GUILayout.Label(
                    "Cache/cluster build: " +
                    _perfCacheBuildMs.ToString("0.000") +
                    " ms (max " +
                    _perfCacheBuildMaxMs.ToString("0.00") + ")");
                GUILayout.Label(
                    "Update max: world " +
                    _perfWorldRefreshMaxMs.ToString("0.00") +
                    " | chams " +
                    _perfChamsMaxMs.ToString("0.00") +
                    " | catalog " +
                    _perfCatalogMaxMs.ToString("0.00") +
                    " ms");
                GUILayout.Label(
                    "Visibility response: up to 15 Hz/target");
                GUILayout.Label(
                    "World fallback: 2 Hz | Scope: event-driven");
                GUILayout.Label(
                    "Targets: " + _targets.Count +
                    " | Loot cached: " +
                    _lootEspEntries.Count +
                    " | Containers: " +
                    _containerEspEntries.Count);
                GUILayout.Label(
                    "Events: loot " +
                    _perfLootInvalidations +
                    " | container " +
                    _perfContainerInvalidations);
                GUILayout.Label(
                    "Event passes: AI " +
                    _perfFriendlyAiRefreshes +
                    " | chams " +
                    _perfChamDiscoveryPasses);
                if (_lootEntryBuildActive)
                {
                    GUILayout.Label(
                        "Loot cache build: " +
                        _lootEntryBuildCursor + "/" +
                        _looseWorldLootItems.Count +
                        " (10/frame)");
                }
                else
                {
                    GUILayout.Label("Loot cache: event-idle");
                }
                if (_containerCacheBuildActive)
                {
                    GUILayout.Label(
                        "Container cache build: " +
                        _containerCacheBuildCursor + "/" +
                        _lootContainers.Count +
                        " (6/frame)");
                }
                GUILayout.Label(
                    "Measures FieldKit main paths only.");
            }

            EndCategoryPanel();
            EndCategoryColumns();

            EndVerticalScrollView();
        }

    }
}
