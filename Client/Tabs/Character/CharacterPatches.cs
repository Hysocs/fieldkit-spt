
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void InstallCharacterPatches()
        {
            try
            {
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(HitCameraShaker),
                        nameof(HitCameraShaker.Hit)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleVisualHitPunch))));
                MethodInfo damageReactionMethod =
                    AccessTools.Method(
                        typeof(EffectsController),
                        "method_7",
                        new[]
                        {
                            typeof(float),
                            typeof(EBodyPart),
                            typeof(EDamageType),
                            typeof(float),
                            typeof(EFT.Ballistics.MaterialType)
                        });
                MethodInfo damageForceMethod =
                    AccessTools.Method(
                        typeof(ForceEffector),
                        nameof(ForceEffector.AddForce),
                        new[]
                        {
                            typeof(float),
                            typeof(float),
                            typeof(float)
                        });
                _harmony.Patch(
                    damageReactionMethod,
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(BeginVisualDamageReaction))),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(EndVisualDamageReaction))));
                _harmony.Patch(
                    damageForceMethod,
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleVisualDamageCameraForce))));
                PatchMovementGetter(
                    nameof(MovementContext.VaultingSpeed),
                    nameof(ScaleLocalVaultSpeed));
                PatchMovementGetter(
                    nameof(MovementContext.TransitionSpeed),
                    nameof(ScaleLocalTransitionSpeed));
                PatchMovementGetter(
                    nameof(MovementContext.Inertia),
                    nameof(RemoveLocalMovementInertia));
                PatchMovementGetter(
                    nameof(MovementContext.MoveSideInertia),
                    nameof(RemoveLocalMovementInertia));
                PatchMovementGetter(
                    nameof(MovementContext.MoveDiagonalInertia),
                    nameof(RemoveLocalMovementInertia));
                PatchMovementGetter(
                    nameof(MovementContext.PoseInertia),
                    nameof(RemoveLocalMovementInertia));
                PatchMovementGetter(
                    nameof(MovementContext.WalkInertia),
                    nameof(RemoveLocalMovementInertia));
                PatchMovementGetter(
                    nameof(MovementContext.SprintBrakeInertia),
                    nameof(RemoveLocalMovementInertia));
                PatchMovementGetter(
                    nameof(MovementContext.CovertEquipmentNoise),
                    nameof(RemoveLocalMovementNoise));
                PatchMovementGetter(
                    nameof(MovementContext.CovertMovementVolumeBySpeed),
                    nameof(RemoveLocalMovementNoise));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(SkillManager),
                        nameof(SkillManager.AttentionEliteLuckySearchValue)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(OverrideLocalLuckySearchChance))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(SkillManager),
                        nameof(SkillManager.IsSearchDouble)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(OverrideLocalConcurrentSearch))));

                _harmony.Patch(
                    AccessTools.Method(
                        typeof(MovementContext),
                        nameof(MovementContext.DirectApplyMotion)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalMovementMotion))),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(RestoreLocalMovementSpeedLimit))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(SimpleCharacterController),
                        nameof(SimpleCharacterController.Move),
                        new[] { typeof(Vector3), typeof(float) }),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(CaptureCollisionFreeMove))),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(CommitCollisionFreeMove))));
                _harmony.Patch(
                    AccessTools.PropertyGetter(
                        typeof(MovementContext),
                        nameof(MovementContext.CanWalk)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(AllowCollisionFreeWalking))));

                Type[] cullingTypes =
                {
                    typeof(DisablerCullingObjectBase),
                    typeof(DisablerCullingObject),
                    typeof(DisablerTerrainCullingObject),
                    typeof(OcclusionCullingSwitcher)
                };
                for (int i = 0; i < cullingTypes.Length; i++)
                {
                    MethodInfo setComponentsEnabled =
                        AccessTools.DeclaredMethod(
                            cullingTypes[i],
                            "SetComponentsEnabled",
                            new[] { typeof(bool) });
                    if (setComponentsEnabled != null &&
                        !setComponentsEnabled.IsAbstract &&
                        setComponentsEnabled.GetMethodBody() != null)
                    {
                        _harmony.Patch(
                            setComponentsEnabled,
                            prefix: new HarmonyMethod(
                                AccessTools.Method(
                                    typeof(Plugin),
                                    nameof(KeepCollisionFreeWorldRendered))));
                    }
                }
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(MovementContext),
                        nameof(MovementContext.PreSprintAcceleration)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalAcceleration))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(MovementContext),
                        nameof(MovementContext.SmoothPoseLevel)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalStanceDelta))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(Player),
                        nameof(Player.PlayStepSound)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(SuppressLocalMovementSound))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(Player),
                        nameof(Player.PlayGroundedSound)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(SuppressLocalMovementSound))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(MovementContext),
                        "method_1",
                        new[] { typeof(Vector3) }),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(SuppressLocalMovementNoiseEvent))));

                if (RunMovementStateType != null)
                {
                    _harmony.Patch(
                        AccessTools.Method(
                            RunMovementStateType,
                            "method_1"),
                        prefix: new HarmonyMethod(
                            AccessTools.Method(
                                typeof(Plugin),
                                nameof(ScaleLocalRunAcceleration))));
                    _harmony.Patch(
                        AccessTools.Method(
                            RunMovementStateType,
                            "method_2"),
                        prefix: new HarmonyMethod(
                            AccessTools.Method(
                                typeof(Plugin),
                                nameof(SnapLocalRunDirection))));
                }
                if (JumpMovementStateType != null &&
                    JumpLiftVelocityField != null)
                {
                    _harmony.Patch(
                        AccessTools.Method(
                            JumpMovementStateType,
                            "Enter"),
                        postfix: new HarmonyMethod(
                            AccessTools.Method(
                                typeof(Plugin),
                                nameof(ScaleLocalJumpTakeoff))));
                }
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(MovementContext),
                        nameof(MovementContext.SprintAcceleration)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalAcceleration))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(ActiveHealthController),
                        nameof(ActiveHealthController.HandleFall)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(PreventLocalFallDamage))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(ActiveHealthController),
                        nameof(ActiveHealthController.ChangeEnergy)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalEnergyDrain))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(ActiveHealthController),
                        nameof(ActiveHealthController.ChangeHydration)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalHydrationDrain))));

                LogSource.LogInfo(
                    "Local character-control patches installed.");
            }
            catch (Exception exception)
            {
                LogSource.LogError(
                    "Failed to install character-control patches: " +
                    exception);
            }
        }

        private void PatchMovementGetter(
            string propertyName,
            string patchName)
        {
            _harmony.Patch(
                AccessTools.PropertyGetter(
                    typeof(MovementContext),
                    propertyName),
                postfix: new HarmonyMethod(
                    AccessTools.Method(
                        typeof(Plugin),
                        patchName)));
        }

        private static bool IsLocalMovement(
            MovementContext context)
        {
            return _instance != null &&
                _instance._localPlayer != null &&
                ReferenceEquals(
                    _instance._localPlayer.MovementContext,
                    context);
        }

        private static void ScaleLocalVaultSpeed(
            MovementContext __instance,
            ref float __result)
        {
            if (IsLocalMovement(__instance))
                __result *= _instance._vaultSpeedMultiplier.Value;
        }

        private static void ScaleLocalStanceDelta(
            MovementContext __instance,
            ref float __0)
        {
            if (IsLocalMovement(__instance))
                __0 *= _instance._stanceSpeedMultiplier.Value;
        }

        private static void ScaleLocalTransitionSpeed(
            MovementContext __instance,
            ref float __result)
        {
            if (IsLocalMovement(__instance))
                __result *= _instance._stanceSpeedMultiplier.Value;
        }

        private static void RemoveLocalMovementInertia(
            MovementContext __instance,
            ref float __result)
        {
            if (IsLocalMovement(__instance) &&
                _instance._noMovementInertia.Value)
                __result = 0f;
        }

        private static void RemoveLocalMovementNoise(
            MovementContext __instance,
            ref float __result)
        {
            if (IsLocalMovement(__instance) &&
                _instance._silentMovement.Value)
                __result = 0f;
        }

        private static bool SuppressLocalMovementSound(
            Player __instance)
        {
            return _instance == null ||
                !_instance._silentMovement.Value ||
                !ReferenceEquals(
                    __instance,
                    _instance._localPlayer);
        }

        private static bool SuppressLocalMovementNoiseEvent(
            MovementContext __instance)
        {
            return _instance == null ||
                !_instance._silentMovement.Value ||
                !IsLocalMovement(__instance);
        }

        private static void ScaleLocalAcceleration(
            MovementContext __instance,
            ref float __0)
        {
            if (IsLocalMovement(__instance))
                __0 *= _instance._accelerationMultiplier.Value;
        }

        private static void ScaleLocalMovementMotion(
            MovementContext __instance,
            ref Vector3 __0,
            out MovementMotionState __state)
        {
            __state = new MovementMotionState
            {
                SpeedLimit = float.NaN
            };

            if (!IsLocalMovement(__instance))
                return;

            BasePhysicalClass physical =
                _instance._localPlayer.Physical;
            bool sprinting =
                physical != null && physical.Sprinting;
            float multiplier = sprinting
                ? _instance._sprintSpeedMultiplier.Value
                : _instance._walkSpeedMultiplier.Value;

            __0.x *= multiplier;
            __0.z *= multiplier;

            if (_instance._noMovementInertia.Value &&
                RunMovementStateType != null &&
                RunMovementStateType.IsInstanceOfType(
                    __instance.CurrentState))
            {
                Vector2 inputDirection =
                    __instance.MovementDirection;

                if (inputDirection.sqrMagnitude < 0.0001f)
                {
                    __0.x = 0f;
                    __0.z = 0f;
                }
                else
                {
                    Vector3 worldDirection =
                        __instance.PlayerTransform.TransformVector(
                            new Vector3(
                                inputDirection.x,
                                0f,
                                inputDirection.y));
                    worldDirection.y = 0f;

                    float horizontalMotion = Mathf.Sqrt(
                        __0.x * __0.x +
                        __0.z * __0.z);

                    if (worldDirection.sqrMagnitude > 0.0001f)
                    {
                        worldDirection.Normalize();
                        __0.x =
                            worldDirection.x * horizontalMotion;
                        __0.z =
                            worldDirection.z * horizontalMotion;
                    }
                }
            }

            ICharacterController controller =
                __instance.CharacterController;
            if (controller != null && multiplier > 1f)
            {
                __state.SpeedLimit = controller.SpeedLimit;
                controller.SpeedLimit = -1f;
            }
        }

        private static void RestoreLocalMovementSpeedLimit(
            MovementContext __instance,
            MovementMotionState __state)
        {
            ICharacterController controller =
                __instance.CharacterController;
            if (controller != null &&
                !float.IsNaN(__state.SpeedLimit))
                controller.SpeedLimit = __state.SpeedLimit;
        }

        private static void CaptureCollisionFreeMove(
            SimpleCharacterController __instance,
            Vector3 motion,
            ref LayerMask ____collisionMask,
            out CollisionFreeMoveState __state)
        {
            __state = new CollisionFreeMoveState();

            if (_instance == null ||
                _instance._localPlayer == null)
                return;

            bool collisionFree =
                _instance._collisionFreeMovement != null &&
                _instance._collisionFreeMovement.Value;
            bool floorSafety =
                _instance._highSpeedFloorSafety != null &&
                _instance._highSpeedFloorSafety.Value;
            if (!collisionFree && !floorSafety)
                return;

            MovementContext movement =
                _instance._localPlayer.MovementContext;
            if (movement == null ||
                !ReferenceEquals(
                    movement.CharacterController,
                    __instance))
                return;

            __state.Active = true;
            __state.CollisionFree = collisionFree;
            __state.Fly =
                collisionFree &&
                _instance._collisionFreeFly != null &&
                _instance._collisionFreeFly.Value;
            __state.FloorSafety = floorSafety;
            __state.StartPosition = movement.TransformPosition;
            __state.IntendedMotion = motion;
            if (__state.Fly)
            {
                __state.IntendedMotion.y =
                    _instance._collisionFreeFlyVelocity *
                    Time.deltaTime;
            }
            __state.CollisionMask = ____collisionMask;

            if (!collisionFree)
                return;

            Vector3 intendedDirection = motion;
            intendedDirection.y = 0f;
            _instance._collisionFreeIntendedDirection =
                Vector3.zero;
            if (intendedDirection.sqrMagnitude > 0.000004f)
                _instance._collisionFreeIntendedDirection =
                    intendedDirection.normalized;
            ____collisionMask = 0;
        }

        private static void AllowCollisionFreeWalking(
            MovementContext __instance,
            ref bool __result)
        {
            if (_instance != null &&
                _instance._collisionFreeMovement != null &&
                _instance._collisionFreeMovement.Value &&
                IsLocalMovement(__instance))
                __result = true;
        }

        private static void KeepCollisionFreeWorldRendered(
            ref bool __0)
        {
            if (_instance != null &&
                _instance._collisionFreeMovement != null &&
                _instance._collisionFreeMovement.Value &&
                _instance._collisionFreeKeepWorldRendered != null &&
                _instance._collisionFreeKeepWorldRendered.Value &&
                !_instance._collisionFreeNearBlockingGeometry)
                __0 = true;
        }

        private static void CommitCollisionFreeMove(
            SimpleCharacterController __instance,
            CollisionFreeMoveState __state,
            ref LayerMask ____collisionMask,
            ref Vector3 ___vector3_1)
        {
            if (!__state.Active ||
                _instance == null ||
                _instance._localPlayer == null)
                return;

            if (__state.CollisionFree)
                ____collisionMask = __state.CollisionMask;

            Vector3 unrestrictedPosition =
                __state.StartPosition + __state.IntendedMotion;
            Vector3 resolvedPosition = __state.CollisionFree
                ? unrestrictedPosition
                : ___vector3_1;

            if (!__state.Fly)
            {
                UpdateCollisionFreeFloorCache(
                    __instance,
                    __state.StartPosition,
                    unrestrictedPosition);
            }

            bool grounded = false;
            if (!__state.Fly &&
                _instance._hasCollisionFreeFloor &&
                resolvedPosition.y <=
                _instance._collisionFreeFloorPositionY)
            {
                resolvedPosition.y =
                    _instance._collisionFreeFloorPositionY;
                grounded =
                    __state.IntendedMotion.y <= 0f;
            }
            else if (!__state.CollisionFree)
            {
                return;
            }

            if (__state.Fly)
                __instance.isGrounded = true;
            else if (__state.CollisionFree || grounded)
                __instance.isGrounded = grounded;

            ___vector3_1 = resolvedPosition;

            Collider controllerCollider = __instance.GetCollider();
            if (controllerCollider != null)
                controllerCollider.transform.position = resolvedPosition;
        }

        private static void UpdateCollisionFreeFloorCache(
            SimpleCharacterController controller,
            Vector3 startPosition,
            Vector3 targetPosition)
        {
            Collider movementCollider = controller.GetCollider();
            MovementContext movement =
                _instance._localPlayer.MovementContext;
            if (movementCollider == null || movement == null)
                return;

            Transform movementTransform =
                movementCollider.transform;
            float verticalScale =
                Mathf.Abs(movementTransform.lossyScale.y);
            float feetOffset =
                (controller.center.y -
                 controller.height * 0.5f) *
                verticalScale;
            float currentFeet =
                startPosition.y + feetOffset;
            float maximumWalkableHeight =
                currentFeet +
                controller.stepOffset +
                0.1f;
            float rayHeight =
                Mathf.Max(
                    controller.height * verticalScale + 0.5f,
                    1.5f);
            Vector3 rayOrigin = new Vector3(
                targetPosition.x,
                targetPosition.y + rayHeight,
                targetPosition.z);
            float rayDistance =
                rayHeight + controller.height + 12f;

            int hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                Vector3.down,
                CollisionFreeGroundHits,
                rayDistance,
                movement.GroundMask,
                QueryTriggerInteraction.Ignore);

            float minimumFloorNormal =
                Mathf.Cos(
                    controller.slopeLimit *
                    Mathf.Deg2Rad);
            bool foundFloor = false;
            float highestFloor = float.NegativeInfinity;
            Transform localPlayerTransform =
                Original(_instance._localPlayer.Transform);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = CollisionFreeGroundHits[i];
                Collider hitCollider = hit.collider;
                if (hitCollider == null ||
                    hit.normal.y < minimumFloorNormal ||
                    hit.point.y > maximumWalkableHeight ||
                    (localPlayerTransform != null &&
                     hitCollider.transform.IsChildOf(
                         localPlayerTransform)))
                    continue;

                if (!foundFloor ||
                    hit.point.y > highestFloor)
                {
                    foundFloor = true;
                    highestFloor = hit.point.y;
                }
            }

            if (foundFloor)
            {
                _instance._collisionFreeFloorPositionY =
                    highestFloor - feetOffset;
                _instance._hasCollisionFreeFloor = true;
            }
        }

        private static void ScaleLocalRunAcceleration(
            object __instance,
            ref float __0)
        {
            MovementContext context =
                _instance == null ||
                _instance._localPlayer == null
                    ? null
                    : _instance._localPlayer.MovementContext;

            if (context == null ||
                !ReferenceEquals(
                    context.CurrentState,
                    __instance))
                return;

            if (_instance._noMovementInertia.Value)
            {
                if (RunPreviousDirectionField != null)
                    RunPreviousDirectionField.SetValue(
                        __instance,
                        Vector2.zero);

                if (RunDirectionDelayField != null)
                    RunDirectionDelayField.SetValue(
                        __instance,
                        0f);

                if (RunDiscreteDirectionDelayField != null)
                    RunDiscreteDirectionDelayField.SetValue(
                        __instance,
                        0f);
            }

            __0 *= _instance._accelerationMultiplier.Value;
        }

        private static void SnapLocalRunDirection(
            object __instance)
        {
            if (_instance == null ||
                !_instance._noMovementInertia.Value ||
                _instance._localPlayer == null ||
                !ReferenceEquals(
                    _instance._localPlayer.MovementContext.CurrentState,
                    __instance))
                return;

            if (RunStateDirectionField == null)
                return;

            Vector2 direction =
                (Vector2)RunStateDirectionField.GetValue(__instance);

            if (RunBlendedDirectionField != null)
                RunBlendedDirectionField.SetValue(
                    __instance,
                    direction);

            if (RunDirectionBlendVelocityField != null)
                RunDirectionBlendVelocityField.SetValue(
                    __instance,
                    Vector2.zero);

            if (RunDiscreteDirectionDelayField != null)
                RunDiscreteDirectionDelayField.SetValue(
                    __instance,
                    0f);
        }

        private static void ScaleLocalJumpTakeoff(
            object __instance)
        {
            if (_instance == null ||
                JumpLiftVelocityField == null ||
                JumpMovementContextField == null ||
                !IsLocalMovement(
                    JumpMovementContextField.GetValue(__instance)
                        as MovementContext))
                return;

            Vector3 liftVelocity =
                (Vector3)JumpLiftVelocityField.GetValue(__instance);
            liftVelocity.y *= Mathf.Sqrt(
                _instance._jumpHeightMultiplier.Value);
            JumpLiftVelocityField.SetValue(
                __instance,
                liftVelocity);
        }

        private static bool PreventLocalFallDamage(
            ActiveHealthController __instance,
            ref float __result)
        {
            if (_instance == null ||
                !_instance._noFallDamage.Value ||
                _instance._localPlayer == null ||
                !ReferenceEquals(
                    __instance,
                    _instance._localPlayer.ActiveHealthController))
                return true;

            __result = 0f;
            return false;
        }

        private static void ScaleLocalEnergyDrain(
            ActiveHealthController __instance,
            ref float __0)
        {
            if (__0 < 0f &&
                IsLocalHealthController(__instance))
                __0 *= _instance._infiniteEnergy.Value
                    ? 0f
                    : _instance._energyDrainMultiplier.Value;
        }

        private static void ScaleLocalHydrationDrain(
            ActiveHealthController __instance,
            ref float __0)
        {
            if (__0 < 0f &&
                IsLocalHealthController(__instance))
                __0 *= _instance._infiniteHydration.Value
                    ? 0f
                    : _instance._hydrationDrainMultiplier.Value;
        }

        private static bool IsLocalHealthController(
            ActiveHealthController controller)
        {
            return _instance != null &&
                _instance._localPlayer != null &&
                ReferenceEquals(
                    controller,
                    _instance._localPlayer.ActiveHealthController);
        }

        private static void OverrideLocalLuckySearchChance(
            SkillManager __instance,
            ref float __result)
        {
            if (IsFastSearchSkillManager(__instance))
                __result = 1f;
        }

        private static void OverrideLocalConcurrentSearch(
            SkillManager __instance,
            ref bool __result)
        {
            if (IsFastSearchSkillManager(__instance))
                __result = true;
        }

        private static bool IsFastSearchSkillManager(
            SkillManager skillManager)
        {
            return _instance != null &&
                _instance._fastContainerSearching != null &&
                _instance._fastContainerSearching.Value &&
                _instance._localPlayer != null &&
                ReferenceEquals(
                    skillManager,
                    _instance._localPlayer.Skills);
        }

    }
}
