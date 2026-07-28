
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void UpdateFastContainerSearching()
        {
            SkillManager skills =
                _localPlayer == null ? null : _localPlayer.Skills;

            if (!ReferenceEquals(skills, _searchSkillManager))
            {
                RestoreContainerSearchOverride();
                _searchSkillManager = skills;
            }

            if (skills == null || !_fastContainerSearching.Value)
            {
                RestoreContainerSearchOverride();
                return;
            }

            if (!_hasSavedContainerScope)
            {
                _savedContainerScope =
                    skills.IntellectEliteContainerScope.Value;
                _hasSavedContainerScope = true;
            }

            if (!skills.IntellectEliteContainerScope.Value)
                skills.IntellectEliteContainerScope.Value = true;
        }

        private void RestoreContainerSearchOverride()
        {
            if (_searchSkillManager != null &&
                _hasSavedContainerScope)
            {
                _searchSkillManager.IntellectEliteContainerScope.Value =
                    _savedContainerScope;
            }

            _hasSavedContainerScope = false;
            _searchSkillManager = null;
        }

        private void UpdateCharacterTools()
        {
            UpdateFastContainerSearching();
            UpdateCollisionFreeFlight();
            UpdateCollisionFreeProximity();
            UpdateDisabledPlayerColliders();
            UpdateCollisionFreeRendering();
            UpdateCollisionFreeFloorTraversal();

            ActiveHealthController health =
                _localPlayer == null
                    ? null
                    : _localPlayer.ActiveHealthController;

            if (health == null)
            {
                _lastCharacterRecoveryTime = 0f;
                _nextCharacterRecoveryTime = 0f;
                return;
            }

            if (_infiniteEnergy.Value)
            {
                ValueStruct energy = health.Energy;

                if (energy.Current < energy.Maximum)
                    health.ChangeEnergy(
                        energy.Maximum - energy.Current);
            }

            if (_infiniteHydration.Value)
            {
                ValueStruct hydration = health.Hydration;

                if (hydration.Current < hydration.Maximum)
                    health.ChangeHydration(
                        hydration.Maximum - hydration.Current);
            }

            float now = Time.unscaledTime;

            if (now < _nextCharacterRecoveryTime)
                return;

            float elapsed = _lastCharacterRecoveryTime <= 0f
                ? 0.1f
                : Mathf.Clamp(
                    now - _lastCharacterRecoveryTime,
                    0.01f,
                    0.5f);
            _lastCharacterRecoveryTime = now;
            _nextCharacterRecoveryTime = now + 0.1f;

            float recovery =
                _healthRegeneration.Value * elapsed;

            if (recovery <= 0f)
                return;

            for (int i = 0; i < 7; i++)
            {
                EBodyPart bodyPart = (EBodyPart)i;

                if (health.IsBodyPartDestroyed(bodyPart))
                    continue;

                ValueStruct value =
                    health.GetBodyPartHealth(bodyPart, false);
                float amount = Mathf.Min(
                    recovery,
                    value.Maximum - value.Current);

                if (amount > 0f)
                    health.ChangeHealth(
                        bodyPart,
                        amount,
                        new DamageInfoStruct());
            }
        }

        private void RestoreLocalCharacter()
        {
            ActiveHealthController health =
                _localPlayer == null
                    ? null
                    : _localPlayer.ActiveHealthController;

            if (health == null)
                return;

            for (int i = 0; i < 7; i++)
                health.FullRestoreBodyPart((EBodyPart)i);
        }

        private void RemoveLocalNegativeEffects()
        {
            ActiveHealthController health =
                _localPlayer == null
                    ? null
                    : _localPlayer.ActiveHealthController;

            if (health == null)
                return;

            for (int i = 0; i <= 7; i++)
                health.RemoveNegativeEffects((EBodyPart)i);
        }

    }
}
