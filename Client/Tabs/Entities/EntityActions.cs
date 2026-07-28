
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void BringEntityToSelf(Player target)
        {
            if (!CanRelocate(target))
                return;

            try
            {
                Vector3 destination =
                    GetSafePlayerDestination(
                        target,
                        _localPlayer.Position,
                        _localPlayer.Transform.forward,
                        _localPlayer);
                BotOwner botOwner =
                    target.AIData == null
                        ? null
                        : target.AIData.BotOwner;
                if (botOwner != null &&
                    botOwner.Mover != null)
                {
                    botOwner.StopMove();
                    botOwner.Mover.Teleport(destination);
                }
                else
                {
                    target.Teleport(destination, false);
                }

                ShowLootActionMessage(
                    "Moved " + GetEntityDisplayName(target) +
                    " to you.");
                _nextEntityListRefresh = 0f;
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not relocate entity: " + exception);
                ShowLootActionMessage(
                    "Could not move that entity.",
                    true);
            }
        }

        private void TeleportSelfToEntity(Player target)
        {
            if (!CanRelocate(target))
                return;

            try
            {
                Vector3 destination =
                    GetSafePlayerDestination(
                        _localPlayer,
                        target.Position,
                        -target.Transform.forward,
                        target);
                _localPlayer.Teleport(destination, false);
                ShowLootActionMessage(
                    "Teleported to " +
                    GetEntityDisplayName(target) + ".");
                _nextEntityListRefresh = 0f;
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not teleport to entity: " + exception);
                ShowLootActionMessage(
                    "Could not teleport to that entity.",
                    true);
            }
        }

        private void BringLootToSelf(LootItem loot)
        {
            if (!CanRelocate(loot))
                return;

            try
            {
                Vector3 destination =
                    GetSafeLootDestination(loot);
                loot.StopPhysics();
                Rigidbody rigidBody = loot.RigidBody;
                if (rigidBody != null)
                {
                    rigidBody.position = destination;
                    rigidBody.transform.position = destination;
                }

                loot.transform.position = destination;
                Physics.SyncTransforms();
                ShowLootActionMessage(
                    "Moved " + GetLootDisplayName(loot) +
                    " to you.");
                _nextEntityListRefresh = 0f;
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not relocate loose loot: " + exception);
                ShowLootActionMessage(
                    "Could not move that loot item.",
                    true);
            }
        }

        private void TeleportSelfToLoot(LootItem loot)
        {
            if (!CanRelocate(loot))
                return;

            try
            {
                Vector3 direction =
                    _localPlayer.Position -
                    loot.transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.01f)
                    direction = Vector3.back;
                Vector3 destination =
                    GetSafePlayerDestination(
                        _localPlayer,
                        loot.transform.position,
                        direction.normalized,
                        null);
                _localPlayer.Teleport(destination, false);
                ShowLootActionMessage(
                    "Teleported to " +
                    GetLootDisplayName(loot) + ".");
                _nextEntityListRefresh = 0f;
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not teleport to loose loot: " +
                    exception);
                ShowLootActionMessage(
                    "Could not teleport to that loot item.",
                    true);
            }
        }

        private bool CanRelocate(Player player)
        {
            return _world != null &&
                   _localPlayer != null &&
                   player != null &&
                   player.HealthController != null &&
                   player.HealthController.IsAlive;
        }

        private bool CanRelocate(LootItem loot)
        {
            return _world != null &&
                   _localPlayer != null &&
                   loot != null &&
                   loot.Item != null;
        }

        private static Vector3 GetSafePlayerDestination(
            Player movingPlayer,
            Vector3 anchor,
            Vector3 direction,
            Player nearbyPlayer)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector3.forward;
            direction.Normalize();

            Vector3 candidate = anchor + direction * 1.4f;
            Vector3 rayOrigin = new Vector3(
                candidate.x,
                anchor.y + 0.75f,
                candidate.z);
            int hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                Vector3.down,
                EntityGroundHits,
                8f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            Transform movingTransform =
                movingPlayer == null
                    ? null
                    : Original(movingPlayer.Transform);
            Transform nearbyTransform =
                nearbyPlayer == null
                    ? null
                    : Original(nearbyPlayer.Transform);
            bool foundGround = false;
            float groundY = anchor.y;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = EntityGroundHits[i];
                Collider collider = hit.collider;
                if (collider == null ||
                    hit.normal.y < 0.55f ||
                    (movingTransform != null &&
                     collider.transform.IsChildOf(movingTransform)) ||
                    (nearbyTransform != null &&
                     collider.transform.IsChildOf(nearbyTransform)))
                {
                    continue;
                }

                if (!foundGround || hit.point.y > groundY)
                {
                    foundGround = true;
                    groundY = hit.point.y;
                }
            }

            float bottomOffset =
                GetPlayerColliderBottomOffset(movingPlayer);
            candidate.y = groundY - bottomOffset + 0.02f;
            return candidate;
        }

        private static float GetPlayerColliderBottomOffset(
            Player player)
        {
            if (player == null ||
                player.MovementContext == null ||
                player.MovementContext.CharacterController == null)
            {
                return 0f;
            }

            Collider collider =
                player.MovementContext.CharacterController
                    .GetCollider();
            return collider == null
                ? 0f
                : collider.bounds.min.y - player.Position.y;
        }

        private Vector3 GetSafeLootDestination(
            LootItem loot)
        {
            Vector3 forward = _localPlayer.Transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            Vector3 anchor = _localPlayer.Position;
            Vector3 desired = anchor + forward * 1.4f;
            Vector3 rayOrigin = new Vector3(
                desired.x,
                anchor.y + 0.75f,
                desired.z);
            int hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                Vector3.down,
                EntityGroundHits,
                8f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            bool foundGround = false;
            float groundY = anchor.y;
            Transform playerTransform =
                Original(_localPlayer.Transform);
            Transform lootTransform = loot.transform;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = EntityGroundHits[i];
                Collider collider = hit.collider;
                if (collider == null ||
                    hit.normal.y < 0.55f ||
                    (playerTransform != null &&
                     collider.transform.IsChildOf(playerTransform)) ||
                    collider.transform.IsChildOf(lootTransform))
                {
                    continue;
                }

                if (!foundGround || hit.point.y > groundY)
                {
                    foundGround = true;
                    groundY = hit.point.y;
                }
            }

            float bottomOffset =
                GetLootColliderBottomOffset(loot);
            desired.y = groundY - bottomOffset + 0.02f;
            return desired;
        }

        private static float GetLootColliderBottomOffset(
            LootItem loot)
        {
            Collider[] colliders =
                loot.GetComponentsInChildren<Collider>(true);
            bool foundBounds = false;
            float minimumY = float.PositiveInfinity;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null ||
                    !collider.enabled ||
                    collider.isTrigger)
                {
                    continue;
                }

                foundBounds = true;
                minimumY = Mathf.Min(
                    minimumY,
                    collider.bounds.min.y);
            }

            return foundBounds
                ? minimumY - loot.transform.position.y
                : 0f;
        }

        private static string GetEntityDisplayName(
            Player player)
        {
            return player.Profile != null &&
                   player.Profile.Info != null &&
                   !string.IsNullOrWhiteSpace(
                       player.Profile.Info.Nickname)
                ? player.Profile.Info.Nickname
                : "entity";
        }

        private string GetLootDisplayName(LootItem loot)
        {
            string id = loot.Item.TemplateId.ToString();
            LootCatalogItem catalogItem;
            return _lootCatalogItems.TryGetValue(
                    id,
                    out catalogItem)
                ? catalogItem.Name
                : "loot item";
        }
    }
}
