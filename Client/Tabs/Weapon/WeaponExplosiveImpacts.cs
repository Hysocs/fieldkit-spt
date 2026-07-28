
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private const string BarrelExplosionEffect = "Grenade_new2";
        private static Effects _impactEffects;

        private static void SpawnBarrelImpactExplosion(
            EftBulletClass bullet,
            RaycastHit hit)
        {
            if (bullet.Player == null)
                return;

            Vector3 point = hit.point + hit.normal * 0.05f;
            ImpactExplosiveItem explosion =
                new ImpactExplosiveItem(bullet.Ammo as AmmoItemClass);
            ISharedBallisticsCalculator calculator =
                Singleton<GameWorld>.Instance.SharedBallisticsCalculator;

            GClass2085.Explosion(
                explosion,
                point,
                bullet.PlayerProfileID,
                calculator,
                bullet.Weapon,
                () => CreateBarrelExplosionDamage(bullet, hit, point),
                0f,
                0f,
                null,
                false);

            if (_impactEffects == null)
                _impactEffects = Object.FindObjectOfType<Effects>();

            if (_impactEffects != null)
                _impactEffects.EmitGrenade(
                    BarrelExplosionEffect,
                    point,
                    hit.normal,
                    1f);
        }

        private static DamageInfoStruct CreateBarrelExplosionDamage(
            EftBulletClass bullet,
            RaycastHit hit,
            Vector3 point)
        {
            return new DamageInfoStruct
            {
                DamageType = EDamageType.Explosion,
                Damage = 120f,
                ArmorDamage = 35f,
                PenetrationPower = 20f,
                HitCollider = hit.collider,
                Direction = bullet.Direction,
                HitPoint = point,
                MasterOrigin = point,
                HitNormal = hit.normal,
                Player = bullet.Player,
                Weapon = bullet.Weapon,
                FireIndex = bullet.FireIndex,
                SourceId = bullet.PlayerProfileID
            };
        }

        private sealed class ImpactExplosiveItem : IExplosiveItem
        {
            private readonly AmmoItemClass _fragment;

            public ImpactExplosiveItem(AmmoItemClass fragment)
            {
                _fragment = fragment;
            }

            public Vector3 Blindness => Vector3.zero;
            public Vector3 Contusion => new Vector3(2f, 8f, 5f);
            public Vector3 ArmorDistanceDistanceDamage =>
                new Vector3(1.5f, 6f, 35f);
            public float MinExplosionDistance => 1.5f;
            public float MaxExplosionDistance => 7f;
            public int FragmentsCount => 0;
            public float GetStrength => 120f;
            public bool IsDummy => false;
            public AmmoItemClass CreateFragment() => _fragment;
        }
    }
}
