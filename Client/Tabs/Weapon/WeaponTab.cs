
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private const int InfiniteAmmoReserve = 4;
        private ConfigEntry<bool> _infiniteAmmo;
        private ConfigEntry<float> _recoilStrength;
        private ConfigEntry<float> _swayStrength;
        private ConfigEntry<bool> _ergonomicsOverride;
        private ConfigEntry<float> _ergonomicsValue;
        private ConfigEntry<bool> _canMalfunction;
        private ConfigEntry<bool> _canOverheat;
        private ConfigEntry<bool> _canLoseDurability;
        private ConfigEntry<float> _fireRateMultiplier;
        private ConfigEntry<bool> _forceFullAuto;
        private ConfigEntry<bool> _bulletsPassThroughObjects;
        private ConfigEntry<bool> _bulletsPassThroughArmor;
        private ConfigEntry<bool> _barrelExplosionOnImpact;
        private ConfigEntry<float> _weaponActionSpeed;
        private ConfigEntry<bool> _noWeaponWeight;
        private ConfigEntry<float> _adsSpeedMultiplier;
        private ConfigEntry<float> _accuracySpreadMultiplier;
        private ConfigEntry<bool> _quickMagazinePacking;
        private ConfigEntry<bool> _showWeaponDiagnostics;
        private Player.FirearmController _equippedWeaponController;
        private Weapon _equippedLocalWeapon;
        private bool _forcedAutoTriggerHeld;
        private MagazineItemClass _protectedMagazine;
        private AmmoItemClass _protectedAmmo;
        private int _protectedAmmoCount;
        private FirearmsAnimator _actionSpeedAnimator;
        private float _baseReloadSpeed;
        private float _baseFixSpeed;
        private float _appliedReloadSpeed;
        private float _appliedFixSpeed;
        private ProceduralWeaponAnimation _adsAnimation;
        private float _baseAdsSpeed;
        private float _appliedAdsSpeed;
        private Weapon _accuracyWeapon;
        private float _appliedAccuracyMultiplier = -1f;
        private Weapon _weightWeapon;
        private bool _appliedNoWeaponWeight;
        private Vector2 _weaponMenuScroll;
        private Rect _weaponDiagnosticsRect =
            new Rect(670f, 30f, 350f, 500f);
        private static readonly FieldInfo AimingSpeedField =
            AccessTools.Field(
                typeof(ProceduralWeaponAnimation),
                "_aimingSpeed");
        private static readonly Type GenericFireOperationType =
            AccessTools.Inner(
                typeof(Player.FirearmController),
                "GenericFireOperationClass");
        private static readonly FieldInfo GenericQueuedShotField =
            GenericFireOperationType == null
                ? null
                : AccessTools.Field(
                    GenericFireOperationType,
                    "Bool_3");
        private static readonly Type AutomaticFireOperationType =
            AccessTools.Inner(
                typeof(Player.FirearmController),
                "GClass2029");
        private static readonly FieldInfo AutomaticShotIntervalField =
            AutomaticFireOperationType == null
                ? null
                : AccessTools.Field(
                    AutomaticFireOperationType,
                    "Float_5");

        private void ConfigureWeaponTools()
        {
            _infiniteAmmo = Config.Bind(
                "Weapon", "Infinite Magazine Ammo", false,
                "Replenishes the current local weapon magazine while firing.");
            _recoilStrength = Config.Bind(
                "Weapon", "Recoil Strength Percent", 100f,
                new ConfigDescription(
                    "Local weapon recoil strength; 100 is normal.",
                    new AcceptableValueRange<float>(0f, 100f)));
            _swayStrength = Config.Bind(
                "Weapon", "Sway Strength Percent", 100f,
                new ConfigDescription(
                    "Local breath, movement, and walking sway; 100 is normal.",
                    new AcceptableValueRange<float>(0f, 100f)));
            _ergonomicsOverride = Config.Bind(
                "Weapon", "Override Ergonomics", false,
                "Force the local equipped weapon's effective ergonomics.");
            _ergonomicsValue = Config.Bind(
                "Weapon", "Ergonomics Value", 100f,
                new ConfigDescription(
                    "Forced effective ergonomics when its override is enabled.",
                    new AcceptableValueRange<float>(0f, 100f)));
            _canMalfunction = Config.Bind(
                "Weapon", "Can Malfunction", true,
                "Allow the equipped local weapon to malfunction.");
            _canOverheat = Config.Bind(
                "Weapon", "Can Overheat", true,
                "Allow the equipped local weapon to accumulate heat.");
            _canLoseDurability = Config.Bind(
                "Weapon", "Can Lose Durability", true,
                "Allow the equipped local weapon to lose durability.");
            _fireRateMultiplier = Config.Bind(
                "Weapon", "Fire Rate Multiplier", 1f,
                new ConfigDescription(
                    "Local equipped weapon fire-rate multiplier; 1.0 is normal.",
                    new AcceptableValueRange<float>(0.1f, 100f)));
            _forceFullAuto = Config.Bind(
                "Weapon", "Force Full Auto", false,
                "Force semi-automatic local weapons into full-auto mode.");
            _bulletsPassThroughObjects = Config.Bind(
                "Weapon", "Bullets Pass Through Objects", false,
                "Locally fired bullets ignore world objects but still hit character body and armor colliders.");
            _bulletsPassThroughArmor = Config.Bind(
                "Weapon", "Bullets Pass Through Armor", false,
                "Locally fired bullets bypass armor mitigation and durability damage.");
            _barrelExplosionOnImpact = Config.Bind(
                "Weapon", "Barrel Explosion On Bullet Impact", false,
                "Spawn a barrel-style explosion on body hits and, when object pass-through is disabled, world-object hits.");
            _weaponActionSpeed = Config.Bind(
                "Weapon", "Weapon Action Speed", 1f,
                new ConfigDescription(
                    "Reload, rechamber, bolt-cycle, and malfunction-fix speed; 1.0 is normal.",
                    new AcceptableValueRange<float>(0.1f, 100f)));
            _noWeaponWeight = Config.Bind(
                "Weapon", "No Weapon Weight", false,
                "Remove equipped local weapon weight from weapon handling.");
            _adsSpeedMultiplier = Config.Bind(
                "Weapon", "ADS Speed Multiplier", 1f,
                new ConfigDescription(
                    "Local aim-down-sights transition speed; 1.0 is normal.",
                    new AcceptableValueRange<float>(0.1f, 10f)));
            _accuracySpreadMultiplier = Config.Bind(
                "Weapon", "Accuracy Spread Multiplier", 1f,
                new ConfigDescription(
                    "Local center-of-impact and shotgun spread; 0 is perfect and 1.0 is normal.",
                    new AcceptableValueRange<float>(0f, 2f)));
            _quickMagazinePacking = Config.Bind(
                "Weapon", "Quick Magazine Packing", false,
                "Load and unload loose ammunition at the safe minimum action time.");
            _showWeaponDiagnostics = Config.Bind(
                "Weapon", "Show Diagnostics Panel", false,
                "Show the draggable live weapon diagnostics window.");
        }

    }
}
