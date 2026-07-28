
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void DrawEntityMenu()
        {
            HandleEntitySearchKeyboard();

            _entityListSection = GUILayout.Toolbar(
                _entityListSection,
                new[] { "Visuals", "Live Entity List", "Live Item List" });
            GUILayout.Space(6f);

            if (_entityListSection == 0)
            {
                DrawEspMenu();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", GUILayout.Width(48f));
            GUI.SetNextControlName("FieldKit.EntitySearch");
            _entitySearch = GUILayout.TextField(
                _entitySearch ?? "",
                GUILayout.ExpandWidth(true));
            if (GUILayout.Button("×", GUILayout.Width(28f)))
            {
                _entitySearch = "";
                GUI.FocusControl(null);
            }
            if (GUILayout.Button(
                "Refresh",
                GUILayout.Width(80f)))
            {
                _nextEntityListRefresh = 0f;
            }
            GUILayout.EndHorizontal();

            int count = _entityListSection == 1
                ? _liveEntityEntries.Count
                : _liveLootEntries.Count;
            GUILayout.Label(
                count + (_entityListSection == 1
                    ? " living entities"
                    : " loose loot objects"));

            _entityListScroll = BeginVerticalScrollView(
                _entityListScroll,
                GUILayout.Height(
                    Mathf.Max(300f, MenuHeight - 170f)));

            if (_world == null || _localPlayer == null)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(
                    "Enter a raid to inspect live entities.");
                GUILayout.EndVertical();
            }
            else if (_entityListSection == 1)
            {
                DrawLiveEntityRows();
            }
            else
            {
                DrawLiveLootRows();
            }

            GUILayout.Space(10f);
            GUILayout.EndScrollView();
        }

        private void HandleEntitySearchKeyboard()
        {
            Event current = Event.current;
            if (current == null)
                return;

            bool keyDown =
                current.type == EventType.KeyDown ||
                current.rawType == EventType.KeyDown;
            if (!keyDown)
                return;

            if (current.keyCode == _menuKey.Value.MainKey &&
                AreShortcutModifiersHeld(_menuKey.Value))
            {
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                SetMenuOpen(false);
                _menuShortcutLatched = true;
                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.Escape &&
                GUI.GetNameOfFocusedControl() ==
                "FieldKit.EntitySearch")
            {
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                current.Use();
            }
        }

        private void DrawLiveEntityRows()
        {
            for (int i = 0; i < _liveEntityEntries.Count; i++)
            {
                LiveEntityEntry entry = _liveEntityEntries[i];
                if (entry.Player == null ||
                    !EntitySearchMatches(
                        entry.Name,
                        entry.Kind))
                {
                    continue;
                }

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.BeginVertical(
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(entry.Name, _sectionTitleStyle);
                GUILayout.Label(
                    entry.Kind + "  •  " +
                    entry.Distance.ToString("0.0") + "m");
                GUILayout.EndVertical();

                bool isLocal =
                    ReferenceEquals(
                        entry.Player,
                        _localPlayer);
                GUI.enabled = !isLocal;
                if (GUILayout.Button(
                    "Bring Here",
                    GUILayout.Width(105f)))
                {
                    BringEntityToSelf(entry.Player);
                }
                if (GUILayout.Button(
                    "Go To",
                    GUILayout.Width(80f)))
                {
                    TeleportSelfToEntity(entry.Player);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawLiveLootRows()
        {
            for (int i = 0; i < _liveLootEntries.Count; i++)
            {
                LiveLootEntry entry = _liveLootEntries[i];
                if (entry.Loot == null ||
                    !EntitySearchMatches(
                        entry.Name,
                        entry.TemplateId))
                {
                    continue;
                }

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.BeginVertical(
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(entry.Name, _sectionTitleStyle);
                GUILayout.Label(
                    entry.Distance.ToString("0.0") + "m" +
                    (entry.Price > 0f
                        ? "  •  " +
                          FormatLootPrice(entry.Price)
                        : "") +
                    "  •  " + entry.TemplateId);
                GUILayout.EndVertical();

                if (GUILayout.Button(
                    "Bring Here",
                    GUILayout.Width(105f)))
                {
                    BringLootToSelf(entry.Loot);
                }
                if (GUILayout.Button(
                    "Go To",
                    GUILayout.Width(80f)))
                {
                    TeleportSelfToLoot(entry.Loot);
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
