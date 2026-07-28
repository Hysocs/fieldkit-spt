
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private readonly Dictionary<
            ConfigEntry<bool>,
            ConfigEntry<KeyboardShortcut>> _toggleHotkeys =
                new Dictionary<
                    ConfigEntry<bool>,
                    ConfigEntry<KeyboardShortcut>>();
        private ConfigEntry<bool> _toggleAwaitingHotkey;
        private int _toggleHotkeyCaptureStartedFrame = -1;
        private ConfigEntry<KeyboardShortcut> _standaloneAwaitingHotkey;

        private void ConfigureToggleHotkeys()
        {
            MapExistingToggleHotkey(_enabled, _espKey);
            MapExistingToggleHotkey(_godMode, _godModeKey);
            MapExistingToggleHotkey(
                _infiniteStamina, _staminaKey);
            MapExistingToggleHotkey(_noWeight, _noWeightKey);
            MapExistingToggleHotkey(_chamsEnabled, _chamsKey);

            List<ConfigEntry<bool>> toggles =
                new List<ConfigEntry<bool>>();
            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> pair
                     in Config)
            {
                ConfigEntry<bool> toggle =
                    pair.Value as ConfigEntry<bool>;
                if (toggle != null &&
                    !_toggleHotkeys.ContainsKey(toggle))
                    toggles.Add(toggle);
            }

            for (int i = 0; i < toggles.Count; i++)
            {
                ConfigEntry<bool> toggle = toggles[i];
                string name =
                    toggle.Definition.Section + " - " +
                    toggle.Definition.Key;
                ConfigEntry<KeyboardShortcut> hotkey = Config.Bind(
                    "Toggle Hotkeys",
                    name,
                    KeyboardShortcut.Empty,
                    "Toggle " + toggle.Definition.Section + " / " +
                    toggle.Definition.Key + ".");
                _toggleHotkeys.Add(toggle, hotkey);
            }

        }

        private void MapExistingToggleHotkey(
            ConfigEntry<bool> toggle,
            ConfigEntry<KeyboardShortcut> hotkey)
        {
            if (toggle != null && hotkey != null)
                _toggleHotkeys[toggle] = hotkey;
        }

        private void UpdateToggleHotkeys()
        {
            CaptureAwaitingToggleHotkey();

            foreach (KeyValuePair<
                     ConfigEntry<bool>,
                     ConfigEntry<KeyboardShortcut>> pair
                     in _toggleHotkeys)
            {
                KeyboardShortcut shortcut = pair.Value.Value;
                if (!IsBindableKey(shortcut.MainKey))
                {
                    if (shortcut.MainKey != KeyCode.None)
                        pair.Value.Value = KeyboardShortcut.Empty;
                    continue;
                }

                if (shortcut.MainKey != KeyCode.None &&
                    shortcut.IsDown())
                    pair.Key.Value = !pair.Key.Value;
            }
        }

        private void CaptureAwaitingToggleHotkey()
        {
            if ((_toggleAwaitingHotkey == null &&
                 _standaloneAwaitingHotkey == null) ||
                Time.frameCount <= _toggleHotkeyCaptureStartedFrame ||
                !Input.anyKeyDown)
                return;

            ConfigEntry<KeyboardShortcut> hotkey;
            if (_standaloneAwaitingHotkey != null)
            {
                hotkey = _standaloneAwaitingHotkey;
            }
            else if (!_toggleHotkeys.TryGetValue(
                         _toggleAwaitingHotkey, out hotkey))
            {
                FinishToggleHotkeyCapture();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                hotkey.Value = KeyboardShortcut.Empty;
                FinishToggleHotkeyCapture();
                return;
            }

            Array values = Enum.GetValues(typeof(KeyCode));
            for (int i = 0; i < values.Length; i++)
            {
                KeyCode key = (KeyCode)values.GetValue(i);
                if (!IsBindableKey(key) ||
                    !Input.GetKeyDown(key))
                    continue;

                hotkey.Value = new KeyboardShortcut(
                    key,
                    CurrentHotkeyModifiers());
                FinishToggleHotkeyCapture();
                return;
            }
        }

        private void FinishToggleHotkeyCapture()
        {
            _toggleAwaitingHotkey = null;
            _standaloneAwaitingHotkey = null;
            _toggleHotkeyCaptureStartedFrame = -1;
        }

        private void DrawStandaloneHotkey(
            ConfigEntry<KeyboardShortcut> hotkey)
        {
            string label = ReferenceEquals(
                    _standaloneAwaitingHotkey, hotkey)
                ? "[Press key]"
                : FormatToggleHotkey(hotkey.Value);
            if (GUILayout.Button(
                    label,
                    GUI.skin.label,
                    GUILayout.Width(76f)))
            {
                _toggleAwaitingHotkey = null;
                _standaloneAwaitingHotkey = hotkey;
                _toggleHotkeyCaptureStartedFrame = Time.frameCount;
            }
        }

        private string GetToggleHotkeyLabel(
            ConfigEntry<bool> toggle)
        {
            ConfigEntry<KeyboardShortcut> hotkey;
            if (!_toggleHotkeys.TryGetValue(toggle, out hotkey))
                return null;

            return ReferenceEquals(_toggleAwaitingHotkey, toggle)
                ? "[Press key]"
                : FormatToggleHotkey(hotkey.Value);
        }

        private void HandleInlineToggleHotkey(
            ConfigEntry<bool> toggle,
            string hotkeyLabel)
        {
            if (string.IsNullOrEmpty(hotkeyLabel))
            {
                DrawHotkeyColumnSpacer();
                return;
            }

            if (GUILayout.Button(
                    hotkeyLabel,
                    GUI.skin.label,
                    GUILayout.Width(76f)))
            {
                if (_toggleHotkeys.ContainsKey(toggle))
                {
                    _toggleAwaitingHotkey = toggle;
                    _toggleHotkeyCaptureStartedFrame = Time.frameCount;
                }
                return;
            }
        }

        private static void DrawHotkeyColumnSpacer()
        {
            GUILayout.Space(76f);
        }

        private static bool IsBindableKey(KeyCode key)
        {
            if (key == KeyCode.None ||
                key == KeyCode.F13 ||
                key == KeyCode.F14 ||
                key == KeyCode.F15 ||
                IsModifierKey(key))
                return false;

            string name = key.ToString();
            return !name.StartsWith(
                       "Mouse",
                       StringComparison.Ordinal) &&
                !name.StartsWith(
                    "Joystick",
                    StringComparison.Ordinal);
        }

        private static bool IsModifierKey(KeyCode key)
        {
            return key == KeyCode.LeftControl ||
                   key == KeyCode.RightControl ||
                   key == KeyCode.LeftShift ||
                   key == KeyCode.RightShift ||
                   key == KeyCode.LeftAlt ||
                   key == KeyCode.RightAlt ||
                   key == KeyCode.LeftCommand ||
                   key == KeyCode.RightCommand;
        }

        private static KeyCode[] CurrentHotkeyModifiers()
        {
            List<KeyCode> modifiers = new List<KeyCode>(3);
            if (Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl))
                modifiers.Add(KeyCode.LeftControl);
            if (Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift))
                modifiers.Add(KeyCode.LeftShift);
            if (Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt))
                modifiers.Add(KeyCode.LeftAlt);
            return modifiers.ToArray();
        }

        private static string FormatToggleHotkey(
            KeyboardShortcut shortcut)
        {
            return shortcut.MainKey == KeyCode.None
                ? "[--]"
                : "[" + shortcut + "]";
        }
    }
}
