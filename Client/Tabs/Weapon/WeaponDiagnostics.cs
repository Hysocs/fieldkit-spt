
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void DrawWeaponDiagnosticsPanel()
        {
            if (_showWeaponDiagnostics == null ||
                !_showWeaponDiagnostics.Value)
                return;

            _weaponDiagnosticsRect = GUI.Window(
                731905,
                _weaponDiagnosticsRect,
                DrawWeaponDiagnosticsWindow,
                "Weapon Diagnostics");
        }

        private void DrawWeaponDiagnosticsWindow(int windowId)
        {
            Player.FirearmController controller =
                GetLocalFirearmController();
            Weapon weapon = controller == null ? null : controller.Weapon;

            if (weapon == null)
            {
                GUILayout.Label("No firearm currently equipped.");
                GUI.DragWindow(new Rect(
                    0f, 0f, _weaponDiagnosticsRect.width, 24f));
                return;
            }

            MagazineItemClass magazine = weapon.GetCurrentMagazine();
            AmmoItemClass ammo = magazine == null ||
                magazine.Cartridges == null
                    ? null
                    : magazine.Cartridges.Last as AmmoItemClass;
            Weapon.MalfunctionState state = weapon.MalfState;
            WeaponTemplate template = weapon.Template;
            float currentDurability = weapon.Repairable == null
                ? 0f
                : weapon.Repairable.Durability;
            float maximumDurability = weapon.Repairable == null
                ? 0f
                : weapon.Repairable.MaxDurability;

            GUILayout.Label(LocalizedItemName(weapon));
            GUILayout.Label("Template: " + weapon.TemplateId);
            GUILayout.Space(4f);
            GUILayout.Label("Fire mode: " + weapon.SelectedFireMode);
            GUILayout.Label(
                "Fire rate: " + weapon.FireRate +
                " RPM  (" + _fireRateMultiplier.Value.ToString("0.0") + "×)");
            GUILayout.Label(
                "Magazine: " +
                (magazine == null
                    ? "none"
                    : magazine.Count + " / " + magazine.MaxCount));
            GUILayout.Label("Chambered: " + weapon.ChamberAmmoCount);
            GUILayout.Label(
                "Ammo: " +
                (ammo == null ? "none" : LocalizedItemName(ammo)));
            GUILayout.Space(4f);
            GUILayout.Label(
                "Recoil: " + weapon.RecoilTotal.ToString("0.0") +
                "  (" + _recoilStrength.Value.ToString("0") + "%)");
            GUILayout.Label(
                "Ergonomics: " +
                controller.TotalErgonomics.ToString("0.0"));
            GUILayout.Label(
                "Weight: " + weapon.TotalWeight.ToString("0.00") + " kg");
            GUILayout.Label(
                "ADS speed: " + _appliedAdsSpeed.ToString("0.00") +
                "  (" + _adsSpeedMultiplier.Value.ToString("0.0") + "×)");
            GUILayout.Label(
                "Center of impact: " +
                weapon.GetTotalCenterOfImpact(false).ToString("0.000"));
            GUILayout.Label(
                "Shotgun spread: " +
                weapon.TotalShotgunDispersion.ToString("0.000"));
            GUILayout.Space(4f);
            GUILayout.Label(
                "Durability: " +
                currentDurability.ToString("0.0") + " / " +
                maximumDurability.ToString("0.0"));
            GUILayout.Label(
                "Heat: " +
                (state == null
                    ? "n/a"
                    : state.LastShotOverheat.ToString("0.0")));
            GUILayout.Label(
                "Malfunction: " +
                (state == null ? "n/a" : state.State.ToString()));
            GUILayout.Label(
                "Base fire rate: " +
                (template == null ? 0 : template.bFirerate) + " RPM");
            GUILayout.Space(6f);
            GUILayout.Label(
                _menuOpen
                    ? "Drag this window by its title bar."
                    : "Open the main menu to free the cursor and drag.");

            GUI.DragWindow(new Rect(
                0f, 0f, _weaponDiagnosticsRect.width, 24f));
        }

        private static string LocalizedItemName(Item item)
        {
            if (item == null)
                return "Unknown";

            try
            {
                string localized = EFT.LocalizationExtensions.LocalizedName(item);

                if (!string.IsNullOrWhiteSpace(localized) &&
                    !IsInteger(localized))
                    return localized;
            }
            catch { }

            return item.TemplateId;
        }

        private struct SwayScalarState
        {
            public bool Applied;
            public float Intensity;
        }

        private struct MotionSwayState
        {
            public bool Applied;
            public float Intensity;
            public Vector3 SwayFactors;
        }

        private struct DurabilityState
        {
            public bool Applied;
            public RepairableComponent Repairable;
            public float Durability;
            public float MaxDurability;
        }
    }
}
