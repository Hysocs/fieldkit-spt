
namespace FieldKit
{
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private void RefreshCamera()
        {
            if (_camera != null &&
                _camera.enabled &&
                _camera.gameObject.activeInHierarchy &&
                _camera.targetTexture == null)
                return;

            Camera best = null;
            float bestScore = float.MinValue;

            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();

            foreach (Camera candidate in cameras)
            {
                if (candidate == null ||
                    !candidate.enabled ||
                    !candidate.gameObject.activeInHierarchy ||
                    candidate.orthographic ||
                    candidate.targetTexture != null)
                    continue;

                float score = candidate.depth;

                if (candidate.CompareTag("MainCamera"))
                    score += 500f;

                if (candidate.name.IndexOf("FPS", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 1000f;

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            _camera = best != null ? best : Camera.main;
        }

        private void RefreshScopeOverlays()
        {
            for (int i = 0; i < _scopeOverlays.Count; i++)
                _scopeOverlays[i].Seen = false;

            foreach (Camera candidate in Camera.allCameras)
            {
                if (!IsScopeCamera(candidate))
                    continue;

                ScopeOverlay existing = null;

                for (int i = 0; i < _scopeOverlays.Count; i++)
                {
                    if (_scopeOverlays[i].Camera == candidate)
                    {
                        existing = _scopeOverlays[i];
                        break;
                    }
                }

                if (existing != null)
                {
                    existing.Seen = true;
                    existing.LastSeenTime = Time.unscaledTime;

                    Renderer currentLens = ResolveOpticLens(candidate);

                    if (currentLens != null &&
                        currentLens != existing.LensRenderer)
                    {
                        existing.LensRenderer = currentLens;
                        existing.HasLensScreenRect = false;
                        if (_cameraDebug.Value)
                        {
                            LogSource.LogInfo(
                                "[Scope Debug] Active optic lens changed to: " +
                                TransformPath(currentLens.transform));
                        }
                    }
                    else if (currentLens == null &&
                             existing.LensRenderer != null &&
                             !existing.LensRenderer.gameObject.activeInHierarchy)
                    {
                        existing.LensRenderer = null;
                        existing.HasLensScreenRect = false;
                    }

                    if (existing.Pass.EnsureAttached() &&
                        _cameraDebug.Value)
                    {
                        LogSource.LogInfo(
                            "[Scope Debug] Reattached optic command buffer: " +
                            candidate.name);
                    }

                    continue;
                }
                MakeRoomForScopeOverlay();

                if (_scopeOverlays.Count < 4)
                {
                    if (_cameraDebug.Value)
                    {
                        LogSource.LogInfo(
                            "[Scope Debug] Camera: " + candidate.name +
                            ", texture=" + candidate.targetTexture.name);
                    }

                    ScopeOverlay created =
                        CreateScopeOverlay(candidate);
                    if (created != null)
                        _scopeOverlays.Add(created);
                }
            }

            for (int i = _scopeOverlays.Count - 1; i >= 0; i--)
            {
                ScopeOverlay overlay = _scopeOverlays[i];
                if (overlay.Camera != null &&
                    overlay.Pass != null &&
                    (overlay.Seen ||
                     Time.unscaledTime - overlay.LastSeenTime < 15f))
                    continue;

                DestroyScopeOverlay(overlay);
                _scopeOverlays.RemoveAt(i);
            }
        }

        private void MakeRoomForScopeOverlay()
        {
            if (_scopeOverlays.Count < 4)
                return;

            int oldestIndex = -1;
            float oldestTime = float.MaxValue;

            for (int i = 0; i < _scopeOverlays.Count; i++)
            {
                ScopeOverlay overlay = _scopeOverlays[i];

                if (overlay.Seen || overlay.LastSeenTime >= oldestTime)
                    continue;

                oldestTime = overlay.LastSeenTime;
                oldestIndex = i;
            }

            if (oldestIndex < 0)
                return;

            DestroyScopeOverlay(_scopeOverlays[oldestIndex]);
            _scopeOverlays.RemoveAt(oldestIndex);
        }

        private bool IsScopeCamera(Camera candidate)
        {
            if (candidate == null ||
                candidate == _camera ||
                !candidate.enabled ||
                !candidate.gameObject.activeInHierarchy ||
                candidate.orthographic ||
                candidate.targetTexture == null)
                return false;

            return candidate.name.IndexOf(
                       "Optic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   candidate.targetTexture.name.IndexOf(
                       "Optic", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string TransformPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            StringBuilder path = new StringBuilder(transform.name);
            Transform parent = transform.parent;

            while (parent != null)
            {
                path.Insert(0, '/');
                path.Insert(0, parent.name);
                parent = parent.parent;
            }

            return path.ToString();
        }

        private ScopeOverlay CreateScopeOverlay(Camera camera)
        {
            if (camera == null)
                return null;

            if (_font == null)
                _font = LoadFont();
            if (_font == null)
            {
                LogSource.LogWarning(
                    "Scope ESP skipped because no Unity font is available.");
                return null;
            }

            try
            {
                return new ScopeOverlay
                {
                    Camera = camera,
                    Pass = new ScopeRenderPass(camera, _font),
                    LensRenderer = ResolveOpticLens(camera),
                    LastSeenTime = Time.unscaledTime,
                    Seen = true
                };
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not create scope ESP render pass: " +
                    exception.Message);
                return null;
            }
        }

        private static Renderer ResolveOpticLens(Camera camera)
        {
            if (camera == null)
                return null;

            Component[] components = camera.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                if (component == null ||
                    component.GetType().Name != "OpticComponentUpdater")
                    continue;

                try
                {
                    FieldInfo[] fields = component.GetType().GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                    for (int fieldIndex = 0;
                         fieldIndex < fields.Length;
                         fieldIndex++)
                    {
                        FieldInfo field = fields[fieldIndex];

                        if (field.FieldType.Name != "OpticSight")
                            continue;

                        object sight = field.GetValue(component);

                        if (sight == null)
                            continue;

                        FieldInfo lensField = sight.GetType().GetField(
                            "LensRenderer",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                        if (lensField != null)
                            return lensField.GetValue(sight) as Renderer;
                    }
                }
                catch (Exception exception)
                {
                    LogSource.LogWarning(
                        "[Scope Debug] Failed to resolve optic lens: " +
                        exception.Message);
                }
            }

            return null;
        }

        private bool TryGetActiveScopeMask(out Rect localRect)
        {
            localRect = default(Rect);

            for (int i = 0; i < _scopeOverlays.Count; i++)
            {
                ScopeOverlay overlay = _scopeOverlays[i];

                if (!IsScopeCamera(overlay.Camera))
                    continue;

                if (overlay.LensRenderer == null &&
                    Time.unscaledTime >= overlay.NextLensResolve)
                {
                    overlay.NextLensResolve = Time.unscaledTime + 1f;
                    overlay.LensRenderer = ResolveOpticLens(overlay.Camera);
                }

                Rect canvasRect;

                if (!TryProjectRendererBoundsToCanvas(
                    overlay.LensRenderer, _camera, out canvasRect))
                    continue;
                float insetX = canvasRect.width * 0.06f;
                float insetY = canvasRect.height * 0.06f;
                canvasRect.xMin += insetX;
                canvasRect.xMax -= insetX;
                canvasRect.yMin += insetY;
                canvasRect.yMax -= insetY;

                localRect = canvasRect;
                overlay.LastLensScreenRect = canvasRect;
                overlay.HasLensScreenRect = true;
                return true;
            }

            return false;
        }

        private bool TryProjectRendererBoundsToCanvas(
            Renderer renderer,
            Camera camera,
            out Rect canvasRect)
        {
            canvasRect = default(Rect);

            if (renderer == null || camera == null || _canvasRect == null)
                return false;

            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            bool found = false;

            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 point = center + Vector3.Scale(
                            extents, new Vector3(x, y, z));
                        Vector2 projected;

                        if (!TryWorldPointToCanvas(
                                camera, _canvasRect, point, out projected))
                            continue;

                        found = true;
                        minX = Mathf.Min(minX, projected.x);
                        minY = Mathf.Min(minY, projected.y);
                        maxX = Mathf.Max(maxX, projected.x);
                        maxY = Mathf.Max(maxY, projected.y);
                    }

            if (!found || maxX - minX < 10f || maxY - minY < 10f)
                return false;

            canvasRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private static bool IsInsideEllipse(Vector2 point, Rect bounds)
        {
            if (bounds.width <= 0f || bounds.height <= 0f)
                return false;

            float x = (point.x - bounds.center.x) /
                      (bounds.width * 0.5f);
            float y = (point.y - bounds.center.y) /
                      (bounds.height * 0.5f);

            return x * x + y * y <= 1f;
        }

        private bool HasValidScopeProjection(Target target)
        {
            for (int i = 0; i < _scopeOverlays.Count; i++)
            {
                ScopeOverlay overlay = _scopeOverlays[i];
                Rect scopeRect;
                if (overlay != null &&
                    IsScopeCamera(overlay.Camera) &&
                    TryGetScopeScreenRect(
                        target, overlay.Camera, out scopeRect))
                    return true;
            }
            return false;
        }

        private void RenderScopeOverlays(
            Vector3 localPosition,
            float maxDistanceSq)
        {
            for (int overlayIndex = 0;
                 overlayIndex < _scopeOverlays.Count;
                 overlayIndex++)
            {
                ScopeOverlay overlay = _scopeOverlays[overlayIndex];

                if (!IsScopeCamera(overlay.Camera) ||
                    overlay.Pass == null)
                    continue;

                overlay.Boxes.Clear();
                overlay.Lines.Clear();
                overlay.Text.Clear();

                if (overlay.LensRenderer == null &&
                    Time.unscaledTime >= overlay.NextLensResolve)
                {
                    overlay.NextLensResolve = Time.unscaledTime + 1f;
                    overlay.LensRenderer = ResolveOpticLens(overlay.Camera);
                }

                Target scopeVisibilityFocus = FindScopeVisibilityFocus(
                    overlay.Camera,
                    localPosition,
                    maxDistanceSq);
                for (int i = 0; i < _targets.Count; i++)
                {
                    Target target = _targets[i];
                    Player player = target.Player;

                    if (player == null ||
                        !target.IsAlive ||
                        target.Root == null ||
                        !ShouldShow(target))
                        continue;

                    float distanceSq =
                        (target.Root.position - localPosition).sqrMagnitude;

                    if (distanceSq > maxDistanceSq)
                        continue;

                    Rect rect;

                    if (!TryGetScopeScreenRect(
                        target, overlay.Camera, out rect))
                        continue;

                    BoneVisibility scopeVisibleBones =
                        GetScopeVisibleBones(
                            target,
                            overlay.Camera,
                            ReferenceEquals(target, scopeVisibilityFocus));
                    Color hiddenScopeColor =
                        GetRoleColor(target, true);
                    Color scopeColor =
                        scopeVisibleBones != BoneVisibility.None
                            ? target.Color
                            : hiddenScopeColor;
                    if (_showBoxes.Value)
                    {
                        overlay.Boxes.Add(new BoxCommand(
                            rect, scopeColor));
                    }
                    AddHealthBar(rect, target.HealthRatio, overlay.Lines);
                    overlay.Text.Add(new TextCommand(
                        new Vector2(rect.center.x, rect.yMax + 5f),
                        FormatTargetEspText(
                            target,
                            Mathf.Sqrt(distanceSq)),
                        scopeColor));

                    if (_showBones.Value || _showAimLines.Value)
                    {
                        AddScopeBoneEsp(
                            target,
                            overlay.Camera,
                            overlay.Lines,
                            rect.height,
                            target.Color,
                            hiddenScopeColor,
                            scopeVisibleBones);
                    }
                }

                if (_cullWorldEspInScopes == null ||
                    !_cullWorldEspInScopes.Value)
                {
                    AppendScopeLootEsp(overlay, localPosition);
                    AppendScopeExtractionEsp(overlay, localPosition);
                }

                overlay.Pass.SetGeometry(
                    overlay.Boxes,
                    overlay.Lines,
                    overlay.Text,
                    _lineThickness.Value,
                    _scopeColorBrightness.Value,
                    _fontSize.Value);

                if (_cameraDebug.Value &&
                    Time.unscaledTime >= overlay.NextDebugLog)
                {
                    overlay.NextDebugLog = Time.unscaledTime + 2f;
                    string lensBounds = overlay.HasLensScreenRect
                        ? string.Format(
                            "({0:0},{1:0},{2:0},{3:0})",
                            overlay.LastLensScreenRect.x,
                            overlay.LastLensScreenRect.y,
                            overlay.LastLensScreenRect.width,
                            overlay.LastLensScreenRect.height)
                        : "<unresolved>";

                    LogSource.LogInfo(string.Format(
                        "[Scope Debug] camera=\"{0}\" boxes={1} lines={2} " +
                        "target={3} lens={4}",
                        overlay.Camera.name,
                        overlay.Boxes.Count,
                        overlay.Lines.Count,
                        overlay.Camera.targetTexture == null
                            ? "<screen>"
                            : overlay.Camera.targetTexture.name,
                        lensBounds));
                }
            }
        }

        private Target FindScopeVisibilityFocus(
            Camera camera,
            Vector3 localPosition,
            float maxDistanceSq)
        {
            Target closest = null;
            float closestDistanceSq = float.MaxValue;
            Vector2 screenCenter = new Vector2(
                camera.pixelWidth * 0.5f,
                camera.pixelHeight * 0.5f);
            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                if (target == null ||
                    target.Player == null ||
                    !target.IsAlive ||
                    target.Root == null ||
                    !ShouldShow(target) ||
                    (target.Root.position - localPosition).sqrMagnitude >
                    maxDistanceSq)
                    continue;

                Rect rect;
                if (!TryGetScopeScreenRect(target, camera, out rect))
                    continue;

                float distanceSq =
                    (rect.center - screenCenter).sqrMagnitude;
                if (distanceSq >= closestDistanceSq)
                    continue;

                closest = target;
                closestDistanceSq = distanceSq;
            }

            return closest;
        }

        private void AppendScopeLootEsp(
            ScopeOverlay overlay,
            Vector3 localPosition)
        {
            if (_lootEspEnabled == null ||
                !_lootEspEnabled.Value ||
                overlay == null ||
                overlay.Camera == null)
                return;

            float maxDistanceSq =
                _lootEspCullDistance.Value *
                _lootEspCullDistance.Value;
            for (int i = 0; i < _lootEspEntries.Count; i++)
            {
                LootEspEntry entry = _lootEspEntries[i];
                LootItem loot = entry.Loot;
                if (entry.Clustered ||
                    !IsLooseWorldLoot(loot) ||
                    loot.Item == null)
                    continue;

                float distanceSq =
                    (loot.transform.position - localPosition).sqrMagnitude;
                if (distanceSq > maxDistanceSq)
                    continue;

                Vector2 screen;
                if (!TryWorldPointToScope(
                        overlay.Camera, entry.MarkerPosition, out screen))
                    continue;

                Color color = GetLootValueColor(
                    entry.Price,
                    entry.PriceMatch,
                    entry.IsQuestItem);
                if (_lootEspBoxes.Value)
                    AddScopeMarkerX(overlay.Lines, screen, color);

                string text = _lootEspNames.Value
                    ? (entry.Item == null ? "Loot" : entry.Item.Name)
                    : "";
                if (_lootEspDistance.Value)
                    text += (text.Length > 0 ? " | " : "") +
                            Mathf.Sqrt(distanceSq).ToString("0") + "m";
                if (_lootEspPrices.Value && entry.Price > 0f)
                    text += (text.Length > 0 ? " | " : "") +
                            FormatLootPrice(entry.Price);
                if (text.Length > 0)
                    overlay.Text.Add(new TextCommand(
                        screen + new Vector2(0f, 5f), text, color));
            }

            for (int i = 0; i < _lootEspClusters.Count; i++)
            {
                LootEspCluster cluster = _lootEspClusters[i];
                if ((cluster.Center - localPosition).sqrMagnitude >
                    _lootGroupCullDistance.Value *
                    _lootGroupCullDistance.Value)
                    continue;
                Vector2 screen;
                if (!TryWorldPointToScope(
                        overlay.Camera, cluster.Center, out screen))
                    continue;
                Color color = GetLootValueColor(
                    cluster.MaximumPrice,
                    cluster.PriceMatch,
                    cluster.HasQuestItem);
                if (_lootEspBoxes.Value)
                    AddScopeMarkerX(overlay.Lines, screen, color);
                overlay.Text.Add(new TextCommand(
                    screen + new Vector2(0f, 5f),
                    cluster.Count + " nearby items",
                    color));
            }

            if (!_lootContainerEsp.Value)
                return;
            float containerDistanceSq =
                _lootContainerCullDistance.Value *
                _lootContainerCullDistance.Value;
            Color containerColor = GetContainerEspColor();
            for (int i = 0; i < _containerEspEntries.Count; i++)
            {
                ContainerEspEntry entry = _containerEspEntries[i];
                if (entry.Container == null ||
                    !entry.Container.gameObject.activeInHierarchy ||
                    (entry.Container.transform.position - localPosition)
                        .sqrMagnitude > containerDistanceSq)
                    continue;

                Rect rect;
                Vector3 center = entry.HasWorldBounds
                    ? entry.WorldBounds.center
                    : entry.Container.transform.position;
                Vector2 screen;
                if (!TryWorldPointToScope(
                        overlay.Camera, center, out screen))
                    continue;
                Color color = entry.HasQuestItem
                    ? GetQuestItemColor()
                    : (entry.PriceMatch
                        ? GetLootEspColor(true)
                        : containerColor);
                if (_lootEspBoxes.Value &&
                    TryWorldBoundsToScopeRect(
                        overlay.Camera, entry.WorldBounds, out rect))
                    overlay.Boxes.Add(new BoxCommand(rect, color));
                float distance = Vector3.Distance(
                    entry.Container.transform.position, localPosition);
                string text = entry.Name;
                if (_lootEspDistance.Value)
                    text += " | " + distance.ToString("0") + "m";
                overlay.Text.Add(new TextCommand(
                    screen + new Vector2(0f, 5f), text, color));
            }
        }

        private void AppendScopeExtractionEsp(
            ScopeOverlay overlay,
            Vector3 localPosition)
        {
            if (_showExtractions == null ||
                !_showExtractions.Value ||
                overlay == null ||
                overlay.Camera == null)
                return;

            float maxDistanceSq = _maxDistance.Value * _maxDistance.Value;
            for (int i = 0; i < _extractionPoints.Count; i++)
            {
                ExfiltrationPoint point = _extractionPoints[i];
                if (point == null)
                    continue;
                Vector3 position = point.transform.position;
                float distanceSq = (position - localPosition).sqrMagnitude;
                if (distanceSq > maxDistanceSq)
                    continue;
                Vector2 screen;
                if (!TryWorldPointToScope(
                        overlay.Camera, position, out screen))
                    continue;
                bool usable = _usableExtractionIds.Contains(
                    point.GetInstanceID());
                overlay.Text.Add(new TextCommand(
                    screen + new Vector2(0f, 5f),
                    "[EXTRACT] " + GetExtractionName(point) + " | " +
                    Mathf.Sqrt(distanceSq).ToString("0") + "m" +
                    (usable ? " | USABLE" : ""),
                    GetExtractionColor(usable)));
            }
        }

        private static bool TryWorldPointToScope(
            Camera camera,
            Vector3 world,
            out Vector2 screen)
        {
            screen = default(Vector2);
            if (camera == null)
                return false;
            Vector3 projected = camera.WorldToScreenPoint(world);
            if (projected.z <= 0f ||
                float.IsNaN(projected.x) ||
                float.IsNaN(projected.y))
                return false;
            screen = projected;
            return true;
        }

        private static void AddScopeMarkerX(
            List<LineCommand> lines,
            Vector2 position,
            Color color)
        {
            const float radius = 3f;
            lines.Add(new LineCommand(
                position + new Vector2(-radius, -radius),
                position + new Vector2(radius, radius), color, 2.5f));
            lines.Add(new LineCommand(
                position + new Vector2(-radius, radius),
                position + new Vector2(radius, -radius), color, 2.5f));
        }

        private static bool TryWorldBoundsToScopeRect(
            Camera camera,
            Bounds bounds,
            out Rect rect)
        {
            rect = default(Rect);
            if (bounds.size == Vector3.zero)
                return false;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    for (int z = 0; z < 2; z++)
                    {
                        Vector2 point;
                        if (!TryWorldPointToScope(
                                camera,
                                new Vector3(
                                    x == 0 ? min.x : max.x,
                                    y == 0 ? min.y : max.y,
                                    z == 0 ? min.z : max.z),
                                out point))
                            return false;
                        minX = Mathf.Min(minX, point.x);
                        minY = Mathf.Min(minY, point.y);
                        maxX = Mathf.Max(maxX, point.x);
                        maxY = Mathf.Max(maxY, point.y);
                    }
            rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return rect.width >= 4f && rect.height >= 4f;
        }

        private void DestroyScopeOverlays()
        {
            for (int i = 0; i < _scopeOverlays.Count; i++)
                DestroyScopeOverlay(_scopeOverlays[i]);

            _scopeOverlays.Clear();
        }

        private static void DestroyScopeOverlay(ScopeOverlay overlay)
        {
            if (overlay != null && overlay.Pass != null)
            {
                overlay.Pass.Dispose();
                overlay.Pass = null;
            }

        }

    }
}
