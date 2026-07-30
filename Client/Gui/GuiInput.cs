
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void SetMenuOpen(bool open)
        {
            if (_menuOpen == open)
            {
                if (open)
                    MaintainMenuCursor();
                return;
            }

            _menuOpen = open;

            if (open)
            {
                _previousCursorLock = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                MaintainMenuCursor();
                MaintainGameUiInputBlock();
            }
            else
            {
                ReleaseGameUiInputBlock();
                if (_menuCursorApplied)
                {
                    Cursor.SetCursor(
                        null,
                        Vector2.zero,
                        CursorMode.Auto);
                    _menuCursorApplied = false;
                }
                Cursor.lockState = _previousCursorLock;
                Cursor.visible = _previousCursorVisible;
            }
        }

        private void MaintainMenuCursor()
        {
            if (!_menuOpen)
                return;

            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;

            EnsureMenuCursorTexture();
            if (_menuCursorTexture != null)
            {
                Cursor.SetCursor(
                    _menuCursorTexture,
                    Vector2.zero,
                    CursorMode.ForceSoftware);
                _menuCursorApplied = true;
            }

            MaintainGameUiInputBlock();
        }

        private void MaintainGameUiInputBlock()
        {
            if (!_menuOpen)
                return;

            UnityEngine.EventSystems.EventSystem current =
                UnityEngine.EventSystems.EventSystem.current;
            if (current == null)
                return;

            if (_blockedEventSystem != current)
            {
                ReleaseGameUiInputBlock();
                _blockedEventSystem = current;
                _blockedEventSystemWasEnabled = current.enabled;
            }

            if (current.enabled)
                current.enabled = false;
        }

        private void ReleaseGameUiInputBlock()
        {
            if (_blockedEventSystem != null)
            {
                _blockedEventSystem.enabled =
                    _blockedEventSystemWasEnabled;
            }

            _blockedEventSystem = null;
            _blockedEventSystemWasEnabled = false;
        }

        private void HandleMenuShortcutUpdate()
        {
            KeyboardShortcut shortcut = _menuKey.Value;

            if (shortcut.MainKey == KeyCode.None)
                return;

            if (Input.GetKeyUp(shortcut.MainKey) ||
                !Input.GetKey(shortcut.MainKey))
                _menuShortcutLatched = false;
            if (Input.GetKeyDown(shortcut.MainKey) &&
                AreShortcutModifiersHeld(shortcut))
                ToggleMenuFromShortcut();
        }

        private static bool AreShortcutModifiersHeld(
            KeyboardShortcut shortcut)
        {
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!Input.GetKey(modifier))
                    return false;
            }

            return true;
        }

        private void HandleMenuShortcutGuiEvent()
        {
            Event current = Event.current;
            KeyboardShortcut shortcut = _menuKey.Value;

            if (current == null || shortcut.MainKey == KeyCode.None)
                return;

            if (current.type == EventType.KeyUp &&
                current.keyCode == shortcut.MainKey)
            {
                _menuShortcutLatched = false;
                return;
            }

            if (current.type != EventType.KeyDown ||
                current.keyCode != shortcut.MainKey)
                return;

            if (!AreShortcutModifiersHeld(shortcut))
                return;

            ToggleMenuFromShortcut();
            current.Use();
        }

        private void ToggleMenuFromShortcut()
        {
            if (_menuShortcutLatched)
                return;

            _menuShortcutLatched = true;
            GUI.FocusControl(null);
            SetMenuOpen(!_menuOpen);
        }
    }
}
