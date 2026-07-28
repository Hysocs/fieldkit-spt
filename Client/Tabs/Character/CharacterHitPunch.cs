
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private static bool _processingLocalDamageReaction;

        private static void ScaleVisualHitPunch(
            ref float hitPower)
        {
            if (_instance == null ||
                _instance._visualHitPunchAmount == null)
            {
                return;
            }

            hitPower *= Mathf.Clamp01(
                _instance._visualHitPunchAmount.Value);
        }

        private static void BeginVisualDamageReaction()
        {
            _processingLocalDamageReaction = true;
        }

        private static void EndVisualDamageReaction()
        {
            _processingLocalDamageReaction = false;
        }

        private static void ScaleVisualDamageCameraForce(
            ref float camera)
        {
            if (!_processingLocalDamageReaction ||
                _instance == null ||
                _instance._visualHitPunchAmount == null)
            {
                return;
            }

            camera *= Mathf.Clamp01(
                _instance._visualHitPunchAmount.Value);
        }
    }
}
