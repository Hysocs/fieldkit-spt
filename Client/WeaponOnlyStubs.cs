#if FIELDKIT_WEAPON_ONLY
namespace FieldKit
{
    [BepInPlugin("com.hysocs.fieldkit.weapononly", "FieldKit Weapon Test", "1.2.0")]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        internal static Plugin _instance;
        internal static ManualLogSource LogSource;
        private Harmony _harmony;
        private Player _localPlayer;
        private bool _menuOpen;

        private void Awake()
        {
            _instance = this;
            LogSource = Logger;
            ConfigureWeaponTools();
            _harmony = new Harmony("com.hysocs.fieldkit.weapononly");
            InstallWeaponPatches();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
                _menuOpen = !_menuOpen;

            GameWorld world = Singleton<GameWorld>.Instance;
            _localPlayer = world == null ? null : world.MainPlayer;
            UpdateWeaponTools();
        }

        private void OnGUI()
        {
            if (_menuOpen)
            {
                GUILayout.BeginArea(new Rect(20f, 20f, 430f, 720f), GUI.skin.box);
                GUILayout.Label("FieldKit — SPT 4.1 Weapon Test (F10)");
                DrawWeaponMenu();
                GUILayout.EndArea();
            }

            DrawWeaponDiagnosticsPanel();
        }

        private static bool IsLocalProceduralAnimation(object effector)
        {
            if (_instance?._localPlayer?.ProceduralWeaponAnimation == null || effector == null)
                return false;

            return ReferenceEquals(
                AccessTools.Field(effector.GetType(), "_weaponAnimation")?.GetValue(effector),
                _instance._localPlayer.ProceduralWeaponAnimation);
        }

        private static bool IsInteger(string value) => int.TryParse(value, out _);

        private void DrawWeaponMenu()
        {
            DrawToggle(_infiniteAmmo, "Infinite magazine ammo");
            DrawToggle(_ergonomicsOverride, "Override ergonomics");
            DrawToggle(_canMalfunction, "Allow malfunctions");
            DrawToggle(_canOverheat, "Allow overheating");
            DrawToggle(_canLoseDurability, "Allow durability loss");
            DrawToggle(_forceFullAuto, "Force full auto");
            DrawToggle(_bulletsPassThroughObjects, "Bullets pass through objects");
            DrawToggle(_bulletsPassThroughArmor, "Bullets pass through armor");
            DrawToggle(_barrelExplosionOnImpact, "Explosion on bullet impact");
            DrawToggle(_noWeaponWeight, "No weapon weight");
            DrawToggle(_quickMagazinePacking, "Quick magazine packing");
            DrawToggle(_showWeaponDiagnostics, "Show diagnostics");
            DrawSlider(_recoilStrength, "Recoil %", 0f, 100f);
            DrawSlider(_swayStrength, "Sway %", 0f, 100f);
            DrawSlider(_ergonomicsValue, "Ergonomics", 0f, 200f);
            DrawSlider(_fireRateMultiplier, "Fire-rate multiplier", 0.1f, 10f);
            DrawSlider(_weaponActionSpeed, "Action-speed multiplier", 0.1f, 100f);
            DrawSlider(_adsSpeedMultiplier, "ADS-speed multiplier", 0.1f, 10f);
            DrawSlider(_accuracySpreadMultiplier, "Spread multiplier", 0f, 10f);
        }

        private static void DrawToggle(ConfigEntry<bool> entry, string label)
        {
            entry.Value = GUILayout.Toggle(entry.Value, label);
        }

        private static void DrawSlider(ConfigEntry<float> entry, string label, float min, float max)
        {
            GUILayout.Label(label + ": " + entry.Value.ToString("0.00"));
            entry.Value = GUILayout.HorizontalSlider(entry.Value, min, max);
        }
    }
}
#endif
