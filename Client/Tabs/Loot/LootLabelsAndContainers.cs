
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void AppendLootEsp(
            Vector3 localPosition,
            Rect scopeMask,
            bool hasScopeMask,
            ref int labelIndex)
        {
            long perfStarted = PerfTimestamp();
            try
            {
                AppendLootEspCore(
                    localPosition,
                    scopeMask,
                    hasScopeMask,
                    ref labelIndex);
            }
            finally
            {
                RecordPerf(
                    perfStarted,
                    ref _perfLootTicks,
                    ref _perfLootCalls,
                    ref _perfLootMaxTicks);
            }
        }

        private void AppendLootEspCore(
            Vector3 localPosition,
            Rect scopeMask,
            bool hasScopeMask,
            ref int labelIndex)
        {
            if (!_lootEspEnabled.Value ||
                _world == null ||
                _world.LootItems == null)
            {
                _lootEspEntries.Clear();
                return;
            }

            if (_lootWorldCacheDirty)
                BeginLootEspEntryBuild(localPosition);
            AdvanceLootEspEntryBuild(localPosition);
            RefreshContainerEspEntries(localPosition);

            float maxDistanceSq =
                _lootEspCullDistance.Value *
                _lootEspCullDistance.Value;
            _screenLootLabelGroups.Clear();
            _activeScreenLootLabelGroups.Clear();
            int cullActivationBudget = 8;
            for (int i = 0; i < _lootEspEntries.Count; i++)
            {
                LootEspEntry entry = _lootEspEntries[i];
                if (entry.Clustered)
                    continue;
                LootItem loot = entry.Loot;
                if (!IsLooseWorldLoot(loot) || loot.Item == null)
                    continue;

                Vector3 position = loot.transform.position;
                float distanceSq =
                    (position - localPosition).sqrMagnitude;
                if (distanceSq > maxDistanceSq)
                {
                    entry.CullVisible = false;
                    continue;
                }
                if (!entry.CullVisible)
                {
                    if (cullActivationBudget <= 0)
                        continue;
                    entry.CullVisible = true;
                    cullActivationBudget--;
                }

                Vector2 markerPosition;
                if (!ProjectLootWorldPoint(
                        entry.MarkerPosition, out markerPosition))
                    continue;
                if (hasScopeMask &&
                    IsInsideEllipse(markerPosition, scopeMask))
                    continue;

                Color color = GetLootValueColor(
                    entry.Price,
                    entry.PriceMatch,
                    entry.IsQuestItem);
                if (_lootEspBoxes.Value)
                    AddLootMarkerX(markerPosition, color);

                if (Time.unscaledTime >= entry.NextTextUpdate)
                {
                    string updatedText = _lootEspNames.Value
                        ? (entry.Item == null
                            ? "Loot"
                            : entry.Item.Name)
                        : "";
                    if (_lootEspDistance.Value)
                        updatedText +=
                            (updatedText.Length > 0 ? " | " : "") +
                            Mathf.Sqrt(distanceSq).ToString("0") + "m";
                    if (_lootEspPrices.Value && entry.Price > 0f)
                        updatedText +=
                            (updatedText.Length > 0 ? " | " : "") +
                            FormatLootPrice(entry.Price);
                    entry.CachedText = updatedText;
                    entry.NextTextUpdate = Time.unscaledTime + 0.2f;
                }

                string text = entry.CachedText;
                if (text.Length == 0)
                    continue;
                QueueLootScreenLabel(
                    entry,
                    markerPosition,
                    color,
                    text,
                    distanceSq);
            }

            AppendLootScreenLabels(ref labelIndex);
            AppendLootClusters(
                localPosition,
                scopeMask,
                hasScopeMask,
                ref labelIndex);

            AppendContainerEsp(
                localPosition,
                scopeMask,
                hasScopeMask,
                ref labelIndex);
        }

        private void QueueLootScreenLabel(
            LootEspEntry entry,
            Vector2 position,
            Color color,
            string text,
            float distanceSq)
        {
            const float cellWidth = 64f;
            const float cellHeight = 30f;
            int cellX = Mathf.FloorToInt(position.x / cellWidth);
            int cellY = Mathf.FloorToInt(position.y / cellHeight);
            long key = ((long)cellX << 32) ^ (uint)cellY;

            ScreenLootLabelGroup group;
            if (!_screenLootLabelGroups.TryGetValue(key, out group))
            {
                int poolIndex = _activeScreenLootLabelGroups.Count;
                if (poolIndex >= _screenLootLabelGroupPool.Count)
                    _screenLootLabelGroupPool.Add(
                        new ScreenLootLabelGroup());
                group = _screenLootLabelGroupPool[poolIndex];
                group.Reset();
                _screenLootLabelGroups.Add(key, group);
                _activeScreenLootLabelGroups.Add(group);
            }

            group.PositionSum += position;
            group.NearestDistanceSq = Mathf.Min(
                group.NearestDistanceSq,
                distanceSq);
            group.Items.Add(new ScreenLootLabelItem
            {
                Text = text,
                Color = color,
                Price = entry.Price,
                IsQuestItem = entry.IsQuestItem,
                DistanceSq = distanceSq
            });
        }

        private void AppendLootScreenLabels(ref int labelIndex)
        {
            for (int i = 0;
                 i < _activeScreenLootLabelGroups.Count;
                 i++)
            {
                ScreenLootLabelGroup group =
                    _activeScreenLootLabelGroups[i];
                int count = group.Items.Count;
                if (count == 0)
                    continue;

                group.Items.Sort(CompareScreenLootLabelItems);
                Vector2 position = group.PositionSum / count;
                float layerFade =
                    GetLootLabelGroupLayerFade(group, position);
                Text label = GetLabel(labelIndex++);
                RectTransform labelRect =
                    (RectTransform)label.transform;
                label.fontSize = _lootItemFontSize.Value;
                label.supportRichText = true;
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.anchoredPosition =
                    position + new Vector2(0f, 7f);

                if (count == 1)
                {
                    ScreenLootLabelItem item = group.Items[0];
                    label.text = item.Text;
                    label.color = ApplyScreenLayerFade(
                        item.Color, layerFade);
                    labelRect.sizeDelta = new Vector2(500f, 28f);
                }
                else
                {
                    float nearestDistanceSq = float.MaxValue;
                    for (int j = 0; j < count; j++)
                    {
                        nearestDistanceSq = Mathf.Min(
                            nearestDistanceSq,
                            group.Items[j].DistanceSq);
                    }
                    int shown = Mathf.Min(4, count);
                    System.Text.StringBuilder builder =
                        new System.Text.StringBuilder(160);
                    builder.Append(count).Append(" overlapping items");
                    for (int j = 0; j < shown; j++)
                    {
                        ScreenLootLabelItem item = group.Items[j];
                        Color itemColor = item.Color;
                        if (item.DistanceSq >
                            nearestDistanceSq * 1.02f)
                        {
                            float depthRatio = Mathf.Sqrt(
                                nearestDistanceSq /
                                Mathf.Max(
                                    1f, item.DistanceSq));
                            itemColor = ApplyScreenLayerFade(
                                itemColor,
                                Mathf.Lerp(
                                    0.42f,
                                    0.7f,
                                    Mathf.Clamp01(depthRatio)));
                        }
                        builder.Append("\n<color=#")
                            .Append(ColorUtility.ToHtmlStringRGBA(
                                itemColor))
                            .Append(">")
                            .Append(SanitizeLootRichText(item.Text))
                            .Append("</color>");
                    }
                    if (count > shown)
                        builder.Append("\n+")
                            .Append(count - shown)
                            .Append(" more");
                    label.text = builder.ToString();
                    label.color = ApplyScreenLayerFade(
                        Color.white, layerFade);
                    labelRect.sizeDelta = new Vector2(
                        540f,
                        (_lootItemFontSize.Value + 3f) *
                        (shown + (count > shown ? 2 : 1)));
                }
                label.gameObject.SetActive(true);
            }
        }

        private float GetLootLabelGroupLayerFade(
            ScreenLootLabelGroup group,
            Vector2 position)
        {
            float fade = 1f;
            for (int i = 0;
                 i < _activeScreenLootLabelGroups.Count;
                 i++)
            {
                ScreenLootLabelGroup nearer =
                    _activeScreenLootLabelGroups[i];
                if (nearer == group ||
                    nearer.Items.Count == 0 ||
                    nearer.NearestDistanceSq >=
                        group.NearestDistanceSq * 0.98f)
                    continue;

                Vector2 nearerPosition =
                    nearer.PositionSum / nearer.Items.Count;
                if (Mathf.Abs(
                        nearerPosition.x - position.x) > 220f ||
                    Mathf.Abs(
                        nearerPosition.y - position.y) > 55f)
                    continue;

                float depthRatio = Mathf.Sqrt(
                    nearer.NearestDistanceSq /
                    Mathf.Max(1f, group.NearestDistanceSq));
                fade = Mathf.Min(
                    fade,
                    Mathf.Lerp(
                        0.42f,
                        0.7f,
                        Mathf.Clamp01(depthRatio)));
            }
            return fade;
        }

        private static int CompareScreenLootLabelItems(
            ScreenLootLabelItem left,
            ScreenLootLabelItem right)
        {
            int quest = right.IsQuestItem.CompareTo(
                left.IsQuestItem);
            return quest != 0
                ? quest
                : right.Price.CompareTo(left.Price);
        }

        private void RefreshContainerEspEntries(
            Vector3 localPosition)
        {
            if (!_lootContainerEsp.Value)
            {
                RecycleContainerEspEntries();
                _containerCacheBuildActive = false;
                return;
            }
            if (_lootSelectedItems.Count == 0 &&
                !_lootPriceRangeEnabled.Value)
            {
                RecycleContainerEspEntries();
                _containerCacheBuildActive = false;
                return;
            }

            if (_containerCacheDirty)
            {
                _containerCacheDirty = false;
                _containerCacheBuildActive = true;
                _containerCacheBuildCursor = 0;
                RecycleContainerEspEntries();
            }

            if (!_containerCacheBuildActive)
                return;

            const int containerBudgetPerFrame = 6;
            int stop = Mathf.Min(
                _containerCacheBuildCursor +
                    containerBudgetPerFrame,
                _lootContainers.Count);
            for (; _containerCacheBuildCursor < stop;
                 _containerCacheBuildCursor++)
            {
                LootableContainer container =
                    _lootContainers[_containerCacheBuildCursor];
                if (container == null ||
                    !container.isActiveAndEnabled ||
                    !container.gameObject.activeInHierarchy ||
                    container.ItemOwner == null)
                    continue;

                _containerMatchingNames.Clear();
                bool priceMatch = false;
                bool questItem = false;
                int matchingCount = 0;
                try
                {
                    Item rootItem =
                        container.ItemOwner.RootItem;
                    if (rootItem == null)
                        continue;

                    foreach (Item contained
                             in rootItem.GetAllItems())
                    {
                        if (contained == null ||
                            ReferenceEquals(contained, rootItem))
                            continue;

                        string id = contained.StringTemplateId;
                        LootCatalogItem catalogItem;
                        _lootCatalogItems.TryGetValue(
                            id, out catalogItem);
                        float price = GetLootPrice(
                            id,
                            catalogItem == null
                                ? 0f
                                : catalogItem.BasePrice);
                        bool itemPriceMatch =
                            IsLootPriceMatch(id, price);
                        if (!_lootSelectedItems.Contains(id) &&
                            !itemPriceMatch)
                            continue;

                        matchingCount++;
                        priceMatch |= itemPriceMatch;
                        questItem |= catalogItem != null
                            ? catalogItem.IsQuestItem
                            : contained.QuestItem;
                        if (_containerMatchingNames.Count < 8)
                        {
                            string name = catalogItem == null
                                ? contained.Name
                                : catalogItem.Name;
                            if (_lootEspPrices.Value && price > 0f)
                                name += " (" +
                                        FormatLootPrice(price) + ")";
                            _containerMatchingNames.Add(name);
                        }
                    }
                }
                catch (Exception exception)
                {
                    LogSource.LogWarning(
                        "Could not inspect loot container '" +
                        container.name + "': " +
                        exception.Message);
                    continue;
                }

                if (matchingCount == 0)
                    continue;

                string containerName = GetContainerDisplayName(
                    container);
                string contents = string.Join(
                    "\n", _containerMatchingNames);
                if (matchingCount > _containerMatchingNames.Count)
                    contents += "\n+" +
                                (matchingCount -
                                 _containerMatchingNames.Count) +
                                " more";

                int containerId = container.GetInstanceID();
                Renderer[] renderers;
                if (!_lootContainerRenderers.TryGetValue(
                    containerId, out renderers) ||
                    renderers == null)
                {
                    renderers =
                        container.GetComponentsInChildren<Renderer>(
                            false);
                    _lootContainerRenderers[containerId] =
                        renderers;
                }
                Bounds worldBounds;
                bool hasWorldBounds =
                    TryGetCachedContainerBounds(
                        container,
                        containerId,
                        renderers,
                        out worldBounds);

                ContainerEspEntry entry =
                    RentContainerEspEntry();
                entry.Container = container;
                entry.Renderers = renderers;
                entry.Name = containerName;
                entry.Contents = contents;
                entry.MatchingCount = matchingCount;
                entry.PriceMatch = priceMatch;
                entry.HasQuestItem = questItem;
                entry.WorldBounds = worldBounds;
                entry.HasWorldBounds = hasWorldBounds;
                _containerEspEntries.Add(entry);
            }

            if (_containerCacheBuildCursor >=
                _lootContainers.Count)
                _containerCacheBuildActive = false;
        }

        private ContainerEspEntry RentContainerEspEntry()
        {
            int index = _containerEspEntryPool.Count - 1;
            if (index < 0)
                return new ContainerEspEntry();

            ContainerEspEntry entry =
                _containerEspEntryPool[index];
            _containerEspEntryPool.RemoveAt(index);
            return entry;
        }

        private void RecycleContainerEspEntries()
        {
            for (int i = 0;
                 i < _containerEspEntries.Count;
                 i++)
            {
                _containerEspEntryPool.Add(
                    _containerEspEntries[i]);
            }
            _containerEspEntries.Clear();
        }

        private string GetContainerDisplayName(
            LootableContainer container)
        {
            EFT.InventoryLogic.Item root =
                container.ItemOwner == null
                    ? null
                    : container.ItemOwner.RootItem;
            LootCatalogItem catalogItem;
            if (root != null &&
                _lootCatalogItems.TryGetValue(
                    root.StringTemplateId,
                    out catalogItem))
                return catalogItem.Name;

            string name = container.ItemOwner == null
                ? null
                : container.ItemOwner.ContainerName;
            if (!string.IsNullOrWhiteSpace(name))
                return name;
            return string.IsNullOrWhiteSpace(container.name)
                ? "Container"
                : container.name;
        }

        private bool TryGetCachedContainerBounds(
            LootableContainer container,
            int containerId,
            Renderer[] renderers,
            out Bounds bounds)
        {
            if (_lootContainersWithBounds.Contains(containerId) &&
                _lootContainerBounds.TryGetValue(containerId, out bounds))
                return true;

            bounds = default(Bounds);
            bool found = false;
            Collider[] colliders =
                container.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                Bounds candidate = collider.bounds;
                if (candidate.size.sqrMagnitude < 0.0001f ||
                    candidate.extents.x > 20f ||
                    candidate.extents.y > 20f ||
                    candidate.extents.z > 20f)
                    continue;

                if (!found)
                {
                    bounds = candidate;
                    found = true;
                }
                else
                    bounds.Encapsulate(candidate);
            }

            if (!found && renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null)
                        continue;

                    if (!found)
                    {
                        bounds = renderer.bounds;
                        found = true;
                    }
                    else
                        bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!found)
                return false;

            _lootContainerBounds[containerId] = bounds;
            _lootContainersWithBounds.Add(containerId);
            return true;
        }

        private void AppendContainerEsp(
            Vector3 localPosition,
            Rect scopeMask,
            bool hasScopeMask,
            ref int labelIndex)
        {
            if (!_lootContainerEsp.Value)
                return;

            float maxDistanceSq =
                _lootContainerCullDistance.Value *
                _lootContainerCullDistance.Value;
            Color containerColor = GetContainerEspColor();

            for (int i = 0; i < _containerEspEntries.Count; i++)
            {
                ContainerEspEntry entry = _containerEspEntries[i];
                LootableContainer container = entry.Container;
                if (container == null ||
                    !container.gameObject.activeInHierarchy)
                    continue;

                float distanceSq =
                    (container.transform.position - localPosition)
                    .sqrMagnitude;
                if (distanceSq > maxDistanceSq)
                    continue;

                Rect rect;
                if (!TryGetContainerScreenRect(entry, out rect))
                    continue;
                if (hasScopeMask &&
                    IsInsideEllipse(rect.center, scopeMask))
                    continue;

                Color entryColor = entry.HasQuestItem
                    ? GetQuestItemColor()
                    : (entry.PriceMatch
                        ? GetLootEspColor(true)
                        : containerColor);
                if (_lootEspBoxes.Value)
                {
                    if (entry.HasWorldBounds)
                    {
                        AddContainerBoundsBox(
                            entry,
                            entryColor);
                    }
                    else
                    {
                        _boxes.Add(new BoxCommand(
                            rect, entryColor));
                    }
                }

                string text = entry.Name;
                if (_lootEspDistance.Value)
                    text += " | " +
                            Mathf.Sqrt(distanceSq).ToString("0") +
                            "m";
                text += "\n" + entry.Contents;

                Text label = GetLabel(labelIndex++);
                RectTransform labelRect =
                    (RectTransform)label.transform;
                label.text = text;
                label.color = entryColor;
                label.fontSize = _lootContainerFontSize.Value;
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.anchoredPosition =
                    new Vector2(rect.center.x, rect.yMax + 3f);
                labelRect.sizeDelta = new Vector2(
                    650f,
                    Mathf.Max(
                        52f,
                        (Mathf.Min(entry.MatchingCount, 8) + 2) *
                        (_lootContainerFontSize.Value + 3f)));
                label.gameObject.SetActive(true);
            }
        }

        private void AddContainerBoundsBox(
            ContainerEspEntry entry,
            Color color)
        {
            Vector2[] corners = entry.ProjectedBoundsCorners;
            if (corners == null || corners.Length < 8)
                return;
            float thickness = Mathf.Max(
                1f, _lineThickness.Value * 0.8f);

            AddContainerEdge(corners, 0, 1, color, thickness);
            AddContainerEdge(corners, 1, 5, color, thickness);
            AddContainerEdge(corners, 5, 4, color, thickness);
            AddContainerEdge(corners, 4, 0, color, thickness);
            AddContainerEdge(corners, 2, 3, color, thickness);
            AddContainerEdge(corners, 3, 7, color, thickness);
            AddContainerEdge(corners, 7, 6, color, thickness);
            AddContainerEdge(corners, 6, 2, color, thickness);
            AddContainerEdge(corners, 0, 2, color, thickness);
            AddContainerEdge(corners, 1, 3, color, thickness);
            AddContainerEdge(corners, 4, 6, color, thickness);
            AddContainerEdge(corners, 5, 7, color, thickness);
        }

        private void AddContainerEdge(
            Vector2[] corners,
            int start,
            int end,
            Color color,
            float thickness)
        {
            _lines.Add(new LineCommand(
                corners[start],
                corners[end],
                color,
                thickness));
        }

        private bool TryGetContainerScreenRect(
            ContainerEspEntry entry,
            out Rect rect)
        {
            rect = default(Rect);
            if (entry.Container == null || _camera == null)
                return false;

            if (entry.HasWorldBounds &&
                TryProjectContainerBounds(entry, out rect))
                return true;

            bool found = false;
            Renderer[] renderers = entry.Renderers;
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Rect rendererRect;
                    if (renderers[i] == null ||
                        !renderers[i].gameObject.activeInHierarchy ||
                        !TryProjectRendererBounds(
                            renderers[i], _camera, out rendererRect))
                        continue;
                    rect = !found
                        ? rendererRect
                        : Rect.MinMaxRect(
                            Mathf.Min(rect.xMin, rendererRect.xMin),
                            Mathf.Min(rect.yMin, rendererRect.yMin),
                            Mathf.Max(rect.xMax, rendererRect.xMax),
                            Mathf.Max(rect.yMax, rendererRect.yMax));
                    found = true;
                }
            }

            if (found)
                return ConvertLootScreenRectToCanvas(rect, out rect);

            Vector3 screen = _camera.WorldToScreenPoint(
                entry.Container.transform.position);
            if (screen.z <= 0f)
                return false;
            return ConvertLootScreenRectToCanvas(
                new Rect(
                    screen.x - 12f,
                    screen.y - 12f,
                    24f,
                    24f),
                out rect);
        }

        private bool TryProjectContainerBounds(
            ContainerEspEntry entry,
            out Rect rect)
        {
            rect = default(Rect);
            if (_camera == null ||
                _canvasRect == null ||
                entry == null)
                return false;

            Bounds bounds = entry.WorldBounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3[] world = entry.WorldBoundsCorners;
            world[0] = new Vector3(min.x, min.y, min.z);
            world[1] = new Vector3(min.x, min.y, max.z);
            world[2] = new Vector3(min.x, max.y, min.z);
            world[3] = new Vector3(min.x, max.y, max.z);
            world[4] = new Vector3(max.x, min.y, min.z);
            world[5] = new Vector3(max.x, min.y, max.z);
            world[6] = new Vector3(max.x, max.y, min.z);
            world[7] = new Vector3(max.x, max.y, max.z);
            Vector2[] projected = entry.ProjectedBoundsCorners;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < world.Length; i++)
            {
                Vector3 screen =
                    _camera.WorldToScreenPoint(world[i]);
                if (screen.z <= 0f ||
                    !TryScreenPointToCanvas(
                        _canvasRect,
                        screen,
                        out projected[i]))
                    return false;

                minX = Mathf.Min(minX, projected[i].x);
                minY = Mathf.Min(minY, projected[i].y);
                maxX = Mathf.Max(maxX, projected[i].x);
                maxY = Mathf.Max(maxY, projected[i].y);
            }

            rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return rect.width >= 4f && rect.height >= 4f;
        }

    }
}
