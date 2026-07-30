
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private Vector2 _espMenuScroll;
        private ConfigEntry<string> _openColorSetting;
        private string _openColorLabel;
        private Color _openColorFallback;
        private GUIStyle _espRoleFoldoutStyle;
        private Vector2 _espRoleListScroll;
        private Vector2 _chamRoleListScroll;

        private void DrawEspMenu()
        {
            _espMenuScroll = BeginVerticalScrollView(
                _espMenuScroll,
                GUILayout.Height(
                    Mathf.Max(300f, MenuHeight - 105f)));

            BeginCategoryColumns();
            BeginCategoryPanel("ESP Targets");
            DrawOptionToggle(_enabled, " Enable ESP");
            DrawColorColumnHeaders();
            _espRoleListScroll = BeginVerticalScrollView(
                _espRoleListScroll,
                GUILayout.Height(260f));
            DrawAllRoleRow();
            for (int i = 0; i < _espRoleGroups.Count; i++)
                DrawRoleGroup(_espRoleGroups[i]);
            GUILayout.EndScrollView();
            DrawOptionToggle(_showBoxes, " Show Boxes");
            DrawOptionToggle(_showBones, " Show Bone ESP");
            DrawOptionToggle(_showAimLines, " Show Aim Lines");
            DrawOptionToggle(
                _visibilityCheck, " Visibility Check");
            DrawExtractionEspRow();
            if (DrawResetGroupButton())
            {
                _enabled.Value = true;
                for (int i = 0; i < _espRoles.Count; i++)
                {
                    EspRoleSettings role = _espRoles[i];
                    role.Enabled.Value = true;
                    role.VisibleColor.Value =
                        "#" + ColorUtility.ToHtmlStringRGBA(
                            role.DefaultVisible);
                    role.HiddenColor.Value =
                        "#" + ColorUtility.ToHtmlStringRGBA(
                            role.DefaultHidden);
                }
                _showBoxes.Value = true;
                _showBones.Value = true;
                _showAimLines.Value = true;
                _visibilityCheck.Value = true;
                _showExtractions.Value = true;
                _extractionColor.Value = "#F59E0BFF";
                _usableExtractionColor.Value = "#22C55EFF";
                ResetVisualColors();
            }
            EndCategoryPanel();

            BeginCategoryPanel("ESP Geometry & Text");
            DrawOptionSlider(
                "Distance", _maxDistance, 25f, 1500f, "0m");
            DrawOptionSlider(
                "Box thickness", _lineThickness, 1f, 8f, "0.0");
            DrawOptionSlider(
                "Skeleton thickness", _boneThickness, 0.5f, 8f, "0.0");
            DrawOptionSlider(
                "Aim-line thickness",
                _aimLineThickness, 0.5f, 8f, "0.0");
            DrawOptionSlider(
                "Scope color", _scopeColorBrightness,
                0.5f, 2f, "0.00");

            int fontIndex = FindEspFontIndex(_espFontName.Value);
            int nextFontIndex = DrawDropdown(
                "esp-font",
                fontIndex,
                EspFontNames,
                "Font used for character ESP labels.");
            if (nextFontIndex != fontIndex)
                _espFontName.Value = EspFontNames[nextFontIndex];
            DrawOptionSlider(
                "Text size", _fontSize, 9, 32, "0");
            DrawOptionSlider(
                "Text outline",
                _textOutlineThickness, 0f, 3f, "0.0");
            if (DrawResetGroupButton())
            {
                _maxDistance.Value = 500f;
                _lineThickness.Value = 2f;
                _boneThickness.Value = 2f;
                _aimLineThickness.Value = 2f;
                _scopeColorBrightness.Value = 0.5f;
                _fontSize.Value = 13;
                _espFontName.Value = "Segoe UI";
                _textOutlineThickness.Value = 1f;
            }
            EndCategoryPanel();

            NextCategoryColumn();
            BeginCategoryPanel("Chams");
            DrawOptionToggle(_chamsEnabled, " GPU Chams");
            DrawOptionToggle(_chamsCharacters, " Characters");
            DrawOptionToggle(
                _chamsPerLimbVisibility,
                " Per-limb visibility");
            DrawColorColumnHeaders();
            _chamRoleListScroll = BeginVerticalScrollView(
                _chamRoleListScroll,
                GUILayout.Height(260f));
            DrawAllChamRoleRow();
            for (int i = 0; i < _espRoleGroups.Count; i++)
                DrawChamRoleGroup(_espRoleGroups[i]);
            GUILayout.EndScrollView();
            GUILayout.Space(5f);
            GUILayout.Label("World (RGBA)", _sectionTitleStyle);
            DrawWorldChamRow(
                _chamsCorpses, " Corpses", WorldChamKind.Corpse);
            DrawWorldChamRow(
                _chamsLoot, " Loot", WorldChamKind.Loot);
            DrawOptionToggle(
                _cullGrass,
                " Disable GPU grass (performance)");
            GUILayout.Label(
                "Loot render distance: " +
                Mathf.RoundToInt(_lootRenderDistance.Value) + "m");
            _lootRenderDistance.Value = GUILayout.HorizontalSlider(
                _lootRenderDistance.Value, 10f, 1000f);
            GUILayout.Label(
                "Character distance: " +
                Mathf.RoundToInt(_chamsMaxDistance.Value) + "m");
            _chamsMaxDistance.Value = GUILayout.HorizontalSlider(
                _chamsMaxDistance.Value, 25f, 500f);

            GUILayout.Label(
                "Character opacity: " +
                _chamsOpacity.Value.ToString("0.00"));
            _chamsOpacity.Value = GUILayout.HorizontalSlider(
                _chamsOpacity.Value, 0.1f, 1f);
            DrawOptionSlider(
                "Limb width",
                _chamsLimbWidth, 0.01f, 0.12f, "0.000");
            if (DrawResetGroupButton())
            {
                _chamsEnabled.Value = false;
                _chamsCharacters.Value = true;
                _chamsPerLimbVisibility.Value = true;
                _chamsShowPmc.Value = true;
                _chamsShowScav.Value = true;
                _chamsShowBoss.Value = true;
                for (int i = 0; i < _espRoles.Count; i++)
                {
                    EspRoleSettings role = _espRoles[i];
                    role.ChamsEnabled.Value = true;
                    role.ChamVisibleColor.Value =
                        "#" + ColorUtility.ToHtmlStringRGBA(
                            role.DefaultVisible);
                    role.ChamHiddenColor.Value =
                        "#" + ColorUtility.ToHtmlStringRGBA(
                            role.DefaultHidden);
                }
                _chamsMaxDistance.Value = 250f;
                _chamsOpacity.Value = 0.65f;
                _chamsLimbWidth.Value = 0.045f;
                ResetChamColors();
                ResetWorldChamSettings();
            }
            EndCategoryPanel();
            EndCategoryColumns();

            GUILayout.EndScrollView();
        }

        private void DrawExtractionEspRow()
        {
            GUILayout.BeginHorizontal();
            DrawOptionToggleLabel(
                _showExtractions,
                " Map extractions",
                GUILayout.ExpandWidth(true));
            DrawColorSquare(
                _extractionColor,
                GetExtractionColor(false),
                new Color(0.96f, 0.62f, 0.04f, 1f),
                "Unavailable map exits");
            DrawColorSquare(
                _usableExtractionColor,
                GetExtractionColor(true),
                new Color(0.13f, 0.77f, 0.37f, 1f),
                "Usable map exits");
            GUILayout.Space(76f);
            GUILayout.EndHorizontal();
        }

        private Color GetExtractionColor(bool usable)
        {
            return usable
                ? ParseVisualColor(
                    _usableExtractionColor.Value,
                    new Color(0.13f, 0.77f, 0.37f, 1f))
                : ParseVisualColor(
                    _extractionColor.Value,
                    new Color(0.96f, 0.62f, 0.04f, 1f));
        }

        private void DrawRoleColorRow(EspRoleSettings role)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(
                role.FollowerSubcategory
                    ? 68f
                    : 42f);
            DrawOptionToggleLabel(
                role.Enabled,
                " " + RoleLeafName(role),
                GUILayout.ExpandWidth(true));
            DrawColorSquare(
                role.VisibleColor,
                ParseVisualColor(
                    role.VisibleColor.Value,
                    role.DefaultVisible),
                role.DefaultVisible,
                role.Label + " visible");
            DrawColorSquare(
                role.HiddenColor,
                ParseVisualColor(
                    role.HiddenColor.Value,
                    role.DefaultHidden),
                role.DefaultHidden,
                role.Label + " hidden");
            DrawOptionHotkey(role.Enabled);
            GUILayout.EndHorizontal();
        }

        private void DrawAllRoleRow()
        {
            int selected = CountEnabledRoles(_espRoles);
            bool all = selected == _espRoles.Count && _espRoles.Count > 0;
            bool any = selected > 0;

            GUILayout.BeginHorizontal();
            if (DrawRoleFoldoutButton(_espAllRolesExpanded))
            {
                _espAllRolesExpanded = !_espAllRolesExpanded;
                for (int i = 0; i < _espRoleGroups.Count; i++)
                    _espRoleGroups[i].Expanded = _espAllRolesExpanded;
            }
            bool toggled = GUILayout.Toggle(
                all,
                (any && !all ? " Some" : " All") +
                " roles (" + selected + "/" + _espRoles.Count + ")",
                GUILayout.ExpandWidth(true));
            if (toggled != all)
                SetRolesEnabled(_espRoles, toggled);
            GUILayout.Space(52f);
            GUILayout.Space(52f);
            GUILayout.Space(76f);
            GUILayout.EndHorizontal();
        }

        private void DrawRoleGroup(EspRoleGroup group)
        {
            int selected = CountEnabledRoles(group.Roles);
            bool all = selected == group.Roles.Count &&
                       group.Roles.Count > 0;
            bool any = selected > 0;

            GUILayout.BeginHorizontal();
            GUILayout.Space(13f);
            if (DrawRoleFoldoutButton(group.Expanded))
                group.Expanded = !group.Expanded;
            bool toggled = GUILayout.Toggle(
                all,
                (any && !all ? " Some " : " ") +
                group.Name + " (" + selected + "/" +
                group.Roles.Count + ")",
                GUILayout.ExpandWidth(true));
            if (toggled != all)
                SetRolesEnabled(group.Roles, toggled);
            GUILayout.Space(52f);
            GUILayout.Space(52f);
            GUILayout.Space(76f);
            GUILayout.EndHorizontal();

            if (!group.Expanded)
                return;
            for (int i = 0; i < group.Roles.Count; i++)
                DrawRoleColorRow(group.Roles[i]);
        }

        private bool DrawRoleFoldoutButton(bool expanded)
        {
            if (_espRoleFoldoutStyle == null)
            {
                _espRoleFoldoutStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fixedWidth = 25f,
                        fixedHeight = 24f,
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        padding = new RectOffset(0, 0, 0, 1),
                        margin = new RectOffset(2, 4, 1, 1)
                    };
            }
            return GUILayout.Button(
                expanded ? "\u25BC" : "\u25B6",
                _espRoleFoldoutStyle);
        }

        private static int CountEnabledRoles(
            System.Collections.Generic.IList<EspRoleSettings> roles)
        {
            int count = 0;
            for (int i = 0; i < roles.Count; i++)
            {
                if (roles[i].Enabled.Value)
                    count++;
            }
            return count;
        }

        private static void SetRolesEnabled(
            System.Collections.Generic.IList<EspRoleSettings> roles,
            bool enabled)
        {
            for (int i = 0; i < roles.Count; i++)
                roles[i].Enabled.Value = enabled;
        }

        private static string RoleLeafName(EspRoleSettings role)
        {
            string prefix = role.Group + " - ";
            string leaf = role.Label.StartsWith(
                    prefix, System.StringComparison.Ordinal)
                ? role.Label.Substring(prefix.Length)
                : role.Label;
            return role.FollowerSubcategory
                ? "Follower / " + leaf
                : leaf;
        }

        private static void DrawColorColumnHeaders()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                "",
                GUILayout.ExpandWidth(true));
            GUILayout.Label(
                "Visible",
                GUILayout.Width(52f));
            GUILayout.Label(
                "Hidden",
                GUILayout.Width(52f));
            GUILayout.Label(
                "Hotkey",
                GUILayout.Width(76f));
            GUILayout.EndHorizontal();
        }

        private void DrawColorSquare(
            ConfigEntry<string> setting,
            Color color,
            Color fallback,
            string label)
        {
            Rect slot = GUILayoutUtility.GetRect(
                52f, 24f, GUILayout.Width(52f), GUILayout.Height(24f));
            Rect rect = new Rect(
                slot.x + 14f, slot.y, 24f, 24f);
            if (GUI.Button(
                rect,
                new GUIContent(
                    "",
                    OptionDescription(setting))))
            {
                _openColorSetting = setting;
                _openColorLabel = label;
                _openColorFallback = fallback;
            }

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(rect.x + 4f, rect.y + 4f, 16f, 16f),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawChamRoleColorRow(EspRoleSettings role)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(
                role.FollowerSubcategory
                    ? 68f
                    : 42f);
            DrawOptionToggleLabel(
                role.ChamsEnabled,
                " " + RoleLeafName(role),
                GUILayout.ExpandWidth(true));
            DrawColorSquare(
                role.ChamVisibleColor,
                GetRoleChamColor(role, false),
                role.DefaultVisible,
                role.Label + " chams visible");
            DrawColorSquare(
                role.ChamHiddenColor,
                GetRoleChamColor(role, true),
                role.DefaultHidden,
                role.Label + " chams hidden");
            DrawOptionHotkey(role.ChamsEnabled);
            GUILayout.EndHorizontal();
        }

        private void DrawAllChamRoleRow()
        {
            int selected = CountEnabledChamRoles(_espRoles);
            bool all = selected == _espRoles.Count && _espRoles.Count > 0;
            bool any = selected > 0;

            GUILayout.BeginHorizontal();
            if (DrawRoleFoldoutButton(_chamAllRolesExpanded))
            {
                _chamAllRolesExpanded = !_chamAllRolesExpanded;
                for (int i = 0; i < _espRoleGroups.Count; i++)
                    _espRoleGroups[i].ChamsExpanded =
                        _chamAllRolesExpanded;
            }
            bool toggled = GUILayout.Toggle(
                all,
                (any && !all ? " Some" : " All") +
                " roles (" + selected + "/" + _espRoles.Count + ")",
                GUILayout.ExpandWidth(true));
            if (toggled != all)
                SetChamRolesEnabled(_espRoles, toggled);
            GUILayout.Space(52f);
            GUILayout.Space(52f);
            GUILayout.Space(76f);
            GUILayout.EndHorizontal();
        }

        private void DrawChamRoleGroup(EspRoleGroup group)
        {
            int selected = CountEnabledChamRoles(group.Roles);
            bool all = selected == group.Roles.Count &&
                       group.Roles.Count > 0;
            bool any = selected > 0;

            GUILayout.BeginHorizontal();
            GUILayout.Space(13f);
            if (DrawRoleFoldoutButton(group.ChamsExpanded))
                group.ChamsExpanded = !group.ChamsExpanded;
            bool toggled = GUILayout.Toggle(
                all,
                (any && !all ? " Some " : " ") +
                group.Name + " (" + selected + "/" +
                group.Roles.Count + ")",
                GUILayout.ExpandWidth(true));
            if (toggled != all)
                SetChamRolesEnabled(group.Roles, toggled);
            GUILayout.Space(52f);
            GUILayout.Space(52f);
            GUILayout.Space(76f);
            GUILayout.EndHorizontal();

            if (!group.ChamsExpanded)
                return;
            for (int i = 0; i < group.Roles.Count; i++)
                DrawChamRoleColorRow(group.Roles[i]);
        }

        private static int CountEnabledChamRoles(
            System.Collections.Generic.IList<EspRoleSettings> roles)
        {
            int count = 0;
            for (int i = 0; i < roles.Count; i++)
            {
                if (roles[i].ChamsEnabled.Value)
                    count++;
            }
            return count;
        }

        private static void SetChamRolesEnabled(
            System.Collections.Generic.IList<EspRoleSettings> roles,
            bool enabled)
        {
            for (int i = 0; i < roles.Count; i++)
                roles[i].ChamsEnabled.Value = enabled;
        }

        private void DrawWorldChamRow(
            ConfigEntry<bool> enabled,
            string label,
            WorldChamKind kind)
        {
            GUILayout.BeginHorizontal();
            DrawOptionToggleLabel(
                enabled, label, GUILayout.ExpandWidth(true));
            DrawColorSquare(
                GetWorldChamColorSetting(kind),
                GetWorldChamColor(kind),
                GetWorldChamFallback(kind),
                GetWorldChamName(kind));
            GUILayout.Space(52f);
            DrawOptionHotkey(enabled);
            GUILayout.EndHorizontal();
        }

        private void DrawColorPickerPopout()
        {
            if (_openColorSetting == null)
                return;

            _colorPickerRect.width = 390f;
            _colorPickerRect.height = 250f;
            _colorPickerRect = GUI.Window(
                731909,
                _colorPickerRect,
                DrawColorPickerWindow,
                "Color Picker");
        }

        private void DrawColorPickerWindow(int windowId)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(_openColorLabel, _sectionTitleStyle);
            if (GUILayout.Button("Close", GUILayout.Width(64f)))
                _openColorSetting = null;
            GUILayout.EndHorizontal();

            if (_openColorSetting != null &&
                DrawRgbaColorPicker(
                    "RGBA", _openColorSetting, _openColorFallback))
                ApplyConfiguredTargetColors();

            GUI.DragWindow(
                new Rect(0f, 0f, _colorPickerRect.width, 24f));
        }

        private bool DrawRgbaColorPicker(
            string label,
            ConfigEntry<string> setting,
            Color fallback)
        {
            Color color = ParseVisualColor(setting.Value, fallback);
            GUILayout.Label(
                label + "  #" + ColorUtility.ToHtmlStringRGBA(color));

            Rect preview = GUILayoutUtility.GetRect(
                1f, 16f, GUILayout.ExpandWidth(true));
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(preview, Texture2D.whiteTexture);
            GUI.color = previousColor;

            float red = DrawColorChannel("R", color.r);
            float green = DrawColorChannel("G", color.g);
            float blue = DrawColorChannel("B", color.b);
            float alpha = DrawColorChannel("A", color.a);
            Color updated = new Color(red, green, blue, alpha);

            if (ApproximatelyEqual(color, updated))
                return false;

            setting.Value = "#" + ColorUtility.ToHtmlStringRGBA(updated);
            return true;
        }

        private static float DrawColorChannel(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                label + " " + Mathf.RoundToInt(value * 255f),
                GUILayout.Width(48f));
            value = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.EndHorizontal();
            return value;
        }

        private static bool ApproximatelyEqual(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r) &&
                   Mathf.Approximately(left.g, right.g) &&
                   Mathf.Approximately(left.b, right.b) &&
                   Mathf.Approximately(left.a, right.a);
        }

        private Color GetVisualColor(EspKind kind, bool occluded = false)
        {
            ConfigEntry<string> setting = occluded
                ? GetOccludedColorSetting(kind)
                : GetVisibleColorSetting(kind);
            Color fallback = occluded
                ? GetOccludedFallback(kind)
                : GetVisualFallback(kind);
            return setting != null
                ? ParseVisualColor(setting.Value, fallback)
                : fallback;
        }

        private ConfigEntry<string> GetVisibleColorSetting(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return _pmcVisualColor;
                case EspKind.Scav:
                    return _scavVisualColor;
                case EspKind.Boss:
                    return _bossVisualColor;
                default:
                    return null;
            }
        }

        private ConfigEntry<string> GetOccludedColorSetting(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return _pmcOccludedColor;
                case EspKind.Scav:
                    return _scavOccludedColor;
                case EspKind.Boss:
                    return _bossOccludedColor;
                default:
                    return null;
            }
        }

        private Color GetChamColor(EspKind kind, bool occluded = false)
        {
            ConfigEntry<string> setting =
                GetChamColorSetting(kind, occluded);
            Color fallback = occluded
                ? GetOccludedFallback(kind)
                : GetVisualFallback(kind);
            return setting != null
                ? ParseVisualColor(setting.Value, fallback)
                : fallback;
        }

        private ConfigEntry<string> GetChamColorSetting(
            EspKind kind,
            bool occluded)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return occluded
                        ? _pmcChamOccludedColor
                        : _pmcChamColor;
                case EspKind.Scav:
                    return occluded
                        ? _scavChamOccludedColor
                        : _scavChamColor;
                case EspKind.Boss:
                    return occluded
                        ? _bossChamOccludedColor
                        : _bossChamColor;
                default:
                    return null;
            }
        }

        private static Color GetVisualFallback(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return new Color(1f, 0.25f, 0.25f, 1f);
                case EspKind.Scav:
                    return new Color(1f, 0.85f, 0.1f, 1f);
                case EspKind.Boss:
                    return new Color(1f, 0.15f, 0.9f, 1f);
                default:
                    return Color.white;
            }
        }

        private static Color GetOccludedFallback(EspKind kind)
        {
            Color visible = GetVisualFallback(kind);
            return new Color(
                visible.r * 0.3f,
                visible.g * 0.3f,
                visible.b * 0.3f,
                visible.a * 0.75f);
        }

        private void ResetVisualColors()
        {
            _pmcVisualColor.Value = "#FF4040FF";
            _scavVisualColor.Value = "#FFD91AFF";
            _bossVisualColor.Value = "#FF26E6FF";
            _pmcOccludedColor.Value = "#4D1313BF";
            _scavOccludedColor.Value = "#4D4108BF";
            _bossOccludedColor.Value = "#4D0745BF";
            ApplyConfiguredTargetColors();
        }

        private void ResetChamColors()
        {
            _pmcChamColor.Value = "#FF4040FF";
            _scavChamColor.Value = "#FFD91AFF";
            _bossChamColor.Value = "#FF26E6FF";
            _pmcChamOccludedColor.Value = "#4D1313BF";
            _scavChamOccludedColor.Value = "#4D4108BF";
            _bossChamOccludedColor.Value = "#4D0745BF";
        }

        private bool IsWorldChamEnabled(WorldChamKind kind)
        {
            switch (kind)
            {
                case WorldChamKind.Corpse:
                    return _chamsCorpses.Value;
                case WorldChamKind.Loot:
                    return _chamsLoot.Value;
                default:
                    return false;
            }
        }

        private Color GetWorldChamColor(WorldChamKind kind)
        {
            ConfigEntry<string> setting =
                GetWorldChamColorSetting(kind);
            Color fallback = GetWorldChamFallback(kind);
            return setting != null
                ? ParseVisualColor(setting.Value, fallback)
                : fallback;
        }

        private ConfigEntry<string> GetWorldChamColorSetting(
            WorldChamKind kind)
        {
            switch (kind)
            {
                case WorldChamKind.Corpse:
                    return _chamsCorpseColor;
                case WorldChamKind.Loot:
                    return _chamsLootColor;
                default:
                    return null;
            }
        }

        private static Color GetWorldChamFallback(WorldChamKind kind)
        {
            switch (kind)
            {
                case WorldChamKind.Corpse:
                    return new Color(0.66f, 0.33f, 0.97f, 0.5f);
                case WorldChamKind.Loot:
                    return new Color(0.13f, 0.83f, 0.93f, 0.5f);
                default:
                    return Color.white;
            }
        }

        private static string GetWorldChamName(WorldChamKind kind)
        {
            switch (kind)
            {
                case WorldChamKind.Corpse:
                    return "Corpses";
                case WorldChamKind.Loot:
                    return "Loot";
                default:
                    return "World";
            }
        }

        private void ResetWorldChamSettings()
        {
            _chamsCorpses.Value = false;
            _chamsLoot.Value = false;
            _cullGrass.Value = false;
            _lootRenderDistance.Value = 250f;
            _chamsCorpseColor.Value = "#A855F780";
            _chamsLootColor.Value = "#22D3EE80";
        }

        private static Color ParseVisualColor(
            string value,
            Color fallback)
        {
            Color parsed;
            return !string.IsNullOrEmpty(value) &&
                   ColorUtility.TryParseHtmlString(value, out parsed)
                ? parsed
                : fallback;
        }

        private void ApplyConfiguredTargetColors()
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                target.Color = GetRoleColor(target, false);
                target.DisplayColor = GetDisplayColor(target);
            }
        }

        private void DrawEspQuickInfo()
        {
            GUILayout.Label(
                "ESP: " + (_enabled.Value ? "Enabled" : "Disabled"));
            GUILayout.Label(
                "Chams: " +
                (_chamsEnabled.Value ? "Enabled" : "Disabled"));
            GUILayout.Label("Tracked targets: " + _targets.Count);
            GUILayout.Space(5f);
            GUILayout.Label("Insert  Open menu");
            GUILayout.Label("Home    Toggle ESP");
            GUILayout.Label("F9      Toggle Chams");
        }
    }
}
