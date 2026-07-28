
namespace FieldKit
{
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            _instance = this;
            LogSource = Logger;

            _enabled = Config.Bind("ESP", "Enabled", true, "Enable the ESP.");
            _showPmc = Config.Bind("ESP", "Show PMCs", true, "Show BEAR and USEC.");
            _showScav = Config.Bind("ESP", "Show Scavs", true, "Show ordinary scavs.");
            _showBoss = Config.Bind("ESP", "Show Bosses", true, "Show bosses and special scav roles.");
            _showBoxes = Config.Bind(
                "ESP", "Show Boxes", true,
                "Draw a box around each enabled ESP target.");
            _showBones = Config.Bind("ESP", "Show Bone ESP", true,
                "Draw the animated skeleton and view direction.");
            _showAimLines = Config.Bind(
                "ESP", "Show Aim Lines", true,
                "Draw each target's look-direction line.");
            _showExtractions = Config.Bind(
                "ESP Extractions", "Enabled", true,
                "Show every extraction point on the map.");
            _extractionColor = Config.Bind(
                "ESP Extractions", "Map Exit Color", "#F59E0BFF",
                "RGBA color for map exits unavailable to the local player.");
            _usableExtractionColor = Config.Bind(
                "ESP Extractions", "Usable Exit Color", "#22C55EFF",
                "RGBA color for extraction points available to the local player.");
            _visibilityCheck = Config.Bind(
                "ESP", "Visibility Check", true,
                "Dim targets whose sampled bones are blocked by geometry.");
            _cameraDebug = Config.Bind("Diagnostics", "Camera Debug Logging", false,
                "Log only accepted optic-camera and lens changes.");
            _scopeColorBrightness = Config.Bind(
                "ESP", "Scope Color Brightness", 0.5f,
                new ConfigDescription(
                    "Compensates for tinting when EFT composites the optic texture.",
                    new AcceptableValueRange<float>(0.5f, 2f)));
            _pmcVisualColor = Config.Bind(
                "Visuals", "PMC Color", "#FF4040FF",
                "RGBA color used for visible PMC ESP.");
            _scavVisualColor = Config.Bind(
                "Visuals", "Scav Color", "#FFD91AFF",
                "RGBA color used for visible Scav ESP.");
            _bossVisualColor = Config.Bind(
                "Visuals", "Boss Color", "#FF26E6FF",
                "RGBA color used for visible Boss and special Scav ESP.");
            _pmcOccludedColor = Config.Bind(
                "Visuals", "PMC Occluded Color", "#4D1313BF",
                "RGBA color used for occluded PMC ESP.");
            _scavOccludedColor = Config.Bind(
                "Visuals", "Scav Occluded Color", "#4D4108BF",
                "RGBA color used for occluded Scav ESP.");
            _bossOccludedColor = Config.Bind(
                "Visuals", "Boss Occluded Color", "#4D0745BF",
                "RGBA color used for occluded Boss and special Scav ESP.");
            _pmcChamColor = Config.Bind(
                "Chams", "PMC Visible Color", "#FF4040FF",
                "Visible PMC cham RGBA color.");
            _scavChamColor = Config.Bind(
                "Chams", "Scav Visible Color", "#FFD91AFF",
                "Visible Scav cham RGBA color.");
            _bossChamColor = Config.Bind(
                "Chams", "Boss Visible Color", "#FF26E6FF",
                "Visible Boss and special Scav cham RGBA color.");
            _pmcChamOccludedColor = Config.Bind(
                "Chams", "PMC Occluded Color", "#4D1313BF",
                "Occluded PMC cham RGBA color.");
            _scavChamOccludedColor = Config.Bind(
                "Chams", "Scav Occluded Color", "#4D4108BF",
                "Occluded Scav cham RGBA color.");
            _bossChamOccludedColor = Config.Bind(
                "Chams", "Boss Occluded Color", "#4D0745BF",
                "Occluded Boss and special Scav cham RGBA color.");
            _godMode = Config.Bind(
                "Developer Tools", "God Mode", false,
                "Prevents damage to the local player.");
            _infiniteStamina = Config.Bind(
                "Developer Tools", "Infinite Stamina", false,
                "Keeps body stamina, arm stamina, and oxygen full.");
            _noWeight = Config.Bind(
                "Developer Tools", "No Weight Penalties", false,
                "Disables local-player encumbrance penalties.");
            _chamsEnabled = Config.Bind(
                "Chams", "Enabled", false,
                "Enable character and world chams.");
            _chamsCharacters = Config.Bind(
                "Chams", "Characters", true,
                "Apply chams to the enabled character types.");
            _chamsPerLimbVisibility = Config.Bind(
                "Chams", "Per-Limb Visibility", true,
                "Split skinned character surfaces by limb and color them from the same visibility checks as skeleton ESP.");
            _chamsShowPmc = Config.Bind(
                "Chams", "Show PMCs", true, "Apply chams to PMCs.");
            _chamsShowScav = Config.Bind(
                "Chams", "Show Scavs", true, "Apply chams to Scavs.");
            _chamsShowBoss = Config.Bind(
                "Chams", "Show Bosses", true,
                "Apply chams to bosses and special Scavs.");
            _chamsMaxDistance = Config.Bind(
                "Chams", "Max Distance", 250f,
                new ConfigDescription(
                    "Maximum distance for character cham rendering.",
                    new AcceptableValueRange<float>(25f, 500f)));
            _chamsOpacity = Config.Bind(
                "Chams", "Opacity", 0.65f,
                new ConfigDescription(
                    "Overlay opacity.",
                    new AcceptableValueRange<float>(0.1f, 1f)));
            _chamsLimbWidth = Config.Bind(
                "Chams", "Limb Width", 0.045f,
                new ConfigDescription(
                    "World-space width of per-limb visibility chams.",
                    new AcceptableValueRange<float>(0.01f, 0.12f)));
            _chamsCorpses = Config.Bind(
                "Chams World", "Corpses", false,
                "Apply a flat RGBA material to corpses.");
            _chamsLoot = Config.Bind(
                "Chams World", "Loot", false,
                "Apply a flat RGBA material to loose loot.");
            _cullGrass = Config.Bind(
                "Chams World", "Cull Grass", false,
                "Disable Tarkov's GPU-instanced grass managers to remove their draw and update cost.");
            _lootRenderDistance = Config.Bind(
                "Chams World", "Loot Render Distance", 250f,
                new ConfigDescription(
                    "Loose-loot renderer distance.",
                    new AcceptableValueRange<float>(10f, 1000f)));
            _chamsCorpseColor = Config.Bind(
                "Chams World", "Corpse Color", "#A855F780",
                "Corpse RGBA color.");
            _chamsLootColor = Config.Bind(
                "Chams World", "Loot Color", "#22D3EE80",
                "Loot RGBA color.");
            _maxDistance = Config.Bind("ESP", "Max Distance", 500f,
                new ConfigDescription("Maximum drawing distance.",
                    new AcceptableValueRange<float>(25f, 1500f)));
            _lineThickness = Config.Bind("ESP", "Box Thickness", 2f,
                new ConfigDescription("Box line thickness.",
                    new AcceptableValueRange<float>(1f, 8f)));
            _boneThickness = Config.Bind("ESP", "Bone Thickness", 2f,
                new ConfigDescription("Skeleton line thickness.",
                    new AcceptableValueRange<float>(0.5f, 8f)));
            _aimLineThickness = Config.Bind(
                "ESP", "Aim Line Thickness", 2f,
                new ConfigDescription(
                    "Look-direction line thickness.",
                    new AcceptableValueRange<float>(0.5f, 8f)));
            _fontSize = Config.Bind("ESP", "Font Size", 13,
                new ConfigDescription("Label font size.",
                    new AcceptableValueRange<int>(9, 32)));
            _espFontName = Config.Bind(
                "ESP", "Font", "Segoe UI",
                "Font used by character ESP labels.");
            _textOutlineThickness = Config.Bind(
                "ESP", "Text Outline Thickness", 1f,
                new ConfigDescription(
                    "Black outline thickness around ESP labels.",
                    new AcceptableValueRange<float>(0f, 3f)));

            _menuKey = Config.Bind("Hotkeys", "Toggle Menu",
                new KeyboardShortcut(KeyCode.Insert));
            _espKey = Config.Bind("Hotkeys", "Toggle ESP",
                new KeyboardShortcut(KeyCode.Home));
            _godModeKey = Config.Bind(
                "Hotkeys", "Toggle God Mode",
                new KeyboardShortcut(KeyCode.F6));
            _staminaKey = Config.Bind(
                "Hotkeys", "Toggle Infinite Stamina",
                new KeyboardShortcut(KeyCode.F7));
            _noWeightKey = Config.Bind(
                "Hotkeys", "Toggle No Weight",
                new KeyboardShortcut(KeyCode.F8));
            _chamsKey = Config.Bind(
                "Hotkeys", "Toggle Chams",
                new KeyboardShortcut(KeyCode.F9));

            ConfigureRoleEsp();
            ConfigureGuiSettings();
            ConfigureCharacterTools();
            ConfigureWeaponTools();
            ConfigureLootTools();
            ConfigureOtherTools();
            ConfigureToggleHotkeys();
            _font = LoadFont();
            _espFontName.SettingChanged += OnEspFontSettingChanged;
            _textOutlineThickness.SettingChanged +=
                OnEspOutlineSettingChanged;
            InstallAdminPatches();
            InstallCharacterPatches();
            InstallWeaponPatches();
            InstallOtherPatches();
            _enabled.SettingChanged += OnEspEnabledSettingChanged;
            if (_enabled.Value)
                Canvas.preWillRenderCanvases += RenderEspFrame;

            PrintLoadedMessage();
        }

        private void Update()
        {
            long perfStarted = PerfTimestamp();
            HandleMenuShortcutUpdate();
            if (_guiThemeRefreshRequested)
            {
                DisposeGuiTheme();
                _guiThemeRefreshRequested = false;
            }

            UpdateToggleHotkeys();

            if (_scopeRefreshRequested ||
                Time.unscaledTime >= _nextWorldRefresh)
            {
                long worldStarted = PerfTimestamp();
                RefreshWorld();
                RecordPerf(
                    worldStarted,
                    ref _perfWorldRefreshTicks,
                    ref _perfWorldRefreshCalls,
                    ref _perfWorldRefreshMaxTicks);
                _nextWorldRefresh = Time.unscaledTime + 0.5f;
            }

            if (_world == null)
                ClearOverlay();

            UpdateAdminTools();
            UpdateCharacterTools();
            UpdateWeaponTools();
            UpdateEntityTools();
            UpdateOtherTools();
            UpdateEntityInspector();
            long chamsStarted = PerfTimestamp();
            UpdateChams();
            RecordPerf(
                chamsStarted,
                ref _perfChamsTicks,
                ref _perfChamsCalls,
                ref _perfChamsMaxTicks);

            if ((_menuOpen ||
                 (_lootEspEnabled != null && _lootEspEnabled.Value) ||
                 (_lootPriceRangeEnabled != null &&
                  _lootPriceRangeEnabled.Value)) &&
                Time.unscaledTime >= _nextLootCatalogCheck)
            {
                _nextLootCatalogCheck = Time.unscaledTime + 0.5f;
                long catalogStarted = PerfTimestamp();
                EnsureLootCatalog();
                RecordPerf(
                    catalogStarted,
                    ref _perfCatalogTicks,
                    ref _perfCatalogCalls,
                    ref _perfCatalogMaxTicks);
            }
            RecordPerf(
                perfStarted,
                ref _perfUpdateTicks,
                ref _perfUpdateCalls,
                ref _perfUpdateMaxTicks);
            PublishPerformanceTelemetry();
        }

        private void LateUpdate()
        {
            MaintainMenuCursor();
        }

        private void OnGUI()
        {
            long perfStarted = PerfTimestamp();
            MaintainMenuCursor();
            HandleMenuShortcutGuiEvent();
            if (!_menuOpen &&
                (_showWeaponDiagnostics == null ||
                 !_showWeaponDiagnostics.Value) &&
                (_showEntityInspector == null ||
                 !_showEntityInspector.Value))
            {
                RecordPerf(
                    perfStarted,
                    ref _perfGuiTicks,
                    ref _perfGuiCalls,
                    ref _perfGuiMaxTicks);
                return;
            }

            EnsureGuiTheme();
            GUISkin previousSkin = GUI.skin;
            Color previousColor = GUI.color;
            Color previousBackground = GUI.backgroundColor;
            Color previousContent = GUI.contentColor;

            try
            {
                GUI.skin = _adminSkin;
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;

                if (_menuOpen)
                {
                    UpdateMenuGeometry();
                    _menuRect = GUI.Window(
                        731904,
                        _menuRect,
                        DrawMenu,
                        "FieldKit — Developer Tools for SPT");
                    DrawAttachedTabInfoPanel();
                    DrawColorPickerPopout();
                    DrawLootQuantityPopup();
                }

                DrawWeaponDiagnosticsPanel();
                DrawEntityInspectorPanel();

                if (Event.current.rawType == EventType.MouseUp)
                    PersistGuiLayout();
            }
            finally
            {
                GUI.skin = previousSkin;
                GUI.color = previousColor;
                GUI.backgroundColor = previousBackground;
                GUI.contentColor = previousContent;
                RecordPerf(
                    perfStarted,
                    ref _perfGuiTicks,
                    ref _perfGuiCalls,
                    ref _perfGuiMaxTicks);
            }
        }

        private void OnDestroy()
        {
            _shuttingDown = true;
            PersistGuiLayout();
            if (_enabled != null)
                _enabled.SettingChanged -= OnEspEnabledSettingChanged;
            if (_guiPrimaryColor != null)
                _guiPrimaryColor.SettingChanged -=
                    OnGuiPrimaryColorChanged;
            Canvas.preWillRenderCanvases -= RenderEspFrame;
            SetMenuOpen(false);
            CloseLivingAiInventory();
            ClearWeaponActionSpeed();
            ClearAdsSpeed();
            ClearProtectedMagazine();
            ClearForcedAutomaticQueue(_equippedWeaponController);
            _forcedAutoTriggerHeld = false;
            RestoreForcedVisionOverrides();
            ReleaseFriendlyAi();
            ReleaseNoWeightOverride();
            RestoreContainerSearchOverride();
            DetachWorld();
            DetachLootCatalog();
            DisposeChams();
            DisposeGuiTheme();
            if (_entityInspectorMarker != null)
            {
                Destroy(_entityInspectorMarker);
                _entityInspectorMarker = null;
            }

            try
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
            catch { }

            _instance = null;

            if (_canvas != null)
                Destroy(_canvas.gameObject);

            _canvas = null;
            _canvasRect = null;
            _boxGraphic = null;
            _labels.Clear();
            DestroyScopeOverlays();
        }

        private void OnEspEnabledSettingChanged(
            object sender,
            EventArgs eventArgs)
        {
            Canvas.preWillRenderCanvases -= RenderEspFrame;

            if (_enabled != null && _enabled.Value)
            {
                _lastRenderFrame = -1;
                Canvas.preWillRenderCanvases += RenderEspFrame;
            }
            else
            {
                ClearOverlay();
            }
        }

        private void InstallAdminPatches()
        {
            _harmony = new Harmony("com.hysocs.fieldkit.patches");

            try
            {
                System.Reflection.MethodInfo playerDamage =
                    AccessTools.Method(
                        typeof(Player),
                        "ApplyDamageInfo",
                        new[]
                        {
                            typeof(DamageInfoStruct),
                            typeof(EBodyPart),
                            typeof(EBodyPartColliderType),
                            typeof(float)
                        });
                System.Reflection.MethodInfo healthDamage =
                    AccessTools.Method(
                        typeof(ActiveHealthController),
                        "ApplyDamage",
                        new[]
                        {
                            typeof(EBodyPart),
                            typeof(float),
                            typeof(DamageInfoStruct)
                        });

                if (playerDamage != null)
                {
                    _harmony.Patch(
                        playerDamage,
                        prefix: new HarmonyMethod(
                            AccessTools.Method(
                                typeof(Plugin),
                                nameof(BlockPlayerDamage))));
                }

                if (healthDamage != null)
                {
                    _harmony.Patch(
                        healthDamage,
                        prefix: new HarmonyMethod(
                            AccessTools.Method(
                                typeof(Plugin),
                                nameof(BlockHealthControllerDamage))));
                }

                LogSource.LogInfo(
                    "Developer-tool damage patches installed.");
            }
            catch (Exception exception)
            {
                LogSource.LogError(
                    "Failed to install God Mode patches: " + exception);
            }
        }

        private static bool BlockPlayerDamage(Player __instance)
        {
            return _instance == null ||
                   !_instance._godMode.Value ||
                   __instance == null ||
                   !__instance.IsYourPlayer;
        }

        private static bool BlockHealthControllerDamage(
            ActiveHealthController __instance,
            ref float __result)
        {
            if (_instance == null ||
                !_instance._godMode.Value ||
                _instance._localPlayer == null ||
                !ReferenceEquals(
                    __instance,
                    _instance._localPlayer.ActiveHealthController))
                return true;

            __result = 0f;
            return false;
        }

        private void UpdateAdminTools()
        {
            if (_localPlayer == null)
            {
                ReleaseNoWeightOverride();
                _wasGodMode = false;
                return;
            }

            if (_godMode.Value && !_wasGodMode)
            {
                try
                {
                    ActiveHealthController health =
                        _localPlayer.ActiveHealthController;

                    if (health != null)
                        health.RestoreFullHealth();
                }
                catch (Exception exception)
                {
                    LogSource.LogWarning(
                        "God Mode health restore failed: " +
                        exception.Message);
                }
            }

            _wasGodMode = _godMode.Value;

            if (_infiniteStamina.Value && _localPlayer.Physical != null)
            {
                try
                {
                    GClass774 stamina = _localPlayer.Physical.Stamina;
                    GClass774 hands = _localPlayer.Physical.HandsStamina;
                    GClass774 oxygen = _localPlayer.Physical.Oxygen;

                    if (stamina != null)
                        stamina.Current = stamina.TotalCapacity;

                    if (hands != null)
                        hands.Current = hands.TotalCapacity;

                    if (oxygen != null)
                        oxygen.Current = oxygen.TotalCapacity;
                }
                catch (Exception exception)
                {
                    LogSource.LogWarning(
                        "Infinite stamina update failed: " +
                        exception.Message);
                }
            }

            UpdateNoWeightOverride();
        }

        private void UpdateNoWeightOverride()
        {
            if (_weightOverridePlayer != _localPlayer)
            {
                ReleaseNoWeightOverride();
                _weightOverridePlayer = _localPlayer;
            }

            if (_localPlayer == null || _localPlayer.Physical == null)
                return;

            if (_noWeight.Value)
            {
                if (!_weightOverrideApplied)
                {
                    _previousEncumberDisabled =
                        _localPlayer.Physical.EncumberDisabled;
                    _weightOverrideApplied = true;
                }

                if (!_localPlayer.Physical.EncumberDisabled)
                    _localPlayer.Physical.EncumberDisabled = true;
            }
            else
            {
                ReleaseNoWeightOverride();
            }
        }

        private void ReleaseNoWeightOverride()
        {
            if (!_weightOverrideApplied)
            {
                _weightOverridePlayer = null;
                return;
            }

            try
            {
                if (_weightOverridePlayer != null &&
                    _weightOverridePlayer.Physical != null)
                {
                    _weightOverridePlayer.Physical.EncumberDisabled =
                        _previousEncumberDisabled;
                }
            }
            catch { }

            _weightOverrideApplied = false;
            _weightOverridePlayer = null;
        }

    }
}
