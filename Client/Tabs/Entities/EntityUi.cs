
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void DrawEntityMenu()
        {
            HandleEntitySearchKeyboard();

            _entityListSection = GUILayout.Toolbar(
                _entityListSection,
                new[] { "Visuals", "Entity List", "Live Item List" });
            GUILayout.Space(6f);

            if (_entityListSection == 0)
            {
                DrawEspMenu();
                return;
            }

            bool spawnEntityView = false;
            if (_entityListSection == 1)
            {
                _liveEntitySubTab = GUILayout.Toolbar(
                    _liveEntitySubTab,
                    new[] { "Live Entities", "Spawn Entity" });
                GUILayout.Space(6f);
                spawnEntityView = _liveEntitySubTab == 1;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", GUILayout.Width(48f));
            GUI.SetNextControlName("FieldKit.EntitySearch");
            if (spawnEntityView)
            {
                _spawnEntitySearch = GUILayout.TextField(
                    _spawnEntitySearch ?? "",
                    GUILayout.ExpandWidth(true));
            }
            else
            {
                _entitySearch = GUILayout.TextField(
                    _entitySearch ?? "",
                    GUILayout.ExpandWidth(true));
            }
            if (GUILayout.Button("×", GUILayout.Width(28f)))
            {
                if (spawnEntityView)
                    _spawnEntitySearch = "";
                else
                    _entitySearch = "";
                GUI.FocusControl(null);
            }
            if (GUILayout.Button(
                "Refresh",
                GUILayout.Width(80f)))
            {
                if (spawnEntityView)
                    RefreshSpawnableAiCatalog(true);
                else
                    _nextEntityListRefresh = 0f;
            }
            GUILayout.EndHorizontal();

            if (spawnEntityView)
            {
                DrawSpawnEntityView();
                return;
            }

            int count = _entityListSection == 1
                ? _liveEntityEntries.Count
                : _liveLootEntries.Count;
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                count + (_entityListSection == 1
                    ? " living entities"
                    : " loose loot objects"));
            if (_entityListSection == 1)
            {
                if (GUILayout.Button(
                    "Kill All",
                    GUILayout.Width(75f)))
                    KillAllEntityBots();

                bool friendly = AreAllEntitiesFriendly();
                bool friendlyToggled = GUILayout.Toggle(
                    friendly,
                    " Friendly (all)",
                    GUILayout.Width(115f));
                if (friendlyToggled != friendly)
                    SetAllEntitiesFriendly(friendlyToggled);

                bool enabled = AreAllEntityAiEnabled();
                bool toggled = GUILayout.Toggle(
                    enabled,
                    " AI enabled (all)",
                    GUILayout.Width(130f));
                if (toggled != enabled)
                    SetAllEntityAiEnabled(toggled);
            }
            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint)
            {
                Rect listHeaderRect = GUILayoutUtility.GetLastRect();
                if (listHeaderRect.yMax > 0f)
                {
                    _entityListViewportHeight = Mathf.Max(
                        120f,
                        _menuRect.height - listHeaderRect.yMax - 28f);
                }
            }

            _entityListScroll = BeginVerticalScrollView(
                _entityListScroll,
                GUILayout.Height(_entityListViewportHeight));

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

            GUILayout.Space(18f);
            EndVerticalScrollView();
        }

        private void DrawSpawnEntityView()
        {
            RefreshSpawnableAiCatalog();

            GUILayout.BeginHorizontal();
            _spawnEntityAiDisabled = GUILayout.Toggle(
                _spawnEntityAiDisabled,
                " Spawn with AI disabled",
                GUILayout.Width(190f));
            _spawnEntityIgnoreNavMesh = GUILayout.Toggle(
                _spawnEntityIgnoreNavMesh,
                " Ignore NavMesh validation",
                GUILayout.Width(190f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Aim the center of the screen at a location. EFT will " +
                "validate the destination against its NavMesh.");
            GUILayout.Label(_spawnEntityStatus);

            if (Event.current.type == EventType.Repaint)
            {
                Rect headerRect = GUILayoutUtility.GetLastRect();
                if (headerRect.yMax > 0f)
                {
                    _entityListViewportHeight = Mathf.Max(
                        120f,
                        _menuRect.height - headerRect.yMax - 28f);
                }
            }

            _spawnEntityScroll = BeginVerticalScrollView(
                _spawnEntityScroll,
                GUILayout.Height(_entityListViewportHeight));

            if (_world == null || _localPlayer == null)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(
                    "Enter a local raid to spawn AI.");
                GUILayout.EndVertical();
            }
            else if (_spawnableAiEntries.Count == 0)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(
                    "No server-supported AI types were found.");
                GUILayout.EndVertical();
            }
            else
                DrawSpawnableAiRows();

            GUILayout.Space(18f);
            EndVerticalScrollView();
        }

        private void DrawSpawnableAiRows()
        {
            string previousGroup = null;
            for (int i = 0;
                 i < _spawnableAiEntries.Count;
                 i++)
            {
                SpawnableAiEntry entry =
                    _spawnableAiEntries[i];
                if (!SpawnEntitySearchMatches(entry))
                    continue;

                if (!string.Equals(
                        previousGroup,
                        entry.Group,
                        StringComparison.OrdinalIgnoreCase))
                {
                    previousGroup = entry.Group;
                    GUILayout.Label(
                        entry.Group,
                        _sectionTitleStyle);
                }

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(
                    entry.Name,
                    GUILayout.ExpandWidth(true));
                GUI.enabled = !_spawnEntityInProgress;
                if (GUILayout.Button(
                    _spawnEntityInProgress
                        ? "Spawning..."
                        : "Spawn at Location",
                    GUILayout.Width(160f)))
                {
                    SpawnEntityAtLocation(entry);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

#if false
        private void DrawSpawnGroupBuilder()
        {
            GUILayout.BeginHorizontal();
            DrawSpawnGroupList();
            GUILayout.Space(10f);
            DrawSelectedSpawnGroup();
            GUILayout.EndHorizontal();
        }

        private void DrawSpawnGroupList()
        {
            float listWidth = Mathf.Clamp(
                _menuRect.width * 0.24f,
                155f,
                220f);
            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.Width(listWidth));
            GUILayout.Label(
                "SAVED GROUPS",
                _sectionTitleStyle);
            GUILayout.BeginHorizontal();
            GUI.enabled =
                !_spawnEntityInProgress &&
                !_spawnGroupInProgress;
            if (GUILayout.Button("+ New Group"))
                AddSpawnGroup();
            GUI.enabled =
                !_spawnEntityInProgress &&
                !_spawnGroupInProgress &&
                GetSelectedSpawnGroup() != null;
            if (GUILayout.Button("−", GUILayout.Width(34f)))
                RemoveSelectedSpawnGroup();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            for (int i = 0;
                 i < _spawnGroups.Count;
                 i++)
            {
                bool selected =
                    i == _selectedSpawnGroup;
                if (GUILayout.Toggle(
                        selected,
                        _spawnGroups[i].Name,
                        GUI.skin.button))
                {
                    _selectedSpawnGroup = i;
                    _selectedSpawnGroupMember = -1;
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawSelectedSpawnGroup()
        {
            SpawnGroupDefinition group =
                GetSelectedSpawnGroup();
            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.ExpandWidth(true));
            if (group == null)
            {
                GUILayout.Label(
                    "Press + to create a group.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label(
                "GROUP NAME",
                _sectionTitleStyle);
            group.Name = GUILayout.TextField(
                group.Name ?? "Group",
                GUILayout.MinHeight(27f));
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            GUI.enabled =
                !_spawnEntityInProgress &&
                !_spawnGroupInProgress;
            if (GUILayout.Button(
                _spawnGroupPickerOpen
                    ? "Close AI Picker"
                    : "+ Add Entity"))
            {
                _spawnGroupPickerOpen =
                    !_spawnGroupPickerOpen;
                _expandedSpawnPickerRole = null;
            }
            GUI.enabled =
                !_spawnEntityInProgress &&
                !_spawnGroupInProgress &&
                _selectedSpawnGroupMember >= 0;
            if (GUILayout.Button(
                "− Remove Entity",
                GUILayout.Width(135f)))
                RemoveSelectedSpawnGroupMember();
            GUI.enabled =
                !_spawnEntityInProgress &&
                !_spawnGroupInProgress &&
                IsSpawnGroupValid(group);
            if (GUILayout.Button(
                _spawnGroupInProgress
                    ? "Spawning..."
                    : "Spawn Group"))
                SpawnConfiguredEntityGroup();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label(
                "GROUP HOSTILITY",
                _sectionTitleStyle);
            GUILayout.Label(
                "Checked categories are enemies. Members of this group are always protected.");
            DrawEntityTargetCategoryEditor(
                ref group.Targets);

            GUILayout.Space(8f);
            GUILayout.Label(
                "LEADER (required)",
                _sectionTitleStyle);
            SpawnGroupEntry leader =
                group.Members.FirstOrDefault(
                    member => member.IsLeader);
            DrawSpawnGroupMemberSlot(
                group,
                leader,
                leader == null
                    ? "+ Choose a leader from the AI picker"
                    : "Leader  |  " + leader.Entity.Name);

            GUILayout.Space(6f);
            GUILayout.Label(
                "FOLLOWERS",
                _sectionTitleStyle);
            for (int i = 0;
                 i < group.Members.Count;
                 i++)
            {
                if (!group.Members[i].IsLeader)
                {
                    DrawSpawnGroupMemberSlot(
                        group,
                        group.Members[i],
                        "Follower  |  " +
                        group.Members[i].Entity.Name);
                }
            }
            if (group.Members.All(member =>
                    member.IsLeader))
                GUILayout.Label(
                    "No followers added yet.");

            if (_spawnGroupPickerOpen)
            {
                GUILayout.Space(10f);
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(
                    "ADD AN ENTITY",
                    _sectionTitleStyle);
                GUILayout.Label(
                    "Choose whether to label this entry as the leader or a follower.");
                DrawSpawnableAiRows();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private static void DrawEntityTargetCategoryEditor(
            ref EntityTargetCategory targets)
        {
            GUILayout.BeginHorizontal();
            DrawEntityTargetCategoryToggle(
                ref targets,
                EntityTargetCategory.Player,
                "Player");
            DrawEntityTargetCategoryToggle(
                ref targets,
                EntityTargetCategory.Usec,
                "USEC");
            DrawEntityTargetCategoryToggle(
                ref targets,
                EntityTargetCategory.Bear,
                "BEAR");
            DrawEntityTargetCategoryToggle(
                ref targets,
                EntityTargetCategory.Scav,
                "Scavs");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawEntityTargetCategoryToggle(
                ref targets,
                EntityTargetCategory.RaiderRogue,
                "Raiders/Rogues");
            DrawEntityTargetCategoryToggle(
                ref targets,
                EntityTargetCategory.Boss,
                "Bosses");
            DrawEntityTargetCategoryToggle(
                ref targets,
                EntityTargetCategory.Follower,
                "Followers");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawEntityTargetCategoryToggle(
                ref targets,
                EntityTargetCategory.Cultist,
                "Cultists");
            DrawEntityTargetCategoryToggle(
                ref targets,
                EntityTargetCategory.Infected,
                "Infected");
            if (GUILayout.Button("All"))
                targets = EntityTargetCategory.All;
            if (GUILayout.Button("None"))
                targets = EntityTargetCategory.None;
            GUILayout.EndHorizontal();
        }

        private static void DrawEntityTargetCategoryToggle(
            ref EntityTargetCategory targets,
            EntityTargetCategory category,
            string label)
        {
            bool enabled =
                (targets & category) != 0;
            bool next = GUILayout.Toggle(
                enabled,
                label);
            if (next)
                targets |= category;
            else
                targets &= ~category;
        }

        private void DrawSpawnGroupMemberSlot(
            SpawnGroupDefinition group,
            SpawnGroupEntry member,
            string label)
        {
            int index =
                member == null
                    ? -1
                    : group.Members.IndexOf(member);
            bool selected =
                index >= 0 &&
                index == _selectedSpawnGroupMember;
            if (GUILayout.Toggle(
                    selected,
                    label,
                    GUI.skin.button))
                _selectedSpawnGroupMember = index;
        }

#endif
        private bool SpawnEntitySearchMatches(
            SpawnableAiEntry entry)
        {
            if (entry == null)
                return false;
            if (string.IsNullOrWhiteSpace(
                    _spawnEntitySearch))
                return true;

            return entry.Name.IndexOf(
                       _spawnEntitySearch,
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   entry.Group.IndexOf(
                       _spawnEntitySearch,
                       StringComparison.OrdinalIgnoreCase) >= 0;
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
            string previousKind = null;
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

                if (!string.Equals(
                        previousKind,
                        entry.Kind,
                        StringComparison.OrdinalIgnoreCase))
                {
                    previousKind = entry.Kind;
                    bool expanded;
                    if (!_liveEntityGroupsExpanded.TryGetValue(
                            entry.Kind,
                            out expanded))
                    {
                        expanded = true;
                        _liveEntityGroupsExpanded[entry.Kind] = true;
                    }

                    int matchingCount =
                        CountVisibleLiveEntities(entry.Kind);
                    GUILayout.BeginHorizontal();
                    if (DrawRoleFoldoutButton(expanded))
                    {
                        expanded = !expanded;
                        _liveEntityGroupsExpanded[entry.Kind] =
                            expanded;
                    }
                    GUILayout.Label(
                        entry.Kind + " (" + matchingCount + ")",
                        _sectionTitleStyle,
                        GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                    if (!expanded)
                        continue;
                }
                else
                {
                    bool expanded;
                    if (_liveEntityGroupsExpanded.TryGetValue(
                            entry.Kind,
                            out expanded) &&
                        !expanded)
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

                if (entry.BotOwner != null)
                {
                    if (GUILayout.Button(
                        "Kill",
                        GUILayout.Width(55f)))
                        KillEntityBot(entry.Player);

                    bool friendly =
                        _friendlyEntityAi.Contains(
                            entry.BotOwner);
                    bool friendlyToggled = GUILayout.Toggle(
                        friendly,
                        " Friendly",
                        GUILayout.Width(85f));
                    if (friendlyToggled != friendly)
                        SetEntityFriendly(
                            entry.BotOwner,
                            friendlyToggled);

                    bool aiEnabled =
                        !_disabledEntityAi.ContainsKey(
                            entry.BotOwner);
                    bool toggled = GUILayout.Toggle(
                        aiEnabled,
                        " AI enabled",
                        GUILayout.Width(100f));
                    if (toggled != aiEnabled)
                        SetEntityAiEnabled(
                            entry.BotOwner,
                            toggled);
                }
                else
                {
                    GUILayout.Space(240f);
                }
                GUILayout.EndHorizontal();
            }
        }

        private int CountVisibleLiveEntities(string kind)
        {
            int count = 0;
            for (int i = 0; i < _liveEntityEntries.Count; i++)
            {
                LiveEntityEntry entry = _liveEntityEntries[i];
                if (entry.Player != null &&
                    string.Equals(
                        entry.Kind,
                        kind,
                        StringComparison.OrdinalIgnoreCase) &&
                    EntitySearchMatches(
                        entry.Name,
                        entry.Kind))
                    count++;
            }
            return count;
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
