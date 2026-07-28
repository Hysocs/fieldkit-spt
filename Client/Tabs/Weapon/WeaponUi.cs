
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void DrawWeaponMenu()
        {
            _weaponMenuScroll = BeginVerticalScrollView(
                _weaponMenuScroll,
                GUILayout.Height(
                    Mathf.Max(300f, MenuHeight - 105f)));

            BeginCategoryColumns();
            BeginCategoryPanel("Ammunition & Fire");
            DrawOptionToggle(
                _infiniteAmmo, " Infinite Magazine Ammo");
            DrawOptionToggle(
                _quickMagazinePacking,
                " Quick magazine packing / unpacking");
            GUILayout.Label(
                "Fire rate: " +
                _fireRateMultiplier.Value.ToString("0.0") + "x");
            float fireRateExponent = GUILayout.HorizontalSlider(
                Mathf.Log10(_fireRateMultiplier.Value), -1f, 2f);
            _fireRateMultiplier.Value =
                Mathf.Pow(10f, fireRateExponent);
            DrawOptionToggle(
                _forceFullAuto,
                " Force full auto (semi-auto weapons)");
            DrawOptionToggle(
                _bulletsPassThroughObjects,
                " Bullets pass through objects");
            DrawOptionToggle(
                _bulletsPassThroughArmor,
                " Bullets pass through armor");
            DrawOptionToggle(
                _barrelExplosionOnImpact,
                " Barrel explosion on bullet impact");
            if (DrawResetGroupButton())
            {
                _infiniteAmmo.Value = false;
                _quickMagazinePacking.Value = false;
                _fireRateMultiplier.Value = 1f;
                _forceFullAuto.Value = false;
                _bulletsPassThroughObjects.Value = false;
                _bulletsPassThroughArmor.Value = false;
                _barrelExplosionOnImpact.Value = false;
            }
            EndCategoryPanel();

            BeginCategoryPanel("Reliability");
            DrawOptionToggle(_canMalfunction, " Can malfunction");
            DrawOptionToggle(_canOverheat, " Can overheat");
            DrawOptionToggle(
                _canLoseDurability, " Can lose durability");
            if (DrawResetGroupButton())
            {
                _canMalfunction.Value = true;
                _canOverheat.Value = true;
                _canLoseDurability.Value = true;
            }
            EndCategoryPanel();

            BeginCategoryPanel("Interface");
            DrawOptionToggle(
                _showWeaponDiagnostics,
                " Show weapon diagnostics panel");
            if (DrawResetGroupButton())
                _showWeaponDiagnostics.Value = false;
            EndCategoryPanel();

            NextCategoryColumn();
            BeginCategoryPanel("Handling");
            DrawOptionSlider(
                "Recoil strength", _recoilStrength, 0f, 100f, "0%");
            DrawOptionSlider(
                "Sway strength", _swayStrength, 0f, 100f, "0%");
            DrawOptionToggle(
                _ergonomicsOverride, " Override Ergonomics");
            DrawOptionSlider(
                "Ergonomics", _ergonomicsValue, 0f, 100f, "0");
            DrawOptionToggle(_noWeaponWeight, " No weapon weight");
            if (DrawResetGroupButton())
            {
                _recoilStrength.Value = 100f;
                _swayStrength.Value = 100f;
                _ergonomicsOverride.Value = false;
                _ergonomicsValue.Value = 100f;
                _noWeaponWeight.Value = false;
            }
            EndCategoryPanel();

            BeginCategoryPanel("Speed & Accuracy");
            GUILayout.Label(
                "Action speed: " +
                _weaponActionSpeed.Value.ToString("0.0") + "x");
            float actionSpeedExponent = GUILayout.HorizontalSlider(
                Mathf.Log10(_weaponActionSpeed.Value), -1f, 2f);
            _weaponActionSpeed.Value =
                Mathf.Pow(10f, actionSpeedExponent);
            GUILayout.Label(
                "Reload, rechamber, bolt cycle, and malfunction fixes");
            DrawOptionSlider(
                "ADS speed", _adsSpeedMultiplier, 0.1f, 10f, "0.0x");
            DrawOptionSlider(
                "Accuracy / spread", _accuracySpreadMultiplier,
                0f, 2f, "0.00x");
            if (DrawResetGroupButton())
            {
                _weaponActionSpeed.Value = 1f;
                _adsSpeedMultiplier.Value = 1f;
                _accuracySpreadMultiplier.Value = 1f;
            }
            EndCategoryPanel();
            EndCategoryColumns();

            GUILayout.EndScrollView();
        }

        private void DrawCurrentWeaponStatus()
        {
            Player.FirearmController controller =
                GetLocalFirearmController();
            Weapon weapon = controller == null ? null : controller.Weapon;
            MagazineItemClass magazine =
                weapon == null ? null : weapon.GetCurrentMagazine();

            if (weapon == null)
            {
                GUILayout.Label("No firearm currently equipped.");
                return;
            }

            GUILayout.Label("Weapon: " + LocalizedItemName(weapon));

            if (magazine == null)
            {
                GUILayout.Label("No detachable magazine.");
                return;
            }

            GUILayout.Label(
                "Magazine: " + magazine.Count + " / " + magazine.MaxCount);

            if (_infiniteAmmo.Value && _protectedAmmo == null)
                GUILayout.Label(
                    "Load one compatible round to seed infinite ammo.");
            else if (_infiniteAmmo.Value)
                GUILayout.Label("Current local magazine is protected.");
        }

    }
}
