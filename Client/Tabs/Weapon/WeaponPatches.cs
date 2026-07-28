
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void InstallWeaponPatches()
        {
            try
            {
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(ShotEffector),
                        nameof(ShotEffector.Process)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalRecoil))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(Player.FirearmController),
                        nameof(Player.FirearmController.TotalErgonomics)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(OverrideLocalErgonomics))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(Weapon),
                        nameof(Weapon.AllowMalfunction)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(OverrideLocalMalfunctionPermission))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(Weapon),
                        nameof(Weapon.AllowOverheat)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(OverrideLocalOverheatPermission))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(Weapon),
                        nameof(Weapon.FireRate)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalFireRate))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(Weapon),
                        nameof(Weapon.SingleFireRate)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalFireRate))));
                InstallForcedAutomaticFirePatches();
                InstallAutomaticFireRatePatch();
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(EFT.CameraControl.OpticRetrice),
                        nameof(EFT.CameraControl.OpticRetrice.UpdateTransform)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ValidateOpticReticleTransform))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(EftBulletClass),
                        "method_17"),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(IgnoreWorldHitForLocalBullet))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(ArmorComponent),
                        nameof(ArmorComponent.ApplyDamage)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(BypassArmorForLocalBullet))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(Weapon),
                        nameof(Weapon.GetDurabilityLossOnShot)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(PreventLocalDurabilityLoss))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(Weapon),
                        nameof(Weapon.OnShot)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(CaptureLocalDurability))),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(RestoreLocalDurability))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(Item),
                        nameof(Item.TotalWeight)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(OverrideLocalWeaponWeight))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(Player.FirearmController),
                        nameof(Player.FirearmController.ErgonomicWeight)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(OverrideLocalErgonomicWeight))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(Weapon),
                        nameof(Weapon.GetTotalCenterOfImpact)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalAccuracySpread))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(Weapon),
                        nameof(Weapon.TotalShotgunDispersion)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalAccuracySpread))));
                _harmony.Patch(
                    AccessTools.Constructor(
                        typeof(Player.PlayerInventoryController.Class1204),
                        new[]
                        {
                            typeof(InventoryController),
                            typeof(MagazineItemClass),
                            typeof(AmmoItemClass),
                            typeof(int),
                            typeof(bool),
                            typeof(float)
                        }),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(QuickLoadMagazineAmmo))));
                _harmony.Patch(
                    AccessTools.Constructor(
                        typeof(Player.PlayerInventoryController.Class1207),
                        new[]
                        {
                            typeof(InventoryController),
                            typeof(MagazineItemClass),
                            typeof(float),
                            typeof(bool)
                        }),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(QuickUnloadMagazineAmmo))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(BreathEffector),
                        nameof(BreathEffector.Process)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalBreathSway))),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(RestoreLocalBreathSway))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(WalkEffector),
                        nameof(WalkEffector.Process)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalWalkSway))),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(RestoreLocalWalkSway))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(MotionEffector),
                        nameof(MotionEffector.Process)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalMotionSway))),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(RestoreLocalMotionSway))));

                LogSource.LogInfo("Local weapon-control patches installed.");
            }
            catch (Exception exception)
            {
                LogSource.LogError(
                    "Failed to install weapon-control patches: " + exception);
            }
        }

        private static bool ValidateOpticReticleTransform(
            EFT.CameraControl.OpticRetrice __instance,
            EFT.CameraControl.OpticSight opticSight)
        {
            return __instance != null &&
                   __instance.Renderer != null &&
                   opticSight != null &&
                   opticSight.ScopeData != null &&
                   opticSight.ScopeData.Reticle != null;
        }

        private static void ScaleLocalRecoil(
            ShotEffector __instance,
            ref float str)
        {
            if (_instance == null ||
                __instance == null ||
                !ReferenceEquals(
                    __instance.FirearmController,
                    GetLocalFirearmController()))
                return;

            str *= _instance._recoilStrength.Value / 100f;
        }

        private static void QuickLoadMagazineAmmo(
            InventoryController __0,
            ref float __5)
        {
            if (IsLocalQuickMagazinePacking(__0))
                __5 = Mathf.Min(__5, 0.025f);
        }

        private static void QuickUnloadMagazineAmmo(
            InventoryController __0,
            ref float __2)
        {
            if (IsLocalQuickMagazinePacking(__0))
                __2 = Mathf.Min(__2, 0.025f);
        }

        private static bool IsLocalQuickMagazinePacking(
            InventoryController inventoryController)
        {
            return _instance != null &&
                _instance._quickMagazinePacking != null &&
                _instance._quickMagazinePacking.Value &&
                _instance._localPlayer != null &&
                ReferenceEquals(
                    inventoryController,
                    _instance._localPlayer.InventoryController);
        }

        private static void OverrideLocalErgonomics(
            Player.FirearmController __instance,
            ref float __result)
        {
            if (_instance == null ||
                !_instance._ergonomicsOverride.Value ||
                !ReferenceEquals(__instance, GetLocalFirearmController()))
                return;

            __result = _instance._ergonomicsValue.Value;
        }

        private static void OverrideLocalMalfunctionPermission(
            Weapon __instance,
            ref bool __result)
        {
            if (_instance != null &&
                !_instance._canMalfunction.Value &&
                IsLocalWeapon(__instance))
                __result = false;
        }

        private static void OverrideLocalOverheatPermission(
            Weapon __instance,
            ref bool __result)
        {
            if (_instance != null &&
                !_instance._canOverheat.Value &&
                IsLocalWeapon(__instance))
                __result = false;
        }

        private static void ScaleLocalFireRate(
            Weapon __instance,
            ref int __result)
        {
            if (_instance == null || !IsLocalWeapon(__instance))
                return;

            __result = Mathf.Clamp(
                Mathf.RoundToInt(
                    __result * _instance._fireRateMultiplier.Value),
                1,
                1000000);
        }

        private void InstallAutomaticFireRatePatch()
        {
            if (AutomaticFireOperationType == null ||
                AutomaticShotIntervalField == null)
            {
                LogSource.LogWarning(
                    "Live automatic fire-rate refresh is unavailable.");
                return;
            }

            _harmony.Patch(
                AccessTools.Method(
                    AutomaticFireOperationType,
                    "Update"),
                prefix: new HarmonyMethod(
                    AccessTools.Method(
                        typeof(Plugin),
                        nameof(RefreshAutomaticShotInterval))));
        }

        private static void RefreshAutomaticShotInterval(
            object __instance)
        {
            Player.FirearmController controller =
                GetLocalFirearmController();
            Weapon weapon =
                controller == null ? null : controller.Weapon;

            if (_instance == null ||
                controller == null ||
                weapon == null ||
                !ReferenceEquals(
                    controller.CurrentOperation,
                    __instance) ||
                AutomaticShotIntervalField == null)
                return;

            int roundsPerMinute = weapon.FireRate;

            if (roundsPerMinute > 0)
                AutomaticShotIntervalField.SetValue(
                    __instance,
                    60f / roundsPerMinute);
        }

        private void InstallForcedAutomaticFirePatches()
        {
            if (GenericFireOperationType == null ||
                GenericQueuedShotField == null)
            {
                LogSource.LogWarning(
                    "Forced automatic fire is unavailable: " +
                    "the semi-auto firing operation was not found.");
                return;
            }

            _harmony.Patch(
                AccessTools.Method(
                    typeof(Player.FirearmController),
                    nameof(Player.FirearmController.SetTriggerPressed)),
                prefix: new HarmonyMethod(
                    AccessTools.Method(
                        typeof(Plugin),
                        nameof(TrackLocalTriggerState))));
            _harmony.Patch(
                AccessTools.Method(
                    GenericFireOperationType,
                    "OnFireEvent"),
                prefix: new HarmonyMethod(
                    AccessTools.Method(
                        typeof(Plugin),
                        nameof(CaptureHeldTriggerForForcedAuto))),
                postfix: new HarmonyMethod(
                    AccessTools.Method(
                        typeof(Plugin),
                        nameof(QueueForcedAutomaticShot))));
            _harmony.Patch(
                AccessTools.Method(
                    GenericFireOperationType,
                    "SetTriggerPressed"),
                prefix: new HarmonyMethod(
                    AccessTools.Method(
                        typeof(Plugin),
                        nameof(CancelForcedAutomaticQueue))));
        }

        private static void TrackLocalTriggerState(
            Player.FirearmController __instance,
            bool __0)
        {
            if (_instance != null &&
                ReferenceEquals(
                    __instance,
                    GetLocalFirearmController()))
                _instance._forcedAutoTriggerHeld = __0;
        }

        private static void CaptureHeldTriggerForForcedAuto(
            object __instance,
            out bool __state)
        {
            __state = false;

            Player.FirearmController controller =
                GetLocalFirearmController();
            Weapon weapon =
                controller == null ? null : controller.Weapon;

            if (_instance == null ||
                !_instance._forceFullAuto.Value ||
                controller == null ||
                weapon == null ||
                !ReferenceEquals(
                    controller.CurrentOperation,
                    __instance) ||
                !_instance._forcedAutoTriggerHeld)
                return;

            Weapon.EFireMode[] modes = weapon.WeapFireType;
            __state =
                modes == null ||
                Array.IndexOf(
                    modes,
                    Weapon.EFireMode.fullauto) < 0;
        }

        private static void QueueForcedAutomaticShot(
            object __instance,
            bool __state)
        {
            if (__state && GenericQueuedShotField != null)
                GenericQueuedShotField.SetValue(__instance, true);
        }

        private static void CancelForcedAutomaticQueue(
            object __instance,
            bool __0)
        {
            if (!__0 && GenericQueuedShotField != null)
                GenericQueuedShotField.SetValue(__instance, false);
        }

        private static void IgnoreWorldHitForLocalBullet(
            EftBulletClass __instance,
            RaycastHit __0,
            ref bool __result)
        {
            if (_instance == null ||
                _instance._localPlayer == null ||
                __instance == null ||
                !string.Equals(
                    __instance.PlayerProfileID,
                    _instance._localPlayer.ProfileId,
                    StringComparison.Ordinal))
                return;

            Collider collider = __0.collider;

            if (collider == null)
                return;

            BodyPartCollider bodyPart =
                collider.GetComponentInParent<BodyPartCollider>();

            bool passThroughObjects =
                _instance._bulletsPassThroughObjects.Value;

            if (_instance._barrelExplosionOnImpact.Value &&
                (bodyPart != null || !passThroughObjects))
                SpawnBarrelImpactExplosion(__instance, __0);

            if (bodyPart == null &&
                !__result &&
                passThroughObjects)
                __result = true;
        }

        private static bool BypassArmorForLocalBullet(
            ref DamageInfoStruct __0,
            ref float __result)
        {
            if (_instance == null ||
                !_instance._bulletsPassThroughArmor.Value ||
                _instance._localPlayer == null ||
                __0.Player == null ||
                __0.Player.iPlayer == null ||
                !string.Equals(
                    __0.Player.iPlayer.ProfileId,
                    _instance._localPlayer.ProfileId,
                    StringComparison.Ordinal))
                return true;
            __result = Mathf.Max(0f, __0.Damage);
            return false;
        }

        private static void PreventLocalDurabilityLoss(
            Weapon __instance,
            ref float __result)
        {
            if (_instance != null &&
                !_instance._canLoseDurability.Value &&
                IsLocalWeapon(__instance))
                __result = 0f;
        }

        private static void CaptureLocalDurability(
            Weapon __instance,
            out DurabilityState __state)
        {
            __state = new DurabilityState();

            if (_instance == null ||
                _instance._canLoseDurability.Value ||
                !IsLocalWeapon(__instance) ||
                __instance.Repairable == null)
                return;

            __state.Applied = true;
            __state.Repairable = __instance.Repairable;
            __state.Durability = __instance.Repairable.Durability;
            __state.MaxDurability = __instance.Repairable.MaxDurability;
        }

        private static void RestoreLocalDurability(
            DurabilityState __state)
        {
            if (!__state.Applied || __state.Repairable == null)
                return;

            __state.Repairable.Durability = __state.Durability;
            __state.Repairable.MaxDurability = __state.MaxDurability;
        }

        private static void OverrideLocalWeaponWeight(
            Item __instance,
            ref float __result)
        {
            Weapon weapon = __instance as Weapon;

            if (_instance != null &&
                _instance._noWeaponWeight.Value &&
                IsLocalWeapon(weapon))
                __result = 0f;
        }

        private static void OverrideLocalErgonomicWeight(
            Player.FirearmController __instance,
            ref float __result)
        {
            if (_instance != null &&
                _instance._noWeaponWeight.Value &&
                ReferenceEquals(__instance, GetLocalFirearmController()))
                __result = 0f;
        }

        private static void ScaleLocalAccuracySpread(
            Weapon __instance,
            ref float __result)
        {
            if (_instance != null && IsLocalWeapon(__instance))
                __result *=
                    _instance._accuracySpreadMultiplier.Value;
        }

        private static bool IsLocalWeapon(Weapon weapon)
        {
            Player.FirearmController controller =
                GetLocalFirearmController();

            return controller != null &&
                weapon != null &&
                ReferenceEquals(controller.Weapon, weapon);
        }

        private static Player.FirearmController GetLocalFirearmController()
        {
            return _instance == null || _instance._localPlayer == null
                ? null
                : _instance._localPlayer.HandsController
                    as Player.FirearmController;
        }

        private static float LocalSwayFactor()
        {
            return _instance == null
                ? 1f
                : _instance._swayStrength.Value / 100f;
        }

        private static bool IsLocalProceduralAnimation(
            BreathEffector effector)
        {
            return _instance != null &&
                _instance._localPlayer != null &&
                _instance._localPlayer.ProceduralWeaponAnimation != null &&
                ReferenceEquals(
                    _instance._localPlayer.ProceduralWeaponAnimation.Breath,
                    effector);
        }

        private static bool IsLocalProceduralAnimation(
            WalkEffector effector)
        {
            return _instance != null &&
                _instance._localPlayer != null &&
                _instance._localPlayer.ProceduralWeaponAnimation != null &&
                ReferenceEquals(
                    _instance._localPlayer.ProceduralWeaponAnimation.Walk,
                    effector);
        }

        private static bool IsLocalProceduralAnimation(
            MotionEffector effector)
        {
            return _instance != null &&
                _instance._localPlayer != null &&
                _instance._localPlayer.ProceduralWeaponAnimation != null &&
                ReferenceEquals(
                    _instance._localPlayer.ProceduralWeaponAnimation.MotionReact,
                    effector);
        }

        private static void ScaleLocalBreathSway(
            BreathEffector __instance,
            out SwayScalarState __state)
        {
            __state = new SwayScalarState();

            if (!IsLocalProceduralAnimation(__instance))
                return;

            __state.Applied = true;
            __state.Intensity = __instance.Intensity;
            __instance.Intensity *= LocalSwayFactor();
        }

        private static void RestoreLocalBreathSway(
            BreathEffector __instance,
            SwayScalarState __state)
        {
            if (__state.Applied && __instance != null)
                __instance.Intensity = __state.Intensity;
        }

        private static void ScaleLocalWalkSway(
            WalkEffector __instance,
            out SwayScalarState __state)
        {
            __state = new SwayScalarState();

            if (!IsLocalProceduralAnimation(__instance))
                return;

            __state.Applied = true;
            __state.Intensity = __instance.Intensity;
            __instance.Intensity *= LocalSwayFactor();
        }

        private static void RestoreLocalWalkSway(
            WalkEffector __instance,
            SwayScalarState __state)
        {
            if (__state.Applied && __instance != null)
                __instance.Intensity = __state.Intensity;
        }

        private static void ScaleLocalMotionSway(
            MotionEffector __instance,
            out MotionSwayState __state)
        {
            __state = new MotionSwayState();

            if (!IsLocalProceduralAnimation(__instance))
                return;

            float factor = LocalSwayFactor();
            __state.Applied = true;
            __state.Intensity = __instance.Intensity;
            __state.SwayFactors = __instance.SwayFactors;
            __instance.Intensity *= factor;
            __instance.SwayFactors *= factor;
        }

        private static void RestoreLocalMotionSway(
            MotionEffector __instance,
            MotionSwayState __state)
        {
            if (!__state.Applied || __instance == null)
                return;

            __instance.Intensity = __state.Intensity;
            __instance.SwayFactors = __state.SwayFactors;
        }

    }
}
