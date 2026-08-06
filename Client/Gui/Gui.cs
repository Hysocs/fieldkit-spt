
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private static readonly string[] MenuTabs =
        {
            "Character",
            "Entities",
            "Loot",
            "Time/Weather",
            "Other"
        };

        private const float PreferredMenuWidth = 1000f;
        private const float PreferredMenuHeight = 820f;
        private const float MinimumMenuWidth = 680f;
        private const float MinimumMenuHeight = 520f;
        private float MaximumMenuScale =>
            Mathf.Max(
                0.5f,
                Mathf.Min(
                    Screen.width / (PreferredMenuWidth + 32f),
                    Screen.height / (PreferredMenuHeight + 32f)));
        private float MenuScale =>
            _menuUiScale == null
                ? 1f
                : Mathf.Clamp(
                    _menuUiScale.Value,
                    0.5f,
                    MaximumMenuScale);
        private float VirtualScreenWidth =>
            Screen.width / MenuScale;
        private float VirtualScreenHeight =>
            Screen.height / MenuScale;
        private float MenuWidth =>
            Mathf.Clamp(
                VirtualScreenWidth - 32f,
                Mathf.Min(MinimumMenuWidth, VirtualScreenWidth),
                PreferredMenuWidth);
        private float CurrentMenuWidth =>
            Mathf.Clamp(
                VirtualScreenWidth - 32f,
                Mathf.Min(MinimumMenuWidth, VirtualScreenWidth),
                PreferredMenuWidth);
        private float MenuHeight =>
            Mathf.Clamp(
                VirtualScreenHeight - 32f,
                Mathf.Min(MinimumMenuHeight, VirtualScreenHeight),
                PreferredMenuHeight);
        private float MenuColumnWidth =>
            Mathf.Max(300f, (MenuWidth - 48f) * 0.5f);
        private const float AttachedInfoWidth = 285f;
        private ConfigEntry<float> _menuWindowX;
        private ConfigEntry<float> _menuWindowY;
        private ConfigEntry<float> _diagnosticsWindowX;
        private ConfigEntry<float> _diagnosticsWindowY;
        private ConfigEntry<float> _colorPickerWindowX;
        private ConfigEntry<float> _colorPickerWindowY;
        private ConfigEntry<float> _entityInspectorWindowX;
        private ConfigEntry<float> _entityInspectorWindowY;
        private ConfigEntry<string> _guiPrimaryColor;
        private ConfigEntry<int> _savedMenuTab;
        private int _menuTab;
        private int _characterSection;
        private bool _menuOpen;
        private Rect _menuRect =
            new Rect(
                30f,
                30f,
                PreferredMenuWidth,
                PreferredMenuHeight);
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;
        private Texture2D _menuCursorTexture;
        private bool _menuCursorApplied;
        private UnityEngine.EventSystems.EventSystem
            _blockedEventSystem;
        private bool _blockedEventSystemWasEnabled;
        private GUISkin _adminSkin;
        private GUIStyle _tabStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _resetButtonStyle;
        private GUIStyle _dropdownButtonStyle;
        private GUIStyle _dropdownArrowStyle;
        private GUIStyle _dropdownMenuStyle;
        private GUIStyle _dropdownItemStyle;
        private GUIStyle _optionTooltipStyle;
        private string _pendingOptionTooltip;
        private float _optionTooltipHoverStarted;
        private bool _categoryResetRequested;
        private string _openDropdownId;
        private Rect _attachedInfoRect;
        private Rect _colorPickerRect =
            new Rect(805f, 270f, 390f, 250f);
        private readonly List<Texture2D> _themeTextures =
            new List<Texture2D>(16);

        private void EnsureMenuCursorTexture()
        {
            if (_menuCursorTexture != null)
                return;

            const int width = 14;
            const int height = 21;
            bool[,] fill = new bool[width, height];
            Vector2[] shape =
            {
                new Vector2(1f, 1f),
                new Vector2(1f, 17f),
                new Vector2(5f, 13f),
                new Vector2(8f, 20f),
                new Vector2(11f, 19f),
                new Vector2(7f, 12f),
                new Vector2(13f, 12f)
            };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = false;
                    for (int current = 0, previous = shape.Length - 1;
                         current < shape.Length;
                         previous = current++)
                    {
                        Vector2 a = shape[current];
                        Vector2 b = shape[previous];
                        if ((a.y > y) != (b.y > y) &&
                            x < (b.x - a.x) * (y - a.y) /
                                (b.y - a.y) + a.x)
                            inside = !inside;
                    }
                    fill[x, y] = inside;
                }
            }

            Color32[] pixels = new Color32[width * height];
            Color32 outline = new Color32(8, 10, 14, 255);
            Color32 fillColor =
                new Color32(242, 246, 252, 255);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool filled = fill[x, y];
                    bool bordered = false;
                    if (!filled)
                    {
                        for (int offsetY = -1;
                             offsetY <= 1 && !bordered;
                             offsetY++)
                        {
                            for (int offsetX = -1;
                                 offsetX <= 1;
                                 offsetX++)
                            {
                                int sampleX = x + offsetX;
                                int sampleY = y + offsetY;
                                if (sampleX >= 0 &&
                                    sampleX < width &&
                                    sampleY >= 0 &&
                                    sampleY < height &&
                                    fill[sampleX, sampleY])
                                {
                                    bordered = true;
                                    break;
                                }
                            }
                        }
                    }

                    pixels[
                        (height - 1 - y) * width + x] =
                        filled
                            ? fillColor
                            : bordered
                                ? outline
                                : new Color32(0, 0, 0, 0);
                }
            }

            _menuCursorTexture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false)
            {
                name = "FieldKit Menu Cursor",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            _menuCursorTexture.SetPixels32(pixels);
            _menuCursorTexture.Apply(false, false);
            _themeTextures.Add(_menuCursorTexture);
        }

        private void ConfigureGuiSettings()
        {
            _menuWindowX = Config.Bind(
                "GUI Layout", "Main Window X", 30f,
                "Saved horizontal position of the main admin window.");
            _menuWindowY = Config.Bind(
                "GUI Layout", "Main Window Y", 30f,
                "Saved vertical position of the main admin window.");
            _diagnosticsWindowX = Config.Bind(
                "GUI Layout", "Weapon Diagnostics X", 670f,
                "Saved horizontal position of weapon diagnostics.");
            _diagnosticsWindowY = Config.Bind(
                "GUI Layout", "Weapon Diagnostics Y", 30f,
                "Saved vertical position of weapon diagnostics.");
            _colorPickerWindowX = Config.Bind(
                "GUI Layout", "Color Picker X", 805f,
                "Saved horizontal position of the color picker.");
            _colorPickerWindowY = Config.Bind(
                "GUI Layout", "Color Picker Y", 270f,
                "Saved vertical position of the color picker.");
            _entityInspectorWindowX = Config.Bind(
                "GUI Layout", "Entity Inspector X", 30f,
                "Saved horizontal position of the entity inspector.");
            _entityInspectorWindowY = Config.Bind(
                "GUI Layout", "Entity Inspector Y", 30f,
                "Saved vertical position of the entity inspector.");
            _guiPrimaryColor = Config.Bind(
                "GUI Appearance", "Primary Color", "#78CFF5FF",
                "Primary RGBA accent color used by the FieldKit menu.");
            _guiPrimaryColor.SettingChanged +=
                OnGuiPrimaryColorChanged;
            _savedMenuTab = Config.Bind(
                "GUI Layout", "Selected Tab", 0,
                new ConfigDescription(
                    "Last selected admin-tools tab.",
                    new AcceptableValueRange<int>(
                        0,
                        MenuTabs.Length - 1)));

            _menuRect.x = _menuWindowX.Value;
            _menuRect.y = _menuWindowY.Value;
            _weaponDiagnosticsRect.x =
                _diagnosticsWindowX.Value;
            _weaponDiagnosticsRect.y =
                _diagnosticsWindowY.Value;
            _colorPickerRect.x = _colorPickerWindowX.Value;
            _colorPickerRect.y = _colorPickerWindowY.Value;
            _entityInspectorRect.x = _entityInspectorWindowX.Value;
            _entityInspectorRect.y = _entityInspectorWindowY.Value;
            _menuTab = Mathf.Clamp(
                _savedMenuTab.Value,
                0,
                MenuTabs.Length - 1);
        }

        private void DrawMenu(int windowId)
        {
            int selectedTab = GUILayout.Toolbar(
                _menuTab,
                MenuTabs,
                _tabStyle);
            if (selectedTab != _menuTab)
            {
                _menuTab = selectedTab;
                _savedMenuTab.Value = _menuTab;
                CloseDropdown();
            }
            GUILayout.Space(8f);

            switch (_menuTab)
            {
                case 1:
                    DrawEntityMenu();
                    break;
                case 2:
                    DrawLootMenu();
                    break;
                case 3:
                    DrawWeatherMenu();
                    break;
                case 4:
                    DrawOtherMenu();
                    break;
                default:
                    DrawCharacterHub();
                    break;
            }

            DrawOptionTooltip();
            GUI.DragWindow(new Rect(0f, 0f, _menuRect.width, 24f));
        }

        private void UpdateMenuGeometry()
        {
            _menuRect.width = CurrentMenuWidth;
            _menuRect.height = MenuHeight;
            _menuRect.x = Mathf.Clamp(
                _menuRect.x,
                0f,
                Mathf.Max(
                    0f,
                    VirtualScreenWidth - _menuRect.width));
            _menuRect.y = Mathf.Clamp(
                _menuRect.y,
                0f,
                Mathf.Max(
                    0f,
                    VirtualScreenHeight - _menuRect.height));
        }

        private void BeginCategoryColumns()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(
                GUILayout.Width(MenuColumnWidth));
        }

        private void NextCategoryColumn()
        {
            GUILayout.EndVertical();
            GUILayout.BeginVertical(
                GUILayout.Width(MenuColumnWidth));
        }

        private static void EndCategoryColumns()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void BeginCategoryPanel(
            string title,
            bool showResetButton = true)
        {
            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.Width(MenuColumnWidth));
            _categoryResetRequested = false;

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                title,
                _sectionTitleStyle,
                GUILayout.ExpandWidth(true));
            if (showResetButton)
            {
                _categoryResetRequested = GUILayout.Button(
                    new GUIContent("\u21BB", "Reset group"),
                    _resetButtonStyle,
                    GUILayout.Width(28f),
                    GUILayout.Height(28f));
            }
            GUILayout.EndHorizontal();
        }

        private static void EndCategoryPanel()
        {
            GUILayout.EndVertical();
        }

        private bool DrawResetGroupButton()
        {
            bool resetRequested = _categoryResetRequested;
            _categoryResetRequested = false;
            return resetRequested;
        }

        private void DrawCharacterHub()
        {
            _characterSection = GUILayout.Toolbar(
                _characterSection,
                new[] { "Character", "Weapons" });
            GUILayout.Space(6f);

            if (_characterSection == 0)
                DrawCharacterMenu();
            else
                DrawWeaponMenu();
        }

        private void DrawAttachedTabInfoPanel()
        {
            if (!_menuOpen ||
                (_menuTab != 0 &&
                 !(_menuTab == 1 &&
                   _entityListSection == 0)))
                return;

            float height = _menuTab == 0
                ? _characterSection == 0 ? 205f : 150f
                : 175f;
            _attachedInfoRect = new Rect(
                _menuRect.xMax + AttachedInfoWidth + 8f <=
                    VirtualScreenWidth
                    ? _menuRect.xMax + 8f
                    : Mathf.Max(0f, _menuRect.x - AttachedInfoWidth - 8f),
                _menuRect.y + 34f,
                AttachedInfoWidth,
                height);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.82f);
            GUI.Window(
                731906 + _menuTab + _entityListSection,
                _attachedInfoRect,
                DrawAttachedTabInfoWindow,
                _menuTab == 0
                    ? _characterSection == 0
                        ? "Character Status"
                        : "Current Weapon"
                    : "Visuals Quick Info");
            GUI.color = previousColor;
        }

        private void DrawAttachedTabInfoWindow(int windowId)
        {
            if (_menuTab == 0)
            {
                if (_characterSection == 0)
                    DrawCharacterStatus();
                else
                    DrawCurrentWeaponStatus();
            }
            else if (_menuTab == 1 &&
                     _entityListSection == 0)
                DrawEspQuickInfo();
        }

        private void PersistGuiLayout()
        {
            if (_menuWindowX == null)
                return;

            if (!Mathf.Approximately(
                _menuWindowX.Value,
                _menuRect.x))
                _menuWindowX.Value = _menuRect.x;
            if (!Mathf.Approximately(
                _menuWindowY.Value,
                _menuRect.y))
                _menuWindowY.Value = _menuRect.y;
            if (!Mathf.Approximately(
                _diagnosticsWindowX.Value,
                _weaponDiagnosticsRect.x))
                _diagnosticsWindowX.Value =
                    _weaponDiagnosticsRect.x;
            if (!Mathf.Approximately(
                _diagnosticsWindowY.Value,
                _weaponDiagnosticsRect.y))
                _diagnosticsWindowY.Value =
                    _weaponDiagnosticsRect.y;
            if (!Mathf.Approximately(
                _colorPickerWindowX.Value,
                _colorPickerRect.x))
                _colorPickerWindowX.Value = _colorPickerRect.x;
            if (!Mathf.Approximately(
                _colorPickerWindowY.Value,
                _colorPickerRect.y))
                _colorPickerWindowY.Value = _colorPickerRect.y;
            if (!Mathf.Approximately(
                _entityInspectorWindowX.Value,
                _entityInspectorRect.x))
                _entityInspectorWindowX.Value =
                    _entityInspectorRect.x;
            if (!Mathf.Approximately(
                _entityInspectorWindowY.Value,
                _entityInspectorRect.y))
                _entityInspectorWindowY.Value =
                    _entityInspectorRect.y;
        }

    }
}
