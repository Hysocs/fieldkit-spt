
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private ConfigEntry<bool> _lootLivingAi;
        private ConfigEntry<bool> _holdLivingAiStill;
        private ConfigEntry<bool> _allAiFriendly;
        private ConfigEntry<bool> _forceThermalVision;
        private ConfigEntry<bool> _forceNightVision;
        private ConfigEntry<bool> _cleanThermalVision;
        private ConfigEntry<bool> _cleanNightVision;
        private ConfigEntry<float> _nightVisionBloomAmount;
        private ConfigEntry<ForcedVisorMode> _forcedVisorMode;
        private ConfigEntry<bool> _showPerformanceTelemetry;
        private ConfigEntry<bool> _showEntityInspector;
        private Vector2 _otherMenuScroll;
        private Player _livingLootTarget;
        private GamePlayerOwner _livingLootOwner;
        private bool _lastLootLivingAi;
        private TraderControllerClass _livingLootProxyOwner;
        private InventoryEquipment _livingLootEquipment;
        private ItemAddress _livingLootOriginalRootAddress;
        private InventoryController _livingLootOriginalController;
        private bool _livingLootOriginalControllerLocked;
        private bool _livingLootOpening;
        private bool _friendlyAiRefreshRequested;
        private bool _lastAllAiFriendly;
        private ThermalVision _configuredThermalVision;
        private bool _originalThermalOn;
        private bool _originalThermalNoise;
        private bool _originalThermalFpsStuck;
        private bool _originalThermalMotionBlur;
        private bool _originalThermalGlitch;
        private bool _originalThermalPixelation;
        private BSG.CameraEffects.NightVision
            _configuredNightVision;
        private bool _originalNightVisionOn;
        private float _originalNightVisionNoiseIntensity;
        private UltimateBloom _configuredNightVisionBloom;
        private float _originalNightVisionBloomIntensity;
        private VisorEffect _configuredVisorEffect;
        private bool _originalVisorVisible;
        private float _originalVisorIntensity;
        private VisorEffect.EMask _originalVisorMask;
        private bool _visionOverridesNeedUpdate;
        private static readonly string[] VisorModeLabels =
        {
            "Follow Equipment",
            "None",
            "Narrow",
            "Wide"
        };
        private string _doorToolStatus =
            "Unlocks locked doors without opening them.";

        private enum ForcedVisorMode
        {
            FollowEquipment,
            None,
            Narrow,
            Wide
        }

        private void ConfigureOtherTools()
        {
            _lootLivingAi = Config.Bind(
                "Other",
                "Loot Living AI",
                false,
                "Add a Search interaction to living AI and open their live inventory.");
            _holdLivingAiStill = Config.Bind(
                "Other",
                "Hold Living AI Still While Looting",
                true,
                "Continuously pause the selected AI's movement while its inventory is open.");
            _allAiFriendly = Config.Bind(
                "Other",
                "All AI Are Friendly",
                false,
                "Prevent every AI from treating players or other AI as enemies.");
            _forceThermalVision = Config.Bind(
                "Other",
                "Force Thermal Vision",
                false,
                "Force the main player camera's thermal-vision effect without requiring thermal equipment.");
            _forceNightVision = Config.Bind(
                "Other",
                "Force Night Vision",
                false,
                "Force the main player camera's night-vision effect without requiring night-vision equipment.");
            _cleanThermalVision = Config.Bind(
                "Other",
                "Clean Thermal Vision",
                false,
                "Remove thermal noise, frame stutter, motion blur, glitches, and pixelation.");
            _cleanNightVision = Config.Bind(
                "Other",
                "Clean Night Vision",
                false,
                "Remove the night-vision static/noise overlay.");
            _nightVisionBloomAmount = Config.Bind(
                "Other",
                "Night Vision Bloom Amount",
                1f,
                new ConfigDescription(
                    "Scale camera bloom while forced night vision is active.",
                    new AcceptableValueRange<float>(0f, 2f)));
            _forcedVisorMode = Config.Bind(
                "Other",
                "Forced Face Shield Overlay",
                ForcedVisorMode.FollowEquipment,
                "Follow equipped face protection, remove its overlay, or force a built-in visor mask.");
            _showPerformanceTelemetry = Config.Bind(
                "Other",
                "Show FieldKit Performance",
                false,
                "Show rolling CPU timing and cache activity for FieldKit only.");
            _showEntityInspector = Config.Bind(
                "Diagnostics",
                "Show Entity Inspector",
                false,
                "Inspect the collider under the center-screen aiming ray in a persistent draggable window.");
            _lastLootLivingAi = _lootLivingAi.Value;
            _allAiFriendly.SettingChanged +=
                OnFriendlyAiSettingChanged;
        }

        private void OnFriendlyAiSettingChanged(
            object sender,
            EventArgs args)
        {
            _friendlyAiRefreshRequested =
                _allAiFriendly.Value;
        }

    }
}
