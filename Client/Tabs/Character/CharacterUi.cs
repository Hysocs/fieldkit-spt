
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void DrawCharacterMenu()
        {
            _characterMenuScroll = BeginVerticalScrollView(
                _characterMenuScroll,
                GUILayout.Height(
                    Mathf.Max(300f, MenuHeight - 105f)));

            BeginCategoryColumns();
            BeginCategoryPanel("Survivability");
            DrawOptionToggle(_godMode, " God Mode");
            DrawOptionToggle(_noFallDamage, " No Fall Damage");
            GUILayout.Label(
                "Health regeneration: " +
                _healthRegeneration.Value.ToString("0.0") +
                " HP/s per body part");
            _healthRegeneration.Value = GUILayout.HorizontalSlider(
                _healthRegeneration.Value, 0f, 25f);
            if (DrawResetGroupButton())
            {
                _godMode.Value = false;
                _noFallDamage.Value = false;
                _healthRegeneration.Value = 0f;
            }
            EndCategoryPanel();

            BeginCategoryPanel("Visual Reactions");
            GUILayout.Label(
                "Hit punch: " +
                (_visualHitPunchAmount.Value * 100f)
                .ToString("0") +
                "%");
            _visualHitPunchAmount.Value =
                GUILayout.HorizontalSlider(
                    _visualHitPunchAmount.Value,
                    0f,
                    1f);
            if (DrawResetGroupButton())
                _visualHitPunchAmount.Value = 1f;
            EndCategoryPanel();

            BeginCategoryPanel("Movement Speed");
            DrawCharacterSlider(
                "Walk speed",
                _walkSpeedMultiplier,
                0.1f,
                50f,
                true);
            DrawCharacterSlider(
                "Sprint speed",
                _sprintSpeedMultiplier,
                0.1f,
                50f,
                true);
            DrawCharacterSlider(
                "Jump height",
                _jumpHeightMultiplier,
                0.1f,
                50f,
                true);
            DrawCharacterSlider(
                "Vault speed",
                _vaultSpeedMultiplier,
                0.1f,
                5f);
            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            DrawOptionToggle(
                _highSpeedFloorSafety, " Raycast Floor Safety");
            GUILayout.EndHorizontal();
            if (DrawResetGroupButton())
            {
                _walkSpeedMultiplier.Value = 1f;
                _sprintSpeedMultiplier.Value = 1f;
                _jumpHeightMultiplier.Value = 1f;
                _vaultSpeedMultiplier.Value = 1f;
                _highSpeedFloorSafety.Value = false;
            }
            EndCategoryPanel();

            BeginCategoryPanel("Recovery Actions", false);

            if (GUILayout.Button("Fully restore all body parts"))
                RestoreLocalCharacter();

            if (GUILayout.Button("Remove negative effects"))
                RemoveLocalNegativeEffects();

            EndCategoryPanel();

            NextCategoryColumn();
            BeginCategoryPanel("Stamina & Needs");
            DrawOptionToggle(_infiniteStamina, " Infinite Stamina");
            DrawOptionToggle(_infiniteEnergy, " Infinite Energy");
            DrawOptionToggle(_infiniteHydration, " Infinite Hydration");
            GUILayout.Label(
                "Energy drain: " +
                _energyDrainMultiplier.Value.ToString("0.00") + "x");
            _energyDrainMultiplier.Value = GUILayout.HorizontalSlider(
                _energyDrainMultiplier.Value, 0f, 2f);
            GUILayout.Label(
                "Hydration drain: " +
                _hydrationDrainMultiplier.Value.ToString("0.00") + "x");
            _hydrationDrainMultiplier.Value = GUILayout.HorizontalSlider(
                _hydrationDrainMultiplier.Value, 0f, 2f);
            if (DrawResetGroupButton())
            {
                _infiniteStamina.Value = false;
                _infiniteEnergy.Value = false;
                _infiniteHydration.Value = false;
                _energyDrainMultiplier.Value = 1f;
                _hydrationDrainMultiplier.Value = 1f;
            }
            EndCategoryPanel();

            BeginCategoryPanel("Movement Feel");
            DrawCharacterSlider(
                "Acceleration",
                _accelerationMultiplier,
                0.1f,
                10f);
            DrawCharacterSlider(
                "Stance transitions",
                _stanceSpeedMultiplier,
                0.1f,
                10f);
            DrawOptionToggle(_noMovementInertia, " No Movement Inertia");
            DrawOptionToggle(
                _collisionFreeMovement, " Collision-Free Movement");
            if (_collisionFreeMovement.Value)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(18f);
                DrawOptionToggle(_collisionFreeFly, " Fly Mode");
                GUILayout.EndHorizontal();
                if (_collisionFreeFly.Value)
                {
                    DrawCharacterSlider(
                        "Fly vertical speed",
                        _collisionFreeFlySpeed,
                        1f,
                        50f);
                    GUILayout.Label("Fly controls: Space up, Ctrl down");
                }
                GUILayout.BeginHorizontal();
                GUILayout.Space(18f);
                DrawOptionToggle(
                    _collisionFreeKeepWorldRendered,
                    " Keep World Rendered");
                GUILayout.EndHorizontal();
                if (_collisionFreeKeepWorldRendered.Value)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(36f);
                    GUILayout.Label(
                        "Nearby geometry auto-hides at contact");
                    GUILayout.EndHorizontal();
                }
                GUILayout.Label(
                    "Floor travel: up [" +
                    _collisionFreeMoveUpFloorKey.Value +
                    "], down [" +
                    _collisionFreeMoveDownFloorKey.Value +
                    "]");
            }
            DrawOptionToggle(_silentMovement, " Silent Movement");
            DrawOptionToggle(
                _noWeight, " No Weight Penalties");
            if (DrawResetGroupButton())
            {
                _accelerationMultiplier.Value = 1f;
                _stanceSpeedMultiplier.Value = 1f;
                _noMovementInertia.Value = false;
                _collisionFreeMovement.Value = false;
                _collisionFreeFly.Value = false;
                _collisionFreeFlySpeed.Value = 6f;
                _collisionFreeKeepWorldRendered.Value = true;
                _silentMovement.Value = false;
                _noWeight.Value = false;
            }
            EndCategoryPanel();

            BeginCategoryPanel("Loot Interaction");
            DrawOptionToggle(
                _fastContainerSearching,
                " Fast searching (all containers)");
            if (DrawResetGroupButton())
                _fastContainerSearching.Value = false;
            EndCategoryPanel();

            EndCategoryColumns();

            GUILayout.Space(36f);
            EndVerticalScrollView();
        }

        private static void DrawCharacterSlider(
            string label,
            ConfigEntry<float> value,
            float minimum,
            float maximum,
            bool logarithmic = false)
        {
            GUILayout.Label(
                label + ": " + value.Value.ToString("0.###") + "x");

            if (!logarithmic)
            {
                value.Value = GUILayout.HorizontalSlider(
                    value.Value,
                    minimum,
                    maximum);
                return;
            }

            float minimumLog = Mathf.Log10(minimum);
            float maximumLog = Mathf.Log10(maximum);
            float sliderPosition = Mathf.InverseLerp(
                minimumLog,
                maximumLog,
                Mathf.Log10(Mathf.Max(minimum, value.Value)));
            sliderPosition = GUILayout.HorizontalSlider(
                sliderPosition,
                0f,
                1f);
            float nextValue = Mathf.Pow(
                10f,
                Mathf.Lerp(
                    minimumLog,
                    maximumLog,
                    sliderPosition));

            value.Value = nextValue;
        }

        private void DrawCharacterStatus()
        {
            if (_localPlayer == null)
            {
                GUILayout.Label("No local player detected.");
                return;
            }

            ActiveHealthController health =
                _localPlayer.ActiveHealthController;

            if (health != null)
            {
                ValueStruct energy = health.Energy;
                ValueStruct hydration = health.Hydration;
                GUILayout.Label(
                    "Energy: " +
                    energy.Current.ToString("0") + " / " +
                    energy.Maximum.ToString("0"));
                GUILayout.Label(
                    "Hydration: " +
                    hydration.Current.ToString("0") + " / " +
                    hydration.Maximum.ToString("0"));
            }

            if (_localPlayer.Physical != null)
            {
                GUILayout.Label(
                    "Stamina: " +
                    _localPlayer.Physical.Stamina.Current.ToString("0"));
                GUILayout.Label(
                    "Hands: " +
                    _localPlayer.Physical.HandsStamina.Current.ToString("0"));
                GUILayout.Label(
                    "Oxygen: " +
                    _localPlayer.Physical.Oxygen.Current.ToString("0"));
            }
        }
    }
}
