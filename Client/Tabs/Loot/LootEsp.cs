
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void BuildLootEspClusters(
            Vector3 localPosition)
        {
            _lootEspClusters.Clear();
            for (int i = 0; i < _lootEspEntries.Count; i++)
                _lootEspEntries[i].Clustered = false;

            if (!_lootProximityGrouping.Value)
                return;

            float groupingDistanceSq =
                _lootGroupingDistance.Value *
                _lootGroupingDistance.Value;
            float proximitySq =
                _lootProximityRadius.Value *
                _lootProximityRadius.Value;
            float cellSize = Mathf.Max(
                0.5f, _lootProximityRadius.Value);
            Dictionary<long, List<LootEspEntry>> spatial =
                new Dictionary<long, List<LootEspEntry>>(
                    _lootEspEntries.Count);
            for (int i = 0; i < _lootEspEntries.Count; i++)
            {
                LootEspEntry entry = _lootEspEntries[i];
                if (entry.Loot == null)
                    continue;
                Vector3 position = entry.Loot.transform.position;
                long key = LootSpatialKey(position, cellSize);
                List<LootEspEntry> bucket;
                if (!spatial.TryGetValue(key, out bucket))
                {
                    bucket = new List<LootEspEntry>(8);
                    spatial.Add(key, bucket);
                }
                bucket.Add(entry);
            }

            for (int i = 0; i < _lootEspEntries.Count; i++)
            {
                LootEspEntry seed = _lootEspEntries[i];
                if (seed.Clustered ||
                    seed.Loot == null ||
                    (seed.Loot.transform.position - localPosition)
                        .sqrMagnitude < groupingDistanceSq)
                    continue;

                List<LootEspEntry> members = null;
                Vector3 seedPosition =
                    seed.Loot.transform.position;

                int seedCellX = Mathf.FloorToInt(
                    seedPosition.x / cellSize);
                int seedCellZ = Mathf.FloorToInt(
                    seedPosition.z / cellSize);
                for (int cellX = seedCellX - 1;
                     cellX <= seedCellX + 1;
                     cellX++)
                {
                    for (int cellZ = seedCellZ - 1;
                         cellZ <= seedCellZ + 1;
                         cellZ++)
                    {
                        List<LootEspEntry> bucket;
                        if (!spatial.TryGetValue(
                                LootSpatialKey(cellX, cellZ),
                                out bucket))
                            continue;
                        for (int j = 0; j < bucket.Count; j++)
                        {
                            LootEspEntry candidate = bucket[j];
                            if (ReferenceEquals(candidate, seed) ||
                                candidate.Clustered ||
                                candidate.Loot == null ||
                                (candidate.Loot.transform.position -
                                 localPosition).sqrMagnitude <
                                groupingDistanceSq ||
                                HorizontalDistanceSquared(
                                    seedPosition,
                                    candidate.Loot.transform.position) >
                                proximitySq ||
                                Mathf.Abs(
                                    seedPosition.y -
                                    candidate.Loot.transform.position.y) >
                                _lootProximityHeight.Value)
                                continue;
                            if (members == null)
                            {
                                members =
                                    new List<LootEspEntry>(8);
                                members.Add(seed);
                            }
                            members.Add(candidate);
                        }
                    }
                }

                if (members == null)
                    continue;

                Vector3 center = Vector3.zero;
                bool priceMatch = false;
                bool questItem = false;
                float maximumPrice = 0f;
                List<string> names = new List<string>(8);
                for (int j = 0; j < members.Count; j++)
                {
                    LootEspEntry member = members[j];
                    member.Clustered = true;
                    center += member.MarkerPosition;
                    priceMatch |= member.PriceMatch;
                    questItem |= member.IsQuestItem;
                    maximumPrice = Mathf.Max(
                        maximumPrice, member.Price);
                    if (names.Count < 8)
                    {
                        string name = member.Item == null
                            ? "Loot"
                            : member.Item.Name;
                        if (_lootEspPrices.Value &&
                            member.Price > 0f)
                            name += " (" +
                                    FormatLootPrice(
                                        member.Price) + ")";
                        Color itemColor = GetLootValueColor(
                            member.Price,
                            member.PriceMatch,
                            member.IsQuestItem);
                        names.Add(
                            "<color=#" +
                            ColorUtility.ToHtmlStringRGB(
                                itemColor) +
                            ">" +
                            SanitizeLootRichText(name) +
                            "</color>");
                    }
                }
                center /= members.Count;

                List<Vector3> itemPositions =
                    new List<Vector3>(members.Count);
                List<Color> itemColors =
                    new List<Color>(members.Count);
                float minimumY = float.MaxValue;
                float maximumY = float.MinValue;
                for (int j = 0; j < members.Count; j++)
                {
                    Vector3 itemPosition =
                        members[j].MarkerPosition;
                    itemPositions.Add(itemPosition);
                    itemColors.Add(GetLootValueColor(
                        members[j].Price,
                        members[j].PriceMatch,
                        members[j].IsQuestItem));
                    minimumY = Mathf.Min(
                        minimumY, itemPosition.y);
                    maximumY = Mathf.Max(
                        maximumY, itemPosition.y);
                }
                List<Vector3> hull =
                    BuildLootClusterHull(
                        itemPositions, center.y);

                string contents = string.Join("\n", names);
                if (members.Count > names.Count)
                    contents += "\n+" +
                                (members.Count - names.Count) +
                                " more";
                _lootEspClusters.Add(new LootEspCluster
                {
                    Center = center,
                    Hull = hull,
                    ItemPositions = itemPositions,
                    ItemColors = itemColors,
                    MinimumY = minimumY - 0.25f,
                    MaximumY = maximumY + 0.25f,
                    Count = members.Count,
                    Contents = contents,
                    PriceMatch = priceMatch,
                    HasQuestItem = questItem,
                    MaximumPrice = maximumPrice
                });
            }
        }

        private static float HorizontalDistanceSquared(
            Vector3 left,
            Vector3 right)
        {
            float x = left.x - right.x;
            float z = left.z - right.z;
            return x * x + z * z;
        }

        private static long LootSpatialKey(
            Vector3 position,
            float cellSize)
        {
            return LootSpatialKey(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.z / cellSize));
        }

        private static long LootSpatialKey(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }

        private static List<Vector3> BuildLootClusterHull(
            List<Vector3> itemPositions,
            float planeY)
        {
            const float padding = 0.3f;
            List<Vector3> points = new List<Vector3>(
                itemPositions.Count * 4);
            for (int i = 0; i < itemPositions.Count; i++)
            {
                Vector3 point = itemPositions[i];
                points.Add(new Vector3(
                    point.x - padding, planeY, point.z - padding));
                points.Add(new Vector3(
                    point.x - padding, planeY, point.z + padding));
                points.Add(new Vector3(
                    point.x + padding, planeY, point.z - padding));
                points.Add(new Vector3(
                    point.x + padding, planeY, point.z + padding));
            }

            points.Sort((left, right) =>
            {
                int x = left.x.CompareTo(right.x);
                return x != 0
                    ? x
                    : left.z.CompareTo(right.z);
            });

            List<Vector3> hull =
                new List<Vector3>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                while (hull.Count >= 2 &&
                       HullCross(
                           hull[hull.Count - 2],
                           hull[hull.Count - 1],
                           points[i]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(points[i]);
            }

            int lowerCount = hull.Count;
            for (int i = points.Count - 2; i >= 0; i--)
            {
                while (hull.Count > lowerCount &&
                       HullCross(
                           hull[hull.Count - 2],
                           hull[hull.Count - 1],
                           points[i]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(points[i]);
            }

            if (hull.Count > 1)
                hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static float HullCross(
            Vector3 origin,
            Vector3 first,
            Vector3 second)
        {
            return (first.x - origin.x) *
                   (second.z - origin.z) -
                   (first.z - origin.z) *
                   (second.x - origin.x);
        }

        private void AppendLootClusters(
            Vector3 localPosition,
            Rect scopeMask,
            bool hasScopeMask,
            ref int labelIndex)
        {
            for (int i = 0; i < _lootEspClusters.Count; i++)
            {
                LootEspCluster cluster = _lootEspClusters[i];
                if ((cluster.Center - localPosition).sqrMagnitude >
                    _lootGroupCullDistance.Value *
                    _lootGroupCullDistance.Value)
                    continue;
                Vector3 centerScreen =
                    _camera.WorldToScreenPoint(cluster.Center);
                Vector2 centerLocal;
                if (centerScreen.z <= 0f ||
                    !TryScreenPointToCanvas(
                        _canvasRect,
                        new Vector2(
                            centerScreen.x,
                            centerScreen.y),
                        out centerLocal))
                {
                    AppendLootClusterFallback(
                        cluster,
                        scopeMask,
                        hasScopeMask,
                        ref labelIndex);
                    continue;
                }
                if (hasScopeMask &&
                    IsInsideEllipse(centerLocal, scopeMask))
                    continue;

                Vector2 labelPosition;
                if (!AddLootClusterFootprint(
                    cluster, out labelPosition))
                {
                    AppendLootClusterFallback(
                        cluster,
                        scopeMask,
                        hasScopeMask,
                        ref labelIndex);
                    continue;
                }
                labelPosition = centerLocal;

                Color color = GetLootValueColor(
                    cluster.MaximumPrice,
                    cluster.PriceMatch,
                    cluster.HasQuestItem);
                string text = cluster.Count + " nearby items\n" +
                              cluster.Contents;
                Text label = GetLabel(labelIndex++);
                RectTransform labelRect =
                    (RectTransform)label.transform;
                label.text = text;
                label.color = color;
                label.supportRichText = true;
                label.fontSize = _lootGroupFontSize.Value;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = labelPosition;
                labelRect.sizeDelta = new Vector2(
                    650f,
                    Mathf.Max(
                        52f,
                        (cluster.Count + 1) *
                        (_lootGroupFontSize.Value + 3f)));
                label.gameObject.SetActive(true);
            }
        }

        private void AppendLootClusterFallback(
            LootEspCluster cluster,
            Rect scopeMask,
            bool hasScopeMask,
            ref int labelIndex)
        {
            Vector2 positionSum = Vector2.zero;
            int visibleCount = 0;
            for (int i = 0; i < cluster.ItemPositions.Count; i++)
            {
                Vector2 local;
                if (!ProjectLootWorldPoint(
                        cluster.ItemPositions[i], out local) ||
                    (hasScopeMask &&
                     IsInsideEllipse(local, scopeMask)))
                    continue;

                Color itemColor =
                    i < cluster.ItemColors.Count
                        ? cluster.ItemColors[i]
                        : GetLootEspColor(false);
                AddLootMarkerX(local, itemColor);
                positionSum += local;
                visibleCount++;
            }

            if (visibleCount == 0)
                return;

            Color color = GetLootValueColor(
                cluster.MaximumPrice,
                cluster.PriceMatch,
                cluster.HasQuestItem);
            Text label = GetLabel(labelIndex++);
            RectTransform labelRect =
                (RectTransform)label.transform;
            label.text = cluster.Count + " nearby items\n" +
                         cluster.Contents;
            label.color = color;
            label.supportRichText = true;
            label.fontSize = _lootGroupFontSize.Value;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition =
                positionSum / visibleCount;
            labelRect.sizeDelta = new Vector2(
                650f,
                Mathf.Max(
                    52f,
                    (cluster.Count + 1) *
                    (_lootGroupFontSize.Value + 3f)));
            label.gameObject.SetActive(true);
        }

        private bool AddLootClusterFootprint(
            LootEspCluster cluster,
            out Vector2 labelPosition)
        {
            labelPosition = default(Vector2);
            if (_camera == null || _canvasRect == null)
                return false;

            if (cluster.Hull == null ||
                cluster.Hull.Count < 3)
                return false;

            cluster.ProjectedBottom.Clear();
            cluster.ProjectedTop.Clear();
            float left = float.MaxValue;
            float right = float.MinValue;
            float top = float.MinValue;
            Color color = GetLootValueColor(
                cluster.MaximumPrice,
                cluster.PriceMatch,
                cluster.HasQuestItem);

            for (int i = 0; i < cluster.Hull.Count; i++)
            {
                Vector3 hullPoint = cluster.Hull[i];
                Vector2 bottomPoint;
                Vector2 topPoint;
                if (!ProjectLootWorldPoint(
                        new Vector3(
                            hullPoint.x,
                            cluster.MinimumY,
                            hullPoint.z),
                        out bottomPoint) ||
                    !ProjectLootWorldPoint(
                        new Vector3(
                            hullPoint.x,
                            cluster.MaximumY,
                            hullPoint.z),
                        out topPoint))
                    return false;

                cluster.ProjectedBottom.Add(bottomPoint);
                cluster.ProjectedTop.Add(topPoint);
                left = Mathf.Min(
                    left,
                    Mathf.Min(bottomPoint.x, topPoint.x));
                right = Mathf.Max(
                    right,
                    Mathf.Max(bottomPoint.x, topPoint.x));
                top = Mathf.Max(
                    top,
                    Mathf.Max(bottomPoint.y, topPoint.y));
            }

            Color fillColor = color;
            fillColor.a *= 0.015f;
            Color outlineColor = color;
            outlineColor.a *= 0.28f;
            _filledPolygons.Add(new FilledPolygonCommand(
                cluster.ProjectedBottom, fillColor));
            _filledPolygons.Add(new FilledPolygonCommand(
                cluster.ProjectedTop, fillColor));
            for (int i = 0;
                 i < cluster.ProjectedBottom.Count;
                 i++)
            {
                int next =
                    (i + 1) % cluster.ProjectedBottom.Count;
                Vector2 bottomStart =
                    cluster.ProjectedBottom[i];
                Vector2 bottomEnd =
                    cluster.ProjectedBottom[next];
                Vector2 topStart =
                    cluster.ProjectedTop[i];
                Vector2 topEnd =
                    cluster.ProjectedTop[next];

                while (cluster.ProjectedSides.Count <= i)
                    cluster.ProjectedSides.Add(
                        new List<Vector2>(4));
                List<Vector2> projectedSide =
                    cluster.ProjectedSides[i];
                projectedSide.Clear();
                projectedSide.Add(bottomStart);
                projectedSide.Add(bottomEnd);
                projectedSide.Add(topEnd);
                projectedSide.Add(topStart);
                _filledPolygons.Add(new FilledPolygonCommand(
                    projectedSide,
                    fillColor));

                _lines.Add(new LineCommand(
                    bottomStart, bottomEnd, outlineColor, 1f));
                _lines.Add(new LineCommand(
                    topStart, topEnd, outlineColor, 1f));
                _lines.Add(new LineCommand(
                    bottomStart, topStart, outlineColor, 1f));
            }

            for (int i = 0;
                 i < cluster.ItemPositions.Count;
                 i++)
            {
                Vector3 item = cluster.ItemPositions[i];
                Vector2 local;
                if (!ProjectLootWorldPoint(
                        item, out local))
                    continue;
                Color itemColor =
                    i < cluster.ItemColors.Count
                        ? cluster.ItemColors[i]
                        : color;
                AddLootMarkerX(local, itemColor);
            }

            labelPosition = new Vector2(
                (left + right) * 0.5f,
                top + 5f);
            return true;
        }

        private static string SanitizeLootRichText(
            string value)
        {
            return string.IsNullOrEmpty(value)
                ? "Loot"
                : value.Replace("<", "‹").Replace(">", "›");
        }

        private bool ProjectLootWorldPoint(
            Vector3 world,
            out Vector2 local)
        {
            local = default(Vector2);
            Vector3 screen =
                _camera.WorldToScreenPoint(world);
            return screen.z > 0f &&
                TryScreenPointToCanvas(
                    _canvasRect,
                    new Vector2(screen.x, screen.y),
                    out local);
        }

        private static Vector3 GetLootMarkerWorldPosition(
            LootItem loot,
            List<Renderer> renderers)
        {
            if (loot == null)
                return Vector3.zero;

            Bounds combined = default(Bounds);
            bool found = false;
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Count; i++)
                {
                    Renderer renderer = renderers[i];
                    if (!IsLooseLootRenderer(loot, renderer))
                        continue;

                    if (!found)
                    {
                        combined = renderer.bounds;
                        found = true;
                    }
                    else
                        combined.Encapsulate(renderer.bounds);
                }
            }

            return found
                ? combined.center
                : loot.transform.position;
        }

        private void AddLootMarkerX(Vector2 position, Color color)
        {
            const float radius = 3f;
            _lines.Add(new LineCommand(
                position + new Vector2(-radius, -radius),
                position + new Vector2(radius, radius),
                color,
                2.5f));
            _lines.Add(new LineCommand(
                position + new Vector2(-radius, radius),
                position + new Vector2(radius, -radius),
                color,
                2.5f));
        }

        private bool ConvertLootScreenRectToCanvas(
            Rect screenRect,
            out Rect localRect)
        {
            localRect = default(Rect);
            if (_canvasRect == null)
                return false;

            Vector2 localMin;
            Vector2 localMax;
            if (!TryScreenPointToCanvas(
                    _canvasRect,
                    new Vector2(screenRect.xMin, screenRect.yMin),
                    out localMin) ||
                !TryScreenPointToCanvas(
                    _canvasRect,
                    new Vector2(screenRect.xMax, screenRect.yMax),
                    out localMax))
                return false;

            localRect = Rect.MinMaxRect(
                localMin.x,
                localMin.y,
                localMax.x,
                localMax.y);
            return true;
        }

        private sealed class LootEspEntry
        {
            public LootItem Loot;
            public LootCatalogItem Item;
            public List<Renderer> Renderers;
            public Vector3 MarkerPosition;
            public float Price;
            public bool PriceMatch;
            public string CachedText = "";
            public float NextTextUpdate;
            public bool Clustered;
            public bool IsQuestItem;
            public bool CullVisible;
        }

        private struct ScreenLootLabelItem
        {
            public string Text;
            public Color Color;
            public float Price;
            public bool IsQuestItem;
            public float DistanceSq;
        }

        private sealed class ScreenLootLabelGroup
        {
            public readonly List<ScreenLootLabelItem> Items =
                new List<ScreenLootLabelItem>(8);
            public Vector2 PositionSum;
            public float NearestDistanceSq;

            public void Reset()
            {
                Items.Clear();
                PositionSum = Vector2.zero;
                NearestDistanceSq = float.MaxValue;
            }
        }

        private sealed class LootEspCluster
        {
            public Vector3 Center;
            public List<Vector3> Hull;
            public List<Vector3> ItemPositions;
            public List<Color> ItemColors;
            public float MinimumY;
            public float MaximumY;
            public readonly List<Vector2> ProjectedBottom =
                new List<Vector2>(16);
            public readonly List<Vector2> ProjectedTop =
                new List<Vector2>(16);
            public readonly List<List<Vector2>> ProjectedSides =
                new List<List<Vector2>>(16);
            public int Count;
            public string Contents;
            public bool PriceMatch;
            public bool HasQuestItem;
            public float MaximumPrice;
        }

        private sealed class ContainerEspEntry
        {
            public LootableContainer Container;
            public Renderer[] Renderers;
            public Bounds WorldBounds;
            public bool HasWorldBounds;
            public string Name;
            public string Contents;
            public int MatchingCount;
            public bool PriceMatch;
            public bool HasQuestItem;
            public readonly Vector2[] ProjectedBoundsCorners =
                new Vector2[8];
            public readonly Vector3[] WorldBoundsCorners =
                new Vector3[8];
        }

        private static string FormatLootPrice(float price)
        {
            if (price >= 1000000f)
                return (price / 1000000f).ToString("0.##") + "m ₽";
            if (price >= 1000f)
                return (price / 1000f).ToString("0.#") + "k ₽";
            return price.ToString("N0") + " ₽";
        }

        private static void DrawDoubleEndedSlider(
            ref float minimum,
            ref float maximum,
            float rangeMinimum,
            float rangeMaximum)
        {
            Rect rect = GUILayoutUtility.GetRect(
                1f, 22f, GUILayout.ExpandWidth(true));
            Rect track = new Rect(
                rect.x + 8f, rect.center.y - 2f,
                rect.width - 16f, 4f);
            float minX = Mathf.Lerp(
                track.xMin, track.xMax,
                PriceSliderPosition(
                    minimum, rangeMinimum, rangeMaximum));
            float maxX = Mathf.Lerp(
                track.xMin, track.xMax,
                PriceSliderPosition(
                    maximum, rangeMinimum, rangeMaximum));
            int minimumId = GUIUtility.GetControlID(
                "LootPriceMinimum".GetHashCode(),
                FocusType.Passive, rect);
            int maximumId = GUIUtility.GetControlID(
                "LootPriceMaximum".GetHashCode(),
                FocusType.Passive, rect);
            Event current = Event.current;
            if (current.type == EventType.MouseDown &&
                current.button == 0 &&
                rect.Contains(current.mousePosition))
            {
                GUIUtility.hotControl =
                    Mathf.Abs(current.mousePosition.x - minX) <=
                    Mathf.Abs(current.mousePosition.x - maxX)
                        ? minimumId
                        : maximumId;
                current.Use();
            }
            if (current.type == EventType.MouseDrag &&
                (GUIUtility.hotControl == minimumId ||
                 GUIUtility.hotControl == maximumId))
            {
                float value = PriceSliderValue(
                    Mathf.InverseLerp(
                        track.xMin,
                        track.xMax,
                        current.mousePosition.x),
                    rangeMinimum,
                    rangeMaximum);
                if (GUIUtility.hotControl == minimumId)
                    minimum = Mathf.Min(value, maximum);
                else
                    maximum = Mathf.Max(value, minimum);
                current.Use();
            }
            if (current.type == EventType.MouseUp &&
                (GUIUtility.hotControl == minimumId ||
                 GUIUtility.hotControl == maximumId))
            {
                GUIUtility.hotControl = 0;
                current.Use();
            }

            Color previous = GUI.color;
            GUI.color = new Color(0.18f, 0.21f, 0.28f, 1f);
            GUI.DrawTexture(track, Texture2D.whiteTexture);
            GUI.color = new Color(0.15f, 0.78f, 0.93f, 1f);
            GUI.DrawTexture(
                Rect.MinMaxRect(
                    minX, track.yMin, maxX, track.yMax),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Box(
                new Rect(minX - 5f, rect.y + 3f, 10f, 16f),
                GUIContent.none);
            GUI.Box(
                new Rect(maxX - 5f, rect.y + 3f, 10f, 16f),
                GUIContent.none);
            GUI.color = previous;
        }

        private static float PriceSliderPosition(
            float value,
            float minimum,
            float maximum)
        {
            return Mathf.Pow(
                Mathf.InverseLerp(minimum, maximum, value),
                1f / 3f);
        }

        private static float PriceSliderValue(
            float position,
            float minimum,
            float maximum)
        {
            return Mathf.Lerp(
                minimum,
                maximum,
                position * position * position);
        }
    }
}
