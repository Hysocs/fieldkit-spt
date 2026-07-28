
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private static string OptionDescription<T>(
            BepInEx.Configuration.ConfigEntry<T> setting)
        {
            return setting != null &&
                   setting.Description != null
                ? setting.Description.Description
                : "";
        }

        private bool DrawOptionToggle(
            BepInEx.Configuration.ConfigEntry<bool> setting,
            string label,
            params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            DrawOptionToggleLabel(setting, label);
            DrawOptionHotkey(setting);
            GUILayout.EndHorizontal();
            return setting.Value;
        }

        private bool DrawOptionToggleLabel(
            BepInEx.Configuration.ConfigEntry<bool> setting,
            string label,
            params GUILayoutOption[] options)
        {
            setting.Value = GUILayout.Toggle(
                setting.Value,
                new GUIContent(label, OptionDescription(setting)),
                options);
            return setting.Value;
        }

        private void DrawOptionHotkey(
            BepInEx.Configuration.ConfigEntry<bool> setting)
        {
            HandleInlineToggleHotkey(
                setting,
                GetToggleHotkeyLabel(setting));
        }

        private static Vector2 BeginVerticalScrollView(
            Vector2 position,
            params GUILayoutOption[] options)
        {
            return GUILayout.BeginScrollView(
                position,
                false,
                true,
                GUIStyle.none,
                GUI.skin.verticalScrollbar,
                GUI.skin.scrollView,
                options);
        }

        private static void DrawOptionSlider(
            string label,
            BepInEx.Configuration.ConfigEntry<float> setting,
            float minimum,
            float maximum,
            string format)
        {
            string description = OptionDescription(setting);
            string displayValue = format.EndsWith(
                    "%",
                    System.StringComparison.Ordinal)
                ? setting.Value.ToString(
                      format.Substring(0, format.Length - 1)) + "%"
                : setting.Value.ToString(format);
            GUILayout.Label(
                new GUIContent(
                    label + ": " + displayValue,
                    description));
            Rect labelRect = GUILayoutUtility.GetLastRect();
            setting.Value = GUILayout.HorizontalSlider(
                setting.Value, minimum, maximum);
            Rect sliderRect = GUILayoutUtility.GetLastRect();
            GUI.Label(
                Rect.MinMaxRect(
                    Mathf.Min(labelRect.xMin, sliderRect.xMin),
                    labelRect.yMin,
                    Mathf.Max(labelRect.xMax, sliderRect.xMax),
                    sliderRect.yMax),
                new GUIContent("", description));
        }

        private static void DrawOptionSlider(
            string label,
            BepInEx.Configuration.ConfigEntry<int> setting,
            int minimum,
            int maximum,
            string format)
        {
            string description = OptionDescription(setting);
            GUILayout.Label(
                new GUIContent(
                    label + ": " + setting.Value.ToString(format),
                    description));
            Rect labelRect = GUILayoutUtility.GetLastRect();
            setting.Value = Mathf.RoundToInt(
                GUILayout.HorizontalSlider(
                    setting.Value, minimum, maximum));
            Rect sliderRect = GUILayoutUtility.GetLastRect();
            GUI.Label(
                Rect.MinMaxRect(
                    Mathf.Min(labelRect.xMin, sliderRect.xMin),
                    labelRect.yMin,
                    Mathf.Max(labelRect.xMax, sliderRect.xMax),
                    sliderRect.yMax),
                new GUIContent("", description));
        }

        private void DrawOptionTooltip()
        {
            if (_optionTooltipStyle == null ||
                Event.current.type != EventType.Repaint)
                return;

            string tooltip = GUI.tooltip;
            if (string.IsNullOrEmpty(tooltip))
            {
                _pendingOptionTooltip = null;
                _optionTooltipHoverStarted = 0f;
                return;
            }

            if (!string.Equals(
                    tooltip,
                    _pendingOptionTooltip,
                    System.StringComparison.Ordinal))
            {
                _pendingOptionTooltip = tooltip;
                _optionTooltipHoverStarted = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _optionTooltipHoverStarted < 1f)
                return;

            const float maximumWidth = 360f;
            GUIContent content = new GUIContent(tooltip);
            float width = Mathf.Min(
                maximumWidth,
                Mathf.Max(
                    180f,
                    _optionTooltipStyle.CalcSize(content).x));
            float height = _optionTooltipStyle.CalcHeight(
                content, width);
            Vector2 mouse = Event.current.mousePosition;
            Rect rect = new Rect(
                mouse.x + 16f,
                mouse.y + 18f,
                width,
                height);
            rect.x = Mathf.Clamp(
                rect.x, 4f, Mathf.Max(4f, _menuRect.width - width - 4f));
            rect.y = Mathf.Clamp(
                rect.y, 28f, Mathf.Max(28f, _menuRect.height - height - 4f));
            GUI.Label(rect, content, _optionTooltipStyle);
        }

        private int DrawDropdown(
            string id,
            int selectedIndex,
            string[] options,
            string tooltip = "")
        {
            selectedIndex = Mathf.Clamp(
                selectedIndex,
                0,
                options.Length - 1);

            bool clicked = GUILayout.Button(
                new GUIContent(options[selectedIndex], tooltip),
                _dropdownButtonStyle);
            Rect anchor = GUILayoutUtility.GetLastRect();
            GUI.Label(
                new Rect(
                    anchor.xMax - 27f,
                    anchor.y,
                    24f,
                    anchor.height),
                "\u25BE",
                _dropdownArrowStyle);

            if (clicked)
            {
                if (_openDropdownId == id)
                    CloseDropdown();
                else
                    _openDropdownId = id;
            }

            if (_openDropdownId == id)
            {
                GUILayout.BeginVertical(_dropdownMenuStyle);
                for (int i = 0; i < options.Length; i++)
                {
                    bool selected = i == selectedIndex;
                    bool next = GUILayout.Toggle(
                        selected,
                        options[i],
                        _dropdownItemStyle);
                    if (next != selected)
                    {
                        if (!selected)
                            selectedIndex = i;
                        CloseDropdown();
                    }
                }
                GUILayout.EndVertical();
            }

            return selectedIndex;
        }

        private void CloseDropdown()
        {
            _openDropdownId = null;
        }
    }
}
