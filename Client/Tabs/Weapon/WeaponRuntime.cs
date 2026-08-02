
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void UpdateWeaponTools()
        {
            if (_localPlayer == null)
            {
                RefreshEquippedWeapon(null, null);
                ClearProtectedMagazine();
                ClearWeaponActionSpeed();
                ClearAdsSpeed();
                _accuracyWeapon = null;
                _weightWeapon = null;
                return;
            }

            Player.FirearmController controller =
                _localPlayer.HandsController as Player.FirearmController;
            Weapon weapon = controller == null ? null : controller.Weapon;

            RefreshEquippedWeapon(controller, weapon);
            UpdateWeaponActionSpeed(controller);
            UpdateWeaponWeight(controller, weapon);
            UpdateAdsSpeed();
            UpdateAccuracySpread(controller, weapon);

            if (weapon != null &&
                _canOverheat != null &&
                !_canOverheat.Value)
                ClearWeaponOverheat(weapon);

            UpdateForcedFireMode(controller, weapon);

            if (_infiniteAmmo == null || !_infiniteAmmo.Value)
            {
                ClearProtectedMagazine();
                return;
            }

            MagazineItemClass magazine =
                weapon == null ? null : weapon.GetCurrentMagazine();
            StackSlot cartridges =
                magazine == null ? null : magazine.Cartridges;
            AmmoItemClass ammo =
                cartridges == null ? null : cartridges.Last as AmmoItemClass;

            if (ammo == null)
            {
                ClearProtectedMagazine();
                return;
            }

            if (!ReferenceEquals(magazine, _protectedMagazine) ||
                !ReferenceEquals(ammo, _protectedAmmo))
            {
                _protectedMagazine = magazine;
                _protectedAmmo = ammo;
                _protectedAmmoCount = Mathf.Max(
                    ammo.StackObjectsCount,
                    InfiniteAmmoReserve);
            }
            else if (ammo.StackObjectsCount > _protectedAmmoCount)
            {
                _protectedAmmoCount = ammo.StackObjectsCount;
            }

            if (ammo.StackObjectsCount < _protectedAmmoCount)
                ammo.StackObjectsCount = _protectedAmmoCount;
        }

        private void RefreshEquippedWeapon(
            Player.FirearmController controller,
            Weapon weapon,
            bool force = false)
        {
            if (!force &&
                ReferenceEquals(
                    controller,
                    _equippedWeaponController) &&
                ReferenceEquals(
                    weapon,
                    _equippedLocalWeapon))
                return;

            _equippedWeaponController = controller;
            _equippedLocalWeapon = weapon;
            _forcedAutoTriggerHeld = false;
            _accuracyWeapon = null;
            _weightWeapon = null;
            ClearProtectedMagazine();

            if (controller == null || weapon == null)
                return;

            try
            {
                controller.WeaponModified();
                controller.RecalculateErgonomic();

                ProceduralWeaponAnimation animation =
                    _localPlayer == null
                        ? null
                        : _localPlayer.ProceduralWeaponAnimation;

                if (animation != null)
                    animation.UpdateWeaponVariables();
            }
            catch (Exception exception)
            {
                _equippedWeaponController = null;
                _equippedLocalWeapon = null;
                LogSource.LogWarning(
                    "Equipped weapon refresh failed: " +
                    exception.Message);
            }
        }

        private void RefreshHandsWeapon(
            Player.AbstractHandsController current)
        {
            Player.FirearmController controller =
                current as Player.FirearmController;
            Weapon weapon =
                controller == null ? null : controller.Weapon;

            RefreshEquippedWeapon(controller, weapon, true);
        }

        private void UpdateWeaponWeight(
            Player.FirearmController controller,
            Weapon weapon)
        {
            bool noWeight = _noWeaponWeight.Value;

            if (ReferenceEquals(weapon, _weightWeapon) &&
                noWeight == _appliedNoWeaponWeight)
                return;

            _weightWeapon = weapon;
            _appliedNoWeaponWeight = noWeight;

            if (controller == null || weapon == null)
                return;

            controller.RecalculateErgonomic();

            ProceduralWeaponAnimation animation =
                _localPlayer.ProceduralWeaponAnimation;

            if (animation != null)
                animation.UpdateWeaponVariables();
        }

        private void UpdateAdsSpeed()
        {
            ProceduralWeaponAnimation animation =
                _localPlayer == null
                    ? null
                    : _localPlayer.ProceduralWeaponAnimation;

            if (animation == null || AimingSpeedField == null)
            {
                ClearAdsSpeed();
                return;
            }

            float current =
                (float)AimingSpeedField.GetValue(animation);

            if (!ReferenceEquals(animation, _adsAnimation))
            {
                ClearAdsSpeed();
                _adsAnimation = animation;
                _baseAdsSpeed = current;
            }
            else if (!Mathf.Approximately(
                current, _appliedAdsSpeed))
            {
                _baseAdsSpeed = current;
            }

            _appliedAdsSpeed =
                _baseAdsSpeed * _adsSpeedMultiplier.Value;
            AimingSpeedField.SetValue(
                animation,
                _appliedAdsSpeed);
        }

        private void ClearAdsSpeed()
        {
            if (_adsAnimation != null &&
                AimingSpeedField != null)
            {
                try
                {
                    AimingSpeedField.SetValue(
                        _adsAnimation,
                        _baseAdsSpeed);
                }
                catch { }
            }

            _adsAnimation = null;
            _baseAdsSpeed = 0f;
            _appliedAdsSpeed = 0f;
        }

        private void UpdateAccuracySpread(
            Player.FirearmController controller,
            Weapon weapon)
        {
            float multiplier = _accuracySpreadMultiplier.Value;

            if (ReferenceEquals(weapon, _accuracyWeapon) &&
                Mathf.Approximately(
                    multiplier, _appliedAccuracyMultiplier))
                return;

            _accuracyWeapon = weapon;
            _appliedAccuracyMultiplier = multiplier;

            if (controller != null && weapon != null)
                controller.WeaponModified();
        }

        private void UpdateWeaponActionSpeed(
            Player.FirearmController controller)
        {
            FirearmsAnimator animator =
                controller == null ? null : controller.FirearmsAnimator;

            if (animator == null || animator.Animator == null)
            {
                ClearWeaponActionSpeed();
                return;
            }

            float currentReload =
                AnimationControllerParametersTable.GetFloatSpeedReload(
                    animator.Animator);
            float currentFix =
                AnimationControllerParametersTable.GetFloatSpeedFix(
                    animator.Animator);

            if (!ReferenceEquals(animator, _actionSpeedAnimator))
            {
                ClearWeaponActionSpeed();
                _actionSpeedAnimator = animator;
                _baseReloadSpeed = currentReload;
                _baseFixSpeed = currentFix;
            }
            else
            {
                if (!Mathf.Approximately(
                    currentReload, _appliedReloadSpeed))
                    _baseReloadSpeed = currentReload;

                if (!Mathf.Approximately(
                    currentFix, _appliedFixSpeed))
                    _baseFixSpeed = currentFix;
            }

            float factor = _weaponActionSpeed.Value;
            _appliedReloadSpeed = _baseReloadSpeed * factor;
            _appliedFixSpeed = _baseFixSpeed * factor;

            AnimationControllerParametersTable.SetSpeedReload(
                animator.Animator,
                _appliedReloadSpeed);
            AnimationControllerParametersTable.SetSpeedFix(
                animator.Animator,
                _appliedFixSpeed);
        }

        private void ClearWeaponActionSpeed()
        {
            if (_actionSpeedAnimator != null &&
                _actionSpeedAnimator.Animator != null)
            {
                try
                {
                    AnimationControllerParametersTable.SetSpeedReload(
                        _actionSpeedAnimator.Animator,
                        _baseReloadSpeed);
                    AnimationControllerParametersTable.SetSpeedFix(
                        _actionSpeedAnimator.Animator,
                        _baseFixSpeed);
                }
                catch { }
            }

            _actionSpeedAnimator = null;
            _baseReloadSpeed = 0f;
            _baseFixSpeed = 0f;
            _appliedReloadSpeed = 0f;
            _appliedFixSpeed = 0f;
        }

        private void UpdateForcedFireMode(
            Player.FirearmController controller,
            Weapon weapon)
        {
            if (_forceFullAuto == null ||
                !_forceFullAuto.Value)
            {
                ClearForcedAutomaticQueue(controller);
                _forcedAutoTriggerHeld = false;
            }

            if (controller == null ||
                weapon == null ||
                weapon.FireMode == null)
                return;

            Weapon.EFireMode[] nativeModes = weapon.WeapFireType;
            bool hasNativeFullAuto =
                nativeModes != null &&
                Array.IndexOf(
                    nativeModes,
                    Weapon.EFireMode.fullauto) >= 0;

            if (hasNativeFullAuto)
                return;

            if (weapon.SelectedFireMode ==
                    Weapon.EFireMode.fullauto &&
                nativeModes != null &&
                nativeModes.Length > 0)
            {
                Weapon.EFireMode restoredMode =
                    Array.IndexOf(
                        nativeModes,
                        Weapon.EFireMode.semiauto) >= 0
                        ? Weapon.EFireMode.semiauto
                        : Array.IndexOf(
                            nativeModes,
                            Weapon.EFireMode.single) >= 0
                            ? Weapon.EFireMode.single
                            : nativeModes[0];

                controller.ChangeFireMode(restoredMode);
            }
        }

        private static void ClearForcedAutomaticQueue(
            Player.FirearmController controller)
        {
            if (controller == null ||
                GenericQueuedShotField == null)
                return;

            object operation = controller.CurrentOperation;
            if (operation == null ||
                GenericFireOperationType == null ||
                !GenericFireOperationType.IsInstanceOfType(operation))
                return;

            try
            {
                GenericQueuedShotField.SetValue(operation, false);
            }
            catch { }
        }

        private static void ClearWeaponOverheat(Weapon weapon)
        {
            Weapon.MalfunctionState state =
                weapon == null ? null : weapon.MalfState;

            if (state == null)
                return;

            state.LastShotOverheat = 0f;
            state.OverheatBarrelMoveMult = 0f;
            state.OverheatBarrelMoveDir = Vector2.zero;
            state.OverheatFirerateMult = 0f;
            state.OverheatFirerateMultInited = false;
            state.SlideOnOverheatReached = false;
        }

        private void ClearProtectedMagazine()
        {
            _protectedMagazine = null;
            _protectedAmmo = null;
            _protectedAmmoCount = 0;
        }

    }
}
