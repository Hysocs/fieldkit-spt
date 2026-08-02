
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private ConfigEntry<float> _walkSpeedMultiplier;
        private ConfigEntry<float> _sprintSpeedMultiplier;
        private ConfigEntry<float> _jumpHeightMultiplier;
        private ConfigEntry<float> _vaultSpeedMultiplier;
        private ConfigEntry<float> _accelerationMultiplier;
        private ConfigEntry<float> _stanceSpeedMultiplier;
        private ConfigEntry<bool> _noMovementInertia;
        private ConfigEntry<bool> _highSpeedFloorSafety;
        private ConfigEntry<bool> _collisionFreeMovement;
        private ConfigEntry<bool> _collisionFreeFly;
        private ConfigEntry<float> _collisionFreeFlySpeed;
        private ConfigEntry<bool> _collisionFreeKeepWorldRendered;
        private ConfigEntry<KeyboardShortcut>
            _collisionFreeMoveUpFloorKey;
        private ConfigEntry<KeyboardShortcut>
            _collisionFreeMoveDownFloorKey;
        private ConfigEntry<bool> _silentMovement;
        private ConfigEntry<bool> _noFallDamage;
        private ConfigEntry<bool> _infiniteEnergy;
        private ConfigEntry<bool> _infiniteHydration;
        private ConfigEntry<float> _energyDrainMultiplier;
        private ConfigEntry<float> _hydrationDrainMultiplier;
        private ConfigEntry<float> _healthRegeneration;
        private ConfigEntry<float> _visualHitPunchAmount;
        private ConfigEntry<bool> _fastContainerSearching;
        private Vector2 _characterMenuScroll;
        private readonly Dictionary<Collider, PlayerColliderState>
            _playerColliderStates =
                new Dictionary<Collider, PlayerColliderState>();
        private static readonly RaycastHit[]
            CollisionFreeGroundHits = new RaycastHit[64];
        private static readonly RaycastHit[]
            CollisionFreeWallHits = new RaycastHit[64];
        private float _nextPlayerColliderRefresh;
        private float _nextCollisionFreeCullingRefresh;
        private bool _collisionFreeCullingLocked;
        private bool _collisionFreeNearBlockingGeometry;
        private bool _hasCollisionFreeWallPass;
        private Vector3 _collisionFreeWallDirection;
        private float _collisionFreeWallEntry;
        private float _collisionFreeWallExit;
        private float _collisionFreeWallBodyRadius;
        private Vector3 _collisionFreeWallContactPosition;
        private bool _hasCollisionFreePreviousPosition;
        private Vector3 _collisionFreePreviousPosition;
        private Vector3 _collisionFreeTravelDirection;
        private Vector3 _collisionFreeIntendedDirection;
        private float _collisionFreeRenderRecoveryUntil;
        private bool _hasCollisionFreeFloor;
        private float _collisionFreeFloorPositionY;
        private float _collisionFreeFlyVelocity;
        private bool _collisionFreeFlyWasActive;
        private float _lastCharacterRecoveryTime;
        private float _nextCharacterRecoveryTime;
        private SkillManager _searchSkillManager;
        private ActiveHealthController _fallDamageHealthController;
        private float _savedFallSafeHeight;
        private bool _hasSavedFallSafeHeight;
        private bool _savedContainerScope;
        private bool _hasSavedContainerScope;
        private struct MovementMotionState
        {
            public float SpeedLimit;
        }
        private struct CollisionFreeMoveState
        {
            public bool Active;
            public bool CollisionFree;
            public bool Fly;
            public bool FloorSafety;
            public Vector3 StartPosition;
            public Vector3 IntendedMotion;
            public LayerMask CollisionMask;
        }
        private struct PlayerColliderState
        {
            public bool Enabled;
            public bool IsTrigger;
        }
        private static readonly Type RunMovementStateType =
            typeof(MovePlayerState);
        private static readonly FieldInfo RunPreviousDirectionField =
            RunMovementStateType == null
                ? null
                : AccessTools.Field(
                    RunMovementStateType,
                    nameof(MovePlayerState.LastNonZeroDirectionInput));
        private static readonly FieldInfo RunDirectionDelayField =
            RunMovementStateType == null
                ? null
                : AccessTools.Field(
                    RunMovementStateType,
                    nameof(MovePlayerState.smoothMovementDirectionTime));
        private static readonly FieldInfo RunBlendedDirectionField =
            RunMovementStateType == null
                ? null
                : AccessTools.Field(
                    RunMovementStateType,
                    nameof(MovePlayerState.InertiaDirection));
        private static readonly FieldInfo RunDirectionBlendVelocityField =
            RunMovementStateType == null
                ? null
                : AccessTools.Field(
                    RunMovementStateType,
                    "_velocity");
        private static readonly FieldInfo RunDiscreteDirectionDelayField =
            RunMovementStateType == null
                ? null
                : AccessTools.Field(
                    RunMovementStateType,
                    nameof(MovePlayerState.smoothMovementDirectionDuration));
        private static readonly FieldInfo RunStateDirectionField =
            RunMovementStateType == null
                ? null
                : AccessTools.Field(
                    RunMovementStateType,
                    nameof(MovePlayerState.LastDirectionInput));
        private static readonly Type JumpMovementStateType =
            typeof(JumpPlayerState);
        private static readonly FieldInfo JumpLiftVelocityField =
            JumpMovementStateType == null
                ? null
                : AccessTools.Field(
                    JumpMovementStateType,
                    "_liftForce");
        private static readonly FieldInfo JumpMovementContextField =
            JumpMovementStateType == null
                ? null
                : AccessTools.Field(
                    JumpMovementStateType.BaseType,
                    "MovementContext");

        private void ConfigureCharacterTools()
        {
            _walkSpeedMultiplier = BindCharacterRange(
                "Walk Speed Multiplier", 1f, 0.1f, 50f,
                "Local walking-speed multiplier.");
            _sprintSpeedMultiplier = BindCharacterRange(
                "Sprint Speed Multiplier", 1f, 0.1f, 50f,
                "Local sprint-speed multiplier.");
            _jumpHeightMultiplier = BindCharacterRange(
                "Jump Height Multiplier", 1f, 0.1f, 50f,
                "Local jump-height multiplier.");
            _vaultSpeedMultiplier = BindCharacterRange(
                "Vault Speed Multiplier", 1f, 0.1f, 5f,
                "Local vault-animation speed multiplier.");
            _accelerationMultiplier = BindCharacterRange(
                "Acceleration Multiplier", 1f, 0.1f, 10f,
                "Local pre-sprint and sprint acceleration multiplier.");
            _stanceSpeedMultiplier = BindCharacterRange(
                "Stance Speed Multiplier", 1f, 0.1f, 10f,
                "Local stance-transition speed multiplier.");
            _noMovementInertia = Config.Bind(
                "Character", "No Movement Inertia", false,
                "Remove local movement, pose, and sprint-braking inertia.");
            _highSpeedFloorSafety = Config.Bind(
                "Character",
                "High-Speed Raycast Floor Safety",
                false,
                "Use a downward raycast to keep accelerated normal movement from tunneling beneath walkable ground.");
            _collisionFreeMovement = Config.Bind(
                "Character",
                "Collision-Free Movement (Raycast Floor)",
                false,
                "Disable every local player collider and use a downward raycast to maintain floor height.");
            _collisionFreeFly = Config.Bind(
                "Character",
                "Collision-Free Fly",
                false,
                "Use Space and Ctrl for smooth unrestricted vertical movement.");
            _collisionFreeFlySpeed = BindCharacterRange(
                "Collision-Free Fly Speed", 6f, 1f, 50f,
                "Maximum collision-free vertical flight speed.");
            _collisionFreeKeepWorldRendered = Config.Bind(
                "Character",
                "Collision-Free Keep World Rendered",
                true,
                "Preserve Tarkov's trigger-based room and terrain visibility while collision-free movement is enabled.");
            _collisionFreeMoveUpFloorKey = Config.Bind(
                "Character",
                "Move Up Floor Key",
                new KeyboardShortcut(KeyCode.PageUp),
                "Teleport to the next walkable floor above while collision-free movement is enabled.");
            _collisionFreeMoveDownFloorKey = Config.Bind(
                "Character",
                "Move Down Floor Key",
                new KeyboardShortcut(KeyCode.PageDown),
                "Teleport to the next walkable floor below while collision-free movement is enabled.");
            _silentMovement = Config.Bind(
                "Character", "Silent Movement", false,
                "Suppress local covert movement and equipment noise values.");
            _noFallDamage = Config.Bind(
                "Character", "No Fall Damage", false,
                "Prevent local fall damage.");
            _infiniteEnergy = Config.Bind(
                "Character", "Infinite Energy", false,
                "Keep local energy at its maximum.");
            _infiniteHydration = Config.Bind(
                "Character", "Infinite Hydration", false,
                "Keep local hydration at its maximum.");
            _energyDrainMultiplier = BindCharacterRange(
                "Energy Drain Multiplier", 1f, 0f, 2f,
                "Scale negative local energy changes; zero disables drain.");
            _hydrationDrainMultiplier = BindCharacterRange(
                "Hydration Drain Multiplier", 1f, 0f, 2f,
                "Scale negative local hydration changes; zero disables drain.");
            _healthRegeneration = BindCharacterRange(
                "Health Regeneration Per Second", 0f, 0f, 25f,
                "Restore this much health per second to each living body part.");
            _visualHitPunchAmount = BindCharacterRange(
                "Visual Hit Punch Amount", 1f, 0f, 1f,
                "Scale the local camera reaction when your character is hit; zero disables visual hit punch.");
            _fastContainerSearching = Config.Bind(
                "Character", "Fast Container Searching", false,
                "Instantly search every local loot-container type and allow concurrent searches.");
        }

        private ConfigEntry<float> BindCharacterRange(
            string name,
            float defaultValue,
            float minimum,
            float maximum,
            string description)
        {
            return Config.Bind(
                "Character",
                name,
                defaultValue,
                new ConfigDescription(
                    description,
                    new AcceptableValueRange<float>(
                        minimum,
                        maximum)));
        }

    }
}
