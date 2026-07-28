
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void UpdateCollisionFreeFlight()
        {
            bool active =
                _localPlayer != null &&
                _collisionFreeMovement != null &&
                _collisionFreeMovement.Value &&
                _collisionFreeFly != null &&
                _collisionFreeFly.Value;
            if (!active)
            {
                _collisionFreeFlyVelocity = 0f;
                _collisionFreeFlyWasActive = false;
                return;
            }

            if (!_collisionFreeFlyWasActive)
            {
                _hasCollisionFreeFloor = false;
                _collisionFreeFlyWasActive = true;
            }

            float direction = 0f;
            if (Input.GetKey(KeyCode.Space))
                direction += 1f;
            if (Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl))
                direction -= 1f;

            float speed = _collisionFreeFlySpeed.Value;
            _collisionFreeFlyVelocity = Mathf.MoveTowards(
                _collisionFreeFlyVelocity,
                direction * speed,
                speed * 4f * Time.unscaledDeltaTime);
        }

        private void UpdateCollisionFreeProximity()
        {
            if (_localPlayer == null ||
                _collisionFreeMovement == null ||
                !_collisionFreeMovement.Value ||
                _collisionFreeKeepWorldRendered == null ||
                !_collisionFreeKeepWorldRendered.Value)
            {
                ResetCollisionFreeWallPass();
                return;
            }

            MovementContext movement =
                _localPlayer.MovementContext;
            SimpleCharacterController controller =
                movement == null
                    ? null
                    : movement.CharacterController
                        as SimpleCharacterController;
            if (controller == null)
                return;

            Vector3 playerPosition = movement.TransformPosition;
            if (_hasCollisionFreePreviousPosition)
            {
                Vector3 travel =
                    playerPosition -
                    _collisionFreePreviousPosition;
                travel.y = 0f;
                if (travel.sqrMagnitude > 0.000004f)
                    _collisionFreeTravelDirection =
                        travel.normalized;
                else
                    _collisionFreeTravelDirection =
                        Vector3.zero;
            }

            _collisionFreePreviousPosition = playerPosition;
            _hasCollisionFreePreviousPosition = true;

            if (_hasCollisionFreeWallPass)
            {
                float projection =
                    Vector3.Dot(
                        playerPosition,
                        _collisionFreeWallDirection);
                float clearance =
                    _collisionFreeWallBodyRadius + 0.025f;
                Vector3 fromContact =
                    playerPosition -
                    _collisionFreeWallContactPosition;
                float normalTravel =
                    Vector3.Dot(
                        fromContact,
                        _collisionFreeWallDirection);
                Vector3 lateralTravel =
                    fromContact -
                    _collisionFreeWallDirection *
                    normalTravel;
                lateralTravel.y = 0f;
                bool movingAlongSurface =
                    _collisionFreeIntendedDirection.sqrMagnitude <
                        0.5f ||
                    Mathf.Abs(
                        Vector3.Dot(
                            _collisionFreeIntendedDirection,
                            _collisionFreeWallDirection)) < 0.3f;
                bool clearedWallEdge =
                    movingAlongSurface &&
                    lateralTravel.sqrMagnitude >
                        Mathf.Pow(
                            Mathf.Max(
                                _collisionFreeWallBodyRadius * 2f,
                                0.65f),
                            2f);
                if (projection >=
                        _collisionFreeWallExit + clearance ||
                    projection <=
                        _collisionFreeWallEntry - clearance ||
                    clearedWallEdge)
                {
                    ResetCollisionFreeWallPass(false);
                    _nextPlayerColliderRefresh = 0f;
                }
                else
                {
                    _collisionFreeNearBlockingGeometry = true;
                    return;
                }
            }

            Collider movementCollider = controller.GetCollider();
            float verticalScale =
                movementCollider == null
                    ? 1f
                    : Mathf.Abs(
                        movementCollider.transform
                            .lossyScale.y);
            float horizontalScale =
                movementCollider == null
                    ? 1f
                    : Mathf.Max(
                        Mathf.Abs(
                            movementCollider.transform
                                .lossyScale.x),
                        Mathf.Abs(
                            movementCollider.transform
                                .lossyScale.z));
            float height =
                controller.height * verticalScale;
            float feet =
                playerPosition.y +
                GetCollisionFreeFeetOffset(controller);
            float bodyRadius =
                Mathf.Max(
                    controller.radius * horizontalScale,
                    0.2f);
            float probeRadius = bodyRadius + 0.015f;
            Vector3 lowerPoint = new Vector3(
                playerPosition.x,
                feet + height * 0.42f,
                playerPosition.z);
            Vector3 upperPoint = new Vector3(
                playerPosition.x,
                feet + height * 0.9f,
                playerPosition.z);

            Vector3 direction =
                _collisionFreeIntendedDirection.sqrMagnitude >
                    0.5f
                    ? _collisionFreeIntendedDirection
                    : _collisionFreeTravelDirection;
            if (direction.sqrMagnitude < 0.5f)
            {
                _collisionFreeNearBlockingGeometry = false;
                return;
            }

            direction.y = 0f;
            direction.Normalize();

            int count = Physics.CapsuleCastNonAlloc(
                lowerPoint,
                upperPoint,
                probeRadius,
                direction,
                CollisionFreeWallHits,
                0.12f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            Transform localPlayerTransform =
                Original(_localPlayer.Transform);

            Collider nearestCollider = null;
            Vector3 nearestPoint = Vector3.zero;
            Vector3 nearestNormal = Vector3.zero;
            float nearestDistance = float.MaxValue;
            Vector3 probeCenter =
                (lowerPoint + upperPoint) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = CollisionFreeWallHits[i];
                Collider collider =
                    hit.collider;
                if (collider == null ||
                    !collider.enabled ||
                    collider.isTrigger ||
                    (localPlayerTransform != null &&
                     collider.transform.IsChildOf(
                         localPlayerTransform)) ||
                    collider.bounds.size.y <
                        height * 0.5f ||
                    collider.bounds.max.y <
                        feet + height * 0.72f)
                    continue;
                if (Mathf.Abs(hit.normal.y) > 0.22f)
                    continue;

                Vector3 horizontalNormal = hit.normal;
                horizontalNormal.y = 0f;
                if (horizontalNormal.sqrMagnitude < 0.5f)
                    continue;

                horizontalNormal.Normalize();
                if (Vector3.Dot(
                        direction,
                        -horizontalNormal) < 0.65f ||
                    hit.distance >= nearestDistance)
                    continue;

                nearestCollider = collider;
                nearestPoint = hit.point;
                nearestNormal = horizontalNormal;
                nearestDistance = hit.distance;
            }

            if (nearestCollider == null)
            {
                _collisionFreeNearBlockingGeometry = false;
                return;
            }

            CacheCollisionFreeWallPass(
                nearestCollider,
                nearestPoint,
                probeCenter,
                -nearestNormal,
                bodyRadius);
            _collisionFreeNearBlockingGeometry = true;
            _nextPlayerColliderRefresh = 0f;
        }

        private void CacheCollisionFreeWallPass(
            Collider collider,
            Vector3 entryPoint,
            Vector3 probeCenter,
            Vector3 direction,
            float bodyRadius)
        {
            direction.y = 0f;
            direction.Normalize();

            float entry =
                Vector3.Dot(entryPoint, direction);
            Bounds bounds = collider.bounds;
            float projectedExtent =
                Mathf.Abs(direction.x) * bounds.extents.x +
                Mathf.Abs(direction.z) * bounds.extents.z;
            float farBound =
                Vector3.Dot(bounds.center, direction) +
                projectedExtent;
            float exit = farBound;

            Vector3 farOrigin =
                probeCenter +
                direction *
                Mathf.Max(
                    farBound -
                    Vector3.Dot(probeCenter, direction) +
                    0.5f,
                    1f);
            RaycastHit farHit;
            float reverseDistance =
                Vector3.Distance(farOrigin, probeCenter) +
                bodyRadius + 1f;
            if (collider.Raycast(
                    new Ray(farOrigin, -direction),
                    out farHit,
                    reverseDistance))
                exit = Vector3.Dot(farHit.point, direction);
            float maximumWallThickness =
                Mathf.Max(bodyRadius * 3f, 1.25f);
            exit = Mathf.Clamp(
                exit,
                entry + 0.02f,
                entry + maximumWallThickness);

            _hasCollisionFreeWallPass = true;
            _collisionFreeWallDirection = direction;
            _collisionFreeWallEntry = entry;
            _collisionFreeWallExit = exit;
            _collisionFreeWallBodyRadius = bodyRadius;
            _collisionFreeWallContactPosition = probeCenter;
        }

        private void ResetCollisionFreeWallPass(
            bool resetPosition = true)
        {
            bool wasPassingWall =
                _hasCollisionFreeWallPass;
            _collisionFreeNearBlockingGeometry = false;
            _hasCollisionFreeWallPass = false;
            _collisionFreeWallDirection = Vector3.zero;
            _collisionFreeWallEntry = 0f;
            _collisionFreeWallExit = 0f;
            _collisionFreeWallBodyRadius = 0f;
            _collisionFreeWallContactPosition = Vector3.zero;
            if (wasPassingWall && !resetPosition)
                _collisionFreeRenderRecoveryUntil =
                    Time.unscaledTime + 0.75f;
            if (resetPosition)
            {
                _hasCollisionFreePreviousPosition = false;
                _collisionFreePreviousPosition = Vector3.zero;
                _collisionFreeTravelDirection = Vector3.zero;
                _collisionFreeIntendedDirection = Vector3.zero;
                _collisionFreeRenderRecoveryUntil = 0f;
            }
        }

        private void UpdateDisabledPlayerColliders()
        {
            if (_localPlayer == null ||
                _collisionFreeMovement == null ||
                !_collisionFreeMovement.Value)
            {
                RestoreDisabledPlayerColliders();
                return;
            }

            if (Time.unscaledTime <
                _nextPlayerColliderRefresh)
                return;

            _nextPlayerColliderRefresh =
                Time.unscaledTime + 0.25f;

            Collider[] colliders =
                _localPlayer.GetComponentsInChildren<Collider>(
                    true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                if (!_playerColliderStates
                        .ContainsKey(collider))
                {
                    _playerColliderStates.Add(
                        collider,
                        new PlayerColliderState
                        {
                            Enabled = collider.enabled,
                            IsTrigger = collider.isTrigger
                        });
                }

                PlayerColliderState original =
                    _playerColliderStates[collider];
                if (!original.Enabled)
                {
                    collider.enabled = false;
                    continue;
                }

                if (_collisionFreeKeepWorldRendered.Value &&
                    !_collisionFreeNearBlockingGeometry)
                {
                    collider.enabled = true;
                    collider.isTrigger = true;
                }
                else
                {
                    collider.enabled = false;
                }
            }
        }

        private void RestoreDisabledPlayerColliders()
        {
            if (_playerColliderStates.Count > 0)
            {
                foreach (
                    KeyValuePair<Collider, PlayerColliderState> pair in
                    _playerColliderStates)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.isTrigger =
                            pair.Value.IsTrigger;
                        pair.Key.enabled =
                            pair.Value.Enabled;
                    }
                }
            }

            _playerColliderStates.Clear();
            _nextPlayerColliderRefresh = 0f;
            ResetCollisionFreeWallPass();
            _hasCollisionFreeFloor = false;
            _collisionFreeFloorPositionY = 0f;
        }

        private void UpdateCollisionFreeRendering()
        {
            bool forceVisible =
                _localPlayer != null &&
                _collisionFreeMovement != null &&
                _collisionFreeMovement.Value &&
                _collisionFreeKeepWorldRendered != null &&
                _collisionFreeKeepWorldRendered.Value &&
                !_collisionFreeNearBlockingGeometry;

            if (!forceVisible)
            {
                CullingManager contactCullingManager =
                    CullingManager.Instance;
                if (contactCullingManager != null)
                {
                    if (_collisionFreeCullingLocked)
                        contactCullingManager.LockState(false);
                    if (_collisionFreeNearBlockingGeometry)
                        contactCullingManager.ForceEnable(false);
                }

                _collisionFreeCullingLocked = false;
                _nextCollisionFreeCullingRefresh = 0f;
                return;
            }

            bool recoveringVisibility =
                Time.unscaledTime <
                _collisionFreeRenderRecoveryUntil;
            if (!recoveringVisibility &&
                Time.unscaledTime <
                    _nextCollisionFreeCullingRefresh)
                return;

            _nextCollisionFreeCullingRefresh =
                recoveringVisibility
                    ? Time.unscaledTime
                    : Time.unscaledTime + 0.25f;

            CullingManager manager = CullingManager.Instance;
            if (manager == null)
                return;

            manager.ForceEnable(true);
            manager.LockState(true);
            _collisionFreeCullingLocked = true;
        }

        private void UpdateCollisionFreeFloorTraversal()
        {
            if (_localPlayer == null ||
                _collisionFreeMovement == null ||
                !_collisionFreeMovement.Value ||
                (_collisionFreeFly != null &&
                 _collisionFreeFly.Value))
                return;

            bool moveUp =
                _collisionFreeMoveUpFloorKey != null &&
                _collisionFreeMoveUpFloorKey.Value.IsDown();
            bool moveDown =
                _collisionFreeMoveDownFloorKey != null &&
                _collisionFreeMoveDownFloorKey.Value.IsDown();
            if (!moveUp && !moveDown)
                return;

            float floorHeight;
            if (!TryFindCollisionFreeFloor(
                    moveUp,
                    out floorHeight))
            {
                LogSource.LogWarning(
                    moveUp
                        ? "No walkable floor found above."
                        : "No walkable floor found below.");
                return;
            }

            MovementContext movement =
                _localPlayer.MovementContext;
            SimpleCharacterController controller =
                movement == null
                    ? null
                    : movement.CharacterController
                        as SimpleCharacterController;
            if (controller == null)
                return;

            float feetOffset =
                GetCollisionFreeFeetOffset(controller);
            Vector3 destination =
                movement.TransformPosition;
            destination.y = floorHeight - feetOffset;

            _collisionFreeFloorPositionY = destination.y;
            _hasCollisionFreeFloor = true;
            _localPlayer.Teleport(destination, false);
        }

        private bool TryFindCollisionFreeFloor(
            bool upward,
            out float floorHeight)
        {
            floorHeight = 0f;

            MovementContext movement =
                _localPlayer == null
                    ? null
                    : _localPlayer.MovementContext;
            SimpleCharacterController controller =
                movement == null
                    ? null
                    : movement.CharacterController
                        as SimpleCharacterController;
            if (controller == null)
                return false;

            float feetOffset =
                GetCollisionFreeFeetOffset(controller);
            Vector3 position = movement.TransformPosition;
            float feetHeight = position.y + feetOffset;
            const float floorSeparation = 0.45f;
            const float searchDistance = 100f;
            float minimumFloorNormal =
                Mathf.Cos(
                    controller.slopeLimit *
                    Mathf.Deg2Rad);

            Vector3 rayOrigin = new Vector3(
                position.x,
                upward
                    ? feetHeight + searchDistance
                    : feetHeight + 0.2f,
                position.z);
            float rayEnd =
                upward
                    ? feetHeight + floorSeparation
                    : feetHeight - searchDistance;
            bool found = false;
            float bestFloor =
                upward
                    ? float.PositiveInfinity
                    : float.NegativeInfinity;

            for (int i = 0; i < 128; i++)
            {
                float remaining = rayOrigin.y - rayEnd;
                if (remaining <= 0.01f)
                    break;

                RaycastHit hit;
                if (!Physics.Raycast(
                        rayOrigin,
                        Vector3.down,
                        out hit,
                        remaining,
                        movement.GroundMask,
                        QueryTriggerInteraction.Ignore))
                    break;

                if (hit.normal.y >= minimumFloorNormal)
                {
                    if (upward)
                    {
                        if (hit.point.y >
                                feetHeight +
                                floorSeparation &&
                            hit.point.y < bestFloor)
                        {
                            bestFloor = hit.point.y;
                            found = true;
                        }
                    }
                    else if (hit.point.y <
                                 feetHeight -
                                 floorSeparation)
                    {
                        floorHeight = hit.point.y;
                        return true;
                    }
                }

                float nextOriginY = hit.point.y - 0.05f;
                if (nextOriginY >= rayOrigin.y)
                    nextOriginY = rayOrigin.y - 0.05f;

                rayOrigin.y = nextOriginY;
            }

            if (found)
                floorHeight = bestFloor;

            return found;
        }

        private static float GetCollisionFreeFeetOffset(
            SimpleCharacterController controller)
        {
            Collider movementCollider = controller.GetCollider();
            float verticalScale =
                movementCollider == null
                    ? 1f
                    : Mathf.Abs(
                        movementCollider.transform
                            .lossyScale.y);
            return (
                controller.center.y -
                controller.height * 0.5f) *
                verticalScale;
        }

    }
}
