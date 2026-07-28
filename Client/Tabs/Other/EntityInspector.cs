
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private const float EntityInspectorRange = 1000f;
        private const float EntityInspectorMarkerRadius = 0.12f;
        private static readonly RaycastHit[] EntityInspectorHits =
            new RaycastHit[128];
        private Rect _entityInspectorRect =
            new Rect(30f, 30f, 470f, 620f);
        private Vector2 _entityInspectorScroll;
        private RaycastHit _entityInspectorHit;
        private bool _entityInspectorHasHit;
        private string _entityInspectorText =
            "Aim the center of the screen at an entity.";
        private Texture2D _entityInspectorMarker;
        private float _nextEntityInspectorDetails;
        private bool _entityInspectorWasEnabled;

        private void UpdateEntityInspector()
        {
            if (_showEntityInspector == null ||
                !_showEntityInspector.Value ||
                _camera == null)
            {
                if (_entityInspectorWasEnabled)
                {
                    _entityInspectorHasHit = false;
                    _entityInspectorHit = new RaycastHit();
                    _entityInspectorText =
                        "Entity inspector is disabled.";
                    Array.Clear(
                        EntityInspectorHits,
                        0,
                        EntityInspectorHits.Length);
                    _entityInspectorWasEnabled = false;
                }
                return;
            }

            _entityInspectorWasEnabled = true;
            Ray ray = _camera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                EntityInspectorHits,
                EntityInspectorRange,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            _entityInspectorHasHit =
                SelectEntityInspectorHit(hitCount, out _entityInspectorHit);

            if (Time.unscaledTime < _nextEntityInspectorDetails)
                return;

            _nextEntityInspectorDetails = Time.unscaledTime + 0.1f;
            _entityInspectorText = _entityInspectorHasHit
                ? BuildEntityInspectorText(_entityInspectorHit)
                : "No collider under the center-screen ray.";
        }

        private bool SelectEntityInspectorHit(
            int hitCount,
            out RaycastHit selected)
        {
            selected = new RaycastHit();
            float nearest = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = EntityInspectorHits[i];
                Collider collider = hit.collider;
                if (collider == null || hit.distance <= 0.03f)
                    continue;

                Transform transform = collider.transform;
                if (_localPlayer != null &&
                    transform.IsChildOf(
                        _localPlayer.gameObject.transform))
                    continue;
                if (collider.isTrigger &&
                    collider.GetComponentInParent<
                        WorldInteractiveObject>() == null &&
                    collider.GetComponentInParent<LootItem>() == null &&
                    collider.GetComponentInParent<BodyPartCollider>() == null)
                    continue;

                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    selected = hit;
                }
            }

            Array.Clear(EntityInspectorHits, 0, hitCount);
            return nearest < float.MaxValue;
        }

        private static string BuildEntityInspectorText(RaycastHit hit)
        {
            Collider collider = hit.collider;
            if (collider == null)
                return "The hit collider was destroyed.";

            GameObject target = ResolveInspectedObject(collider);
            StringBuilder text = new StringBuilder(2048);
            text.AppendLine("UNITY");
            text.AppendLine("Name: " + target.name);
            text.AppendLine("Instance ID: " + target.GetInstanceID());
            text.AppendLine("Path: " + GetTransformPath(target.transform));
            text.AppendLine(
                "Layer: " + target.layer + " (" +
                LayerMask.LayerToName(target.layer) + ")");
            text.AppendLine("Tag: " + target.tag);
            text.AppendLine("Active: " + target.activeInHierarchy);
            text.AppendLine(
                "Position: " + FormatVector(target.transform.position));
            text.AppendLine(
                "Hit point: " + FormatVector(hit.point));
            text.AppendLine(
                "Hit normal: " + FormatVector(hit.normal));
            text.AppendLine("Distance: " + hit.distance.ToString("0.000"));
            text.AppendLine("Collider: " + collider.GetType().FullName);
            text.AppendLine("Trigger: " + collider.isTrigger);
            text.AppendLine(
                "Bounds center: " + FormatVector(collider.bounds.center));
            text.AppendLine(
                "Bounds size: " + FormatVector(collider.bounds.size));

            Rigidbody body = collider.attachedRigidbody;
            if (body != null)
            {
                text.AppendLine();
                text.AppendLine("RIGIDBODY");
                text.AppendLine("Mass: " + body.mass.ToString("0.###"));
                text.AppendLine(
                    "Velocity: " + FormatVector(body.velocity));
                text.AppendLine("Kinematic: " + body.isKinematic);
            }

            LootItem loot = collider.GetComponentInParent<LootItem>();
            Item item = loot == null ? null : loot.Item;
            if (item != null)
            {
                text.AppendLine();
                text.AppendLine("EFT ITEM");
                text.AppendLine("Name: " + LocalizedItemName(item));
                text.AppendLine("ID: " + item.Id);
                text.AppendLine("Template ID: " + item.TemplateId);
                text.AppendLine("Type: " + item.GetType().FullName);
                text.AppendLine("Stack: " + item.StackObjectsCount);
                text.AppendLine("Weight: " +
                    item.TotalWeight.ToString("0.###"));
                text.AppendLine("Quest item: " + item.QuestItem);
            }

            Component[] components =
                collider.GetComponentsInParent<Component>(true);
            text.AppendLine();
            text.AppendLine("COMPONENTS IN PARENT CHAIN");
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null)
                    text.AppendLine(component.GetType().FullName);
            }

            return text.ToString();
        }

        private static GameObject ResolveInspectedObject(Collider collider)
        {
            BodyPartCollider bodyPart =
                collider.GetComponentInParent<BodyPartCollider>();
            if (bodyPart != null)
            {
                Player player = bodyPart.GetComponentInParent<Player>();
                if (player != null)
                    return player.gameObject;
            }

            LootItem loot = collider.GetComponentInParent<LootItem>();
            if (loot != null)
                return loot.gameObject;

            WorldInteractiveObject interactive =
                collider.GetComponentInParent<WorldInteractiveObject>();
            if (interactive != null)
                return interactive.gameObject;

            Rigidbody body = collider.attachedRigidbody;
            if (body != null)
                return body.gameObject;

            return collider.transform.root.gameObject;
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return "null";

            StringBuilder path = new StringBuilder(transform.name);
            Transform parent = transform.parent;
            while (parent != null)
            {
                path.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return path.ToString();
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.###") + ", " +
                value.y.ToString("0.###") + ", " +
                value.z.ToString("0.###");
        }

        private void DrawEntityInspectorPanel()
        {
            if (_showEntityInspector == null ||
                !_showEntityInspector.Value)
                return;

            DrawEntityInspectorMarker();
            _entityInspectorRect = GUI.Window(
                731907,
                _entityInspectorRect,
                DrawEntityInspectorWindow,
                "Entity Inspector");
        }

        private void DrawEntityInspectorWindow(int windowId)
        {
            _entityInspectorScroll = GUILayout.BeginScrollView(
                _entityInspectorScroll);
            GUILayout.TextArea(_entityInspectorText);
            GUILayout.EndScrollView();
            GUILayout.Label(
                _menuOpen
                    ? "Drag this window by its title bar."
                    : "Open the main menu to free the cursor and drag.");
            GUI.DragWindow(new Rect(
                0f, 0f, _entityInspectorRect.width, 24f));
        }

        private void DrawEntityInspectorMarker()
        {
            if (!_entityInspectorHasHit || _camera == null)
                return;

            Vector3 screen =
                _camera.WorldToScreenPoint(_entityInspectorHit.point);
            if (screen.z <= 0f)
                return;

            EnsureEntityInspectorMarker();
            Vector3 normal = _entityInspectorHit.normal.normalized;
            Vector3 tangent = Vector3.Cross(
                normal,
                Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.95f
                    ? Vector3.right
                    : Vector3.up).normalized;
            Vector3 bitangent =
                Vector3.Cross(normal, tangent).normalized;
            Vector3 center =
                _entityInspectorHit.point + normal * 0.004f;

            const int segments = 40;
            Vector2 previous = Vector2.zero;
            bool hasPrevious = false;
            for (int i = 0; i <= segments; i++)
            {
                float angle =
                    i * Mathf.PI * 2f / segments;
                Vector3 world = center +
                    (tangent * Mathf.Cos(angle) +
                     bitangent * Mathf.Sin(angle)) *
                    EntityInspectorMarkerRadius;
                Vector3 projected =
                    _camera.WorldToScreenPoint(world);
                if (projected.z <= 0f)
                {
                    hasPrevious = false;
                    continue;
                }

                Vector2 current = new Vector2(
                    projected.x,
                    Screen.height - projected.y);
                if (hasPrevious)
                    DrawEntityInspectorLine(
                        previous,
                        current,
                        new Color(1f, 0.72f, 0.1f, 1f),
                        2f);
                previous = current;
                hasPrevious = true;
            }
        }

        private void EnsureEntityInspectorMarker()
        {
            if (_entityInspectorMarker != null)
                return;

            _entityInspectorMarker = new Texture2D(
                1, 1, TextureFormat.RGBA32, false)
            {
                name = "FieldKit Entity Inspector Marker",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            _entityInspectorMarker.SetPixel(0, 0, Color.white);
            _entityInspectorMarker.Apply(false, true);
        }

        private void DrawEntityInspectorLine(
            Vector2 start,
            Vector2 end,
            Color color,
            float thickness)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.01f)
                return;

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            float angle =
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(
                    start.x,
                    start.y - thickness * 0.5f,
                    length,
                    thickness),
                _entityInspectorMarker);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }
    }
}
