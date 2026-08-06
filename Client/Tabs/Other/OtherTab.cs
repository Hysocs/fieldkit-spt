
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private ConfigEntry<bool> _lootLivingAi;
        private ConfigEntry<bool> _holdLivingAiStill;
        private ConfigEntry<bool> _forceThermalVision;
        private ConfigEntry<bool> _forceNightVision;
        private ConfigEntry<bool> _cleanThermalVision;
        private ConfigEntry<bool> _cleanNightVision;
        private ConfigEntry<float> _nightVisionBloomAmount;
        private ConfigEntry<ForcedVisorMode> _forcedVisorMode;
        private ConfigEntry<bool> _showPerformanceTelemetry;
        private ConfigEntry<bool> _showEntityInspector;
        private ConfigEntry<bool> _legacyEspProjection;
        private ConfigEntry<string> _menuFontName;
        private ConfigEntry<float> _menuUiScale;
        private float _pendingMenuUiScale;
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
        private BotOwner _livingLootPausedBot;
        private EBotState _livingLootPausedBotState;
        private readonly List<KeyValuePair<Animator, float>>
            _livingLootPausedAnimators =
                new List<KeyValuePair<Animator, float>>();
        private static readonly MethodInfo SetBotStateMethod =
            AccessTools.PropertySetter(
                typeof(BotOwner),
                nameof(BotOwner.BotState));
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
        private bool _weatherOverrideEnabled;
        private float _weatherCloud = -1f;
        private float _weatherFog = 0.0013f;
        private float _weatherRain = 1f;
        private float _weatherRainIntensity;
        private bool _weatherCustomRainIntensity;
        private float _weatherWindSpeed;
        private float _weatherWindGustiness;
        private float _weatherTemperature = 20f;
        private float _weatherPressure = 770f;
        private bool _weatherTimeEnabled;
        private float _weatherHourOfDay = 12f;
        private float _weatherTimeScale = 1f;
        private bool _weatherHasServerState;
        private bool _serverWeatherEnabled;
        private bool _serverWeatherTimeEnabled;
        private float _serverWeatherCloud;
        private float _serverWeatherFog;
        private float _serverWeatherRain;
        private float _serverWeatherRainIntensity;
        private bool _serverWeatherCustomRainIntensity;
        private float _serverWeatherWindSpeed;
        private float _serverWeatherWindGustiness;
        private float _serverWeatherTemperature;
        private float _serverWeatherPressure;
        private float _serverWeatherHourOfDay;
        private float _serverWeatherTimeScale;
        private long _weatherRevision;
        private bool _weatherRequestInFlight;
        private bool _weatherInitialRequestStarted;
        private Vector2 _weatherMenuScroll;
        private string _weatherServerStatus =
            "Waiting for FieldKit server weather state.";
        private bool _weatherLiveTimeOverrideApplied;
        private float _weatherOriginalLiveTimeScale = 1f;
        private DateTime _weatherOriginalLiveGameTime;
        private DateTime _weatherOriginalLiveRealTime;

        private enum ForcedVisorMode
        {
            FollowEquipment,
            None,
            Narrow,
            Wide
        }

        private void ConfigureOtherTools()
        {
            _menuFontName = Config.Bind(
                "GUI Appearance",
                "Menu Font",
                "Segoe UI",
                "Font used by the FieldKit menu.");
            _menuUiScale = Config.Bind(
                "GUI Appearance",
                "UI Scale",
                1f,
                new ConfigDescription(
                    "Scale the complete FieldKit menu for high-resolution displays.",
                    new AcceptableValueRange<float>(0.5f, 50f)));
            _pendingMenuUiScale = _menuUiScale.Value;
            _menuFontName.SettingChanged +=
                OnMenuFontSettingChanged;
            _lootLivingAi = Config.Bind(
                "Other",
                "Loot Living AI",
                false,
                "Add a Search interaction to living AI and open their live inventory.");
            _holdLivingAiStill = Config.Bind(
                "Other",
                "Hold Living AI Still While Looting",
                true,
                "Temporarily pause the selected AI's brain and animation while its inventory is open.");
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
            _legacyEspProjection = Config.Bind(
                "Other",
                "Legacy ESP Projection",
                false,
                "Use the original screen-size-based ESP projection.");
            _showEntityInspector = Config.Bind(
                "Diagnostics",
                "Show Entity Inspector",
                false,
                "Inspect the collider under the center-screen aiming ray in a persistent draggable window.");
            _lastLootLivingAi = _lootLivingAi.Value;
        }

    }
}
