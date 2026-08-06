
namespace FieldKit
{
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private static readonly string[] EspFontNames =
        {
            "Tarkov (Native)",
            "Segoe UI",
            "Arial",
            "Calibri",
            "Tahoma",
            "Consolas"
        };
        private void RenderEspFrame()
        {
            if (_lastRenderFrame == Time.frameCount)
                return;

            long perfStarted = PerfTimestamp();
            try
            {
                RenderEspFrameCore();
            }
            finally
            {
                RecordPerf(
                    perfStarted,
                    ref _perfEspTicks,
                    ref _perfEspCalls,
                    ref _perfEspMaxTicks);
            }
        }

        private void RenderEspFrameCore()
        {
            if (_lastRenderFrame == Time.frameCount)
                return;

            _lastRenderFrame = Time.frameCount;

            if (!_enabled.Value ||
                _world == null ||
                _localPlayer == null)
            {
                ClearOverlay();
                return;
            }

            if (!EnsureOverlay())
                return;

            if (!_canvas.enabled)
                _canvas.enabled = true;

            if (_camera == null ||
                !_camera.enabled ||
                !_camera.gameObject.activeInHierarchy)
            {
                RefreshCamera();

                if (_camera == null)
                {
                    ClearOverlay();
                    return;
                }
            }

            int overlayDisplay =
                _legacyEspProjection != null &&
                _legacyEspProjection.Value
                    ? 0
                    : _camera.targetDisplay;
            if (_canvas.targetDisplay != overlayDisplay)
                _canvas.targetDisplay = overlayDisplay;

            _boxes.Clear();
            _lines.Clear();
            _filledPolygons.Clear();

            Vector3 localPosition = _localPlayer.Transform.position;
            float maxDistanceSq = _maxDistance.Value * _maxDistance.Value;
            int labelIndex = 0;
            Rect scopeMask;
            bool hasScopeMask = TryGetActiveScopeMask(out scopeMask);

            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                Player player = target.Player;

                if (player == null ||
                    !target.IsAlive ||
                    !ShouldShow(target))
                    continue;

                if (!target.IsOnMainScreen)
                {
                    target.HasSmoothedScreenRect = false;
                    continue;
                }

                Transform root = target.Root;
                if (root == null)
                    continue;
                Vector3 position = root.position;
                float distanceSq = (position - localPosition).sqrMagnitude;

                if (distanceSq > maxDistanceSq)
                    continue;

                Rect rect;

                if (!TryGetScreenRect(target, out rect))
                {
                    target.HasSmoothedScreenRect = false;
                    continue;
                }

                rect = StabilizeReturningScreenRect(target, rect);
                if (hasScopeMask &&
                    IsInsideEllipse(rect.center, scopeMask) &&
                    HasValidScopeProjection(target))
                    continue;

                target.ScreenLayerFade = GetTargetScreenLayerFade(
                    target, rect, distanceSq, localPosition);
                Color layeredColor = ApplyScreenLayerFade(
                    target.DisplayColor,
                    target.ScreenLayerFade);
                if (_showBoxes.Value)
                    _boxes.Add(new BoxCommand(rect, layeredColor));
                AddHealthBar(
                    rect,
                    target.HealthRatio,
                    _lines,
                    target.ScreenLayerFade);

                if (_showBones.Value || _showAimLines.Value)
                    AddBoneEsp(target, rect.height);

                Text label = GetLabel(labelIndex++);
                RectTransform labelRect = (RectTransform)label.transform;

                if (Time.unscaledTime >= target.NextTextUpdate)
                {
                    target.CachedText = FormatTargetEspText(
                        target,
                        Mathf.Sqrt(distanceSq));

                    target.NextTextUpdate = Time.unscaledTime + 0.2f;
                }

                label.text = target.CachedText;
                label.color = layeredColor;
                label.fontSize = _fontSize.Value;
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.anchoredPosition =
                    new Vector2(rect.center.x, rect.yMax + 3f);
                label.gameObject.SetActive(true);
            }

            AppendLootEsp(
                localPosition,
                scopeMask,
                hasScopeMask,
                ref labelIndex);
            AppendExtractionEsp(
                localPosition,
                scopeMask,
                hasScopeMask,
                ref labelIndex);

            for (int i = labelIndex; i < _labels.Count; i++)
                _labels[i].gameObject.SetActive(false);

            _boxGraphic.SetGeometry(
                _boxes,
                _lines,
                _filledPolygons,
                _lineThickness.Value);
            _overlayHasContent =
                _boxes.Count != 0 ||
                _lines.Count != 0 ||
                _filledPolygons.Count != 0 ||
                labelIndex != 0 ||
                _scopeOverlays.Count != 0;
            for (int i = 0; i < _targets.Count; i++)
                _targets[i].ScreenLayerFade = 1f;
            RenderScopeOverlays(localPosition, maxDistanceSq);
        }

        private static Rect StabilizeReturningScreenRect(
            Target target,
            Rect current)
        {
            float now = Time.unscaledTime;
            if (target.HasSmoothedScreenRect)
            {
                target.SmoothedScreenRect = current;
                target.LastScreenRectTime = now;
                return current;
            }

            float hiddenTime = now - target.LastScreenRectTime;
            Vector2 delta =
                current.center - target.SmoothedScreenRect.center;
            bool brieflyHidden =
                target.LastScreenRectTime > 0f &&
                hiddenTime > 0f &&
                hiddenTime < 0.25f &&
                delta.sqrMagnitude < 80f * 80f;
            Rect result = current;
            if (brieflyHidden)
            {
                const float returnBlend = 0.7f;
                Rect previous = target.SmoothedScreenRect;
                result = Rect.MinMaxRect(
                    Mathf.Lerp(previous.xMin, current.xMin, returnBlend),
                    Mathf.Lerp(previous.yMin, current.yMin, returnBlend),
                    Mathf.Lerp(previous.xMax, current.xMax, returnBlend),
                    Mathf.Lerp(previous.yMax, current.yMax, returnBlend));
            }

            target.SmoothedScreenRect = current;
            target.HasSmoothedScreenRect = true;
            target.LastScreenRectTime = now;
            return result;
        }

        private static void AddHealthBar(
            Rect rect,
            float ratio,
            List<LineCommand> lines,
            float layerFade = 1f)
        {
            ratio = Mathf.Clamp01(ratio);
            float outerWidth = Mathf.Round(Mathf.Clamp(
                rect.width * 0.04f, 2f, 5f));
            float innerWidth = Mathf.Max(
                1f, Mathf.Round(outerWidth * 0.6f));
            float x = Mathf.Round(
                rect.xMin - outerWidth * 1.35f);
            Vector2 bottom = new Vector2(x, Mathf.Round(rect.yMin));
            Vector2 top = new Vector2(x, Mathf.Round(rect.yMax));
            Vector2 healthTop = Vector2.Lerp(bottom, top, ratio);
            healthTop.y = Mathf.Round(healthTop.y);
            Color healthColor = Color.Lerp(
                new Color(1f, 0.1f, 0.05f),
                new Color(0.15f, 1f, 0.2f),
                ratio);
            healthColor = ApplyScreenLayerFade(
                healthColor, layerFade);

            lines.Add(new LineCommand(
                bottom,
                top,
                ApplyScreenLayerFade(
                    new Color(0f, 0f, 0f, 0.8f),
                    layerFade),
                outerWidth));
            lines.Add(new LineCommand(
                bottom, healthTop, healthColor, innerWidth));
        }

        private float GetTargetScreenLayerFade(
            Target target,
            Rect rect,
            float distanceSq,
            Vector3 localPosition)
        {
            float fade = 1f;
            Rect labelRect = new Rect(
                rect.center.x - 125f,
                rect.yMax,
                250f,
                _fontSize.Value + 8f);

            for (int i = 0; i < _targets.Count; i++)
            {
                Target nearer = _targets[i];
                if (nearer == target ||
                    !nearer.IsAlive ||
                    !nearer.IsOnMainScreen ||
                    nearer.Root == null ||
                    !nearer.HasSmoothedScreenRect ||
                    Time.unscaledTime -
                        nearer.LastScreenRectTime > 0.1f)
                    continue;

                float nearerDistanceSq =
                    (nearer.Root.position - localPosition).sqrMagnitude;
                if (nearerDistanceSq >= distanceSq * 0.98f)
                    continue;

                Rect nearerRect = nearer.SmoothedScreenRect;
                Rect nearerLabel = new Rect(
                    nearerRect.center.x - 125f,
                    nearerRect.yMax,
                    250f,
                    _fontSize.Value + 8f);
                float boxOverlap =
                    RectOverlapRatio(rect, nearerRect);
                if (boxOverlap < 0.12f &&
                    !labelRect.Overlaps(nearerLabel))
                    continue;

                float depthRatio = Mathf.Sqrt(
                    nearerDistanceSq /
                    Mathf.Max(1f, distanceSq));
                fade = Mathf.Min(
                    fade,
                    Mathf.Lerp(
                        0.42f,
                        0.68f,
                        Mathf.Clamp01(depthRatio)));
            }

            return fade;
        }

        private static float RectOverlapRatio(
            Rect left,
            Rect right)
        {
            float width = Mathf.Max(
                0f,
                Mathf.Min(left.xMax, right.xMax) -
                Mathf.Max(left.xMin, right.xMin));
            float height = Mathf.Max(
                0f,
                Mathf.Min(left.yMax, right.yMax) -
                Mathf.Max(left.yMin, right.yMin));
            float overlap = width * height;
            float smallerArea = Mathf.Min(
                left.width * left.height,
                right.width * right.height);
            return smallerArea <= 0f
                ? 0f
                : overlap / smallerArea;
        }

        private static Color ApplyScreenLayerFade(
            Color color,
            float fade)
        {
            fade = Mathf.Clamp01(fade);
            float brightness = Mathf.Lerp(0.62f, 1f, fade);
            return new Color(
                color.r * brightness,
                color.g * brightness,
                color.b * brightness,
                color.a * fade);
        }

        private bool TryGetScreenRect(Target target, out Rect rect)
        {
            return TryGetScreenRect(
                target, _camera, _canvasRect, out rect);
        }

        private bool TryGetScreenRect(
            Target target,
            Camera camera,
            RectTransform canvasRect,
            out Rect rect)
        {
            rect = default(Rect);

            if (camera == null || canvasRect == null)
                return false;

            if (!EnsureBones(target))
                return false;

            Vector2 localMin;
            Vector2 localMax;
            if (_legacyEspProjection != null &&
                _legacyEspProjection.Value)
            {
                Rect screenRect;
                if (!TryGetCharacterScreenRect(
                        target, camera, out screenRect) ||
                    !TryScreenPointToCanvas(
                        camera, canvasRect, screenRect.min, out localMin) ||
                    !TryScreenPointToCanvas(
                        camera, canvasRect, screenRect.max, out localMax))
                    return false;
            }
            else
            {
                Vector3 headWorld =
                    target.Head.position + Vector3.up * 0.15f;
                Vector3 feetWorld =
                    (target.LeftFoot.position + target.RightFoot.position) *
                    0.5f - Vector3.up * 0.05f;
                Vector2 head;
                Vector2 feet;
                if (!TryWorldPointToCanvas(
                        camera, canvasRect, headWorld, out head) ||
                    !TryWorldPointToCanvas(
                        camera, canvasRect, feetWorld, out feet))
                    return false;

                float height = Mathf.Abs(head.y - feet.y);
                Vector3 bodySpan =
                    target.Head.position -
                    (target.LeftFoot.position +
                     target.RightFoot.position) * 0.5f;
                float horizontalSpan =
                    new Vector2(bodySpan.x, bodySpan.z).magnitude;
                if (height >= 3f &&
                    Mathf.Abs(bodySpan.y) >= horizontalSpan * 0.75f)
                {
                    float centerX = (head.x + feet.x) * 0.5f;
                    float halfWidth = height * 0.22f;
                    localMin = new Vector2(
                        centerX - halfWidth,
                        Mathf.Min(head.y, feet.y));
                    localMax = new Vector2(
                        centerX + halfWidth,
                        Mathf.Max(head.y, feet.y));
                }
                else
                {
                    float bodyLength = Vector2.Distance(head, feet);
                    if (bodyLength < 3f)
                        return false;
                    float halfThickness =
                        Mathf.Max(2f, bodyLength * 0.22f);
                    localMin = new Vector2(
                        Mathf.Min(head.x, feet.x) - halfThickness,
                        Mathf.Min(head.y, feet.y) - halfThickness);
                    localMax = new Vector2(
                        Mathf.Max(head.x, feet.x) + halfThickness,
                        Mathf.Max(head.y, feet.y) + halfThickness);
                }
            }

            rect = Rect.MinMaxRect(
                localMin.x - 2f,
                localMin.y - 2f,
                localMax.x + 2f,
                localMax.y + 2f);

            return true;
        }

        private bool TryGetScopeScreenRect(
            Target target,
            Camera camera,
            out Rect rect)
        {
            rect = default(Rect);

            if (camera == null || !EnsureBones(target))
                return false;

            Rect screenRect;
            if (!TryGetCharacterScreenRect(target, camera, out screenRect))
                return false;

            rect = Rect.MinMaxRect(
                screenRect.xMin - 2f,
                screenRect.yMin - 2f,
                screenRect.xMax + 2f,
                screenRect.yMax + 2f);
            return true;
        }

        private static bool TryGetCharacterScreenRect(
            Target target,
            Camera camera,
            out Rect rect)
        {
            rect = default(Rect);
            Vector3 head = camera.WorldToScreenPoint(
                target.Head.position + Vector3.up * 0.15f);
            Vector3 feet = camera.WorldToScreenPoint(
                (target.LeftFoot.position + target.RightFoot.position) * 0.5f -
                Vector3.up * 0.05f);

            if (head.z <= 0f || feet.z <= 0f)
                return false;

            float standingHeight = Mathf.Abs(head.y - feet.y);
            Vector3 bodySpan =
                target.Head.position -
                (target.LeftFoot.position + target.RightFoot.position) * 0.5f;
            float horizontalSpan = new Vector2(bodySpan.x, bodySpan.z).magnitude;
            if (standingHeight >= 3f &&
                Mathf.Abs(bodySpan.y) >= horizontalSpan * 0.75f)
            {
                float centerX = (head.x + feet.x) * 0.5f;
                float halfWidth = standingHeight * 0.22f;
                rect = Rect.MinMaxRect(
                    centerX - halfWidth,
                    Mathf.Min(head.y, feet.y),
                    centerX + halfWidth,
                    Mathf.Max(head.y, feet.y));
                return true;
            }

            Vector2 headPoint = new Vector2(head.x, head.y);
            Vector2 feetPoint = new Vector2(feet.x, feet.y);
            float bodyLength = Vector2.Distance(headPoint, feetPoint);
            if (bodyLength < 3f)
                return false;

            float halfThickness = Mathf.Max(2f, bodyLength * 0.22f);
            rect = Rect.MinMaxRect(
                Mathf.Min(head.x, feet.x) - halfThickness,
                Mathf.Min(head.y, feet.y) - halfThickness,
                Mathf.Max(head.x, feet.x) + halfThickness,
                Mathf.Max(head.y, feet.y) + halfThickness);
            return true;
        }

        private bool EnsureBones(Target target)
        {
            if (target.Head != null &&
                target.LeftFoot != null &&
                target.RightFoot != null)
                return true;

            if (Time.unscaledTime < target.NextBoneRefresh)
                return false;

            target.NextBoneRefresh = Time.unscaledTime + 1f;
            CacheBones(target);

            return target.Head != null &&
                   target.LeftFoot != null &&
                   target.RightFoot != null;
        }

        private static void CacheBones(Target target)
        {
            Player player = target.Player;

            if (player == null || player.PlayerBones == null)
                return;

            PlayerBones bones = player.PlayerBones;

            target.Head = Original(bones.Head);
            target.Neck = bones.Neck;
            target.Chest = Original(bones.Ribcage);
            target.Pelvis = Original(bones.Pelvis);
            target.LeftShoulder = Original(bones.LeftShoulder);
            target.RightShoulder = Original(bones.RightShoulder);
            target.LeftHip = Original(bones.LeftThigh1);
            target.RightHip = Original(bones.RightThigh1);
            target.LeftKnee = Original(bones.LeftThigh2);
            target.RightKnee = Original(bones.RightThigh2);
            target.LeftHand = bones.LeftPalm;
            target.RightHand = bones.RightPalm;

            IDictionary<string, Transform> skeleton = null;

            if (player.PlayerBody != null &&
                player.PlayerBody.SkeletonRootJoint != null)
                skeleton = player.PlayerBody.SkeletonRootJoint.Bones;

            target.LeftElbow = FindBone(
                skeleton, "HumanLForearm1", "HumanLForearm2", "LeftForearm");
            target.RightElbow = FindBone(
                skeleton, "HumanRForearm1", "HumanRForearm2", "RightForearm");
            target.LeftCalf = FindBone(
                skeleton, "HumanLCalf", "LeftCalf");
            target.RightCalf = FindBone(
                skeleton, "HumanRCalf", "RightCalf");
            target.LeftFoot = FindBone(
                skeleton, "HumanLFoot", "LeftFoot", "LFoot");
            target.RightFoot = FindBone(
                skeleton, "HumanRFoot", "RightFoot", "RFoot");

            CacheHumanoidFallbacks(target);
        }

        private static void CacheHumanoidFallbacks(Target target)
        {
            Animator[] animators =
                target.Player.GetComponentsInChildren<Animator>(true);
            Animator animator = null;

            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null && animators[i].isHuman)
                {
                    animator = animators[i];
                    break;
                }
            }

            if (animator == null)
                return;

            SetMissingBone(
                ref target.Head, animator, HumanBodyBones.Head);
            SetMissingBone(
                ref target.Neck, animator, HumanBodyBones.Neck);
            SetMissingBone(
                ref target.Chest, animator, HumanBodyBones.Chest);
            SetMissingBone(
                ref target.Chest, animator, HumanBodyBones.Spine);
            SetMissingBone(
                ref target.Pelvis, animator, HumanBodyBones.Hips);

            SetMissingBone(
                ref target.LeftShoulder, animator, HumanBodyBones.LeftUpperArm);
            SetMissingBone(
                ref target.LeftElbow, animator, HumanBodyBones.LeftLowerArm);
            SetMissingBone(
                ref target.LeftHand, animator, HumanBodyBones.LeftHand);
            SetMissingBone(
                ref target.RightShoulder, animator, HumanBodyBones.RightUpperArm);
            SetMissingBone(
                ref target.RightElbow, animator, HumanBodyBones.RightLowerArm);
            SetMissingBone(
                ref target.RightHand, animator, HumanBodyBones.RightHand);

            SetMissingBone(
                ref target.LeftHip, animator, HumanBodyBones.LeftUpperLeg);
            SetMissingBone(
                ref target.LeftKnee, animator, HumanBodyBones.LeftLowerLeg);
            SetMissingBone(
                ref target.LeftCalf, animator, HumanBodyBones.LeftLowerLeg);
            SetMissingBone(
                ref target.LeftFoot, animator, HumanBodyBones.LeftFoot);
            SetMissingBone(
                ref target.RightHip, animator, HumanBodyBones.RightUpperLeg);
            SetMissingBone(
                ref target.RightKnee, animator, HumanBodyBones.RightLowerLeg);
            SetMissingBone(
                ref target.RightCalf, animator, HumanBodyBones.RightLowerLeg);
            SetMissingBone(
                ref target.RightFoot, animator, HumanBodyBones.RightFoot);
        }

        private static void SetMissingBone(
            ref Transform target,
            Animator animator,
            HumanBodyBones bone)
        {
            if (target == null)
                target = animator.GetBoneTransform(bone);
        }

        private static Transform Original(BifacialTransform bone)
        {
            return bone == null ? null : bone.Original;
        }

        private static Transform FindBone(
            IDictionary<string, Transform> bones,
            params string[] names)
        {
            if (bones == null)
                return null;

            for (int i = 0; i < names.Length; i++)
            {
                Transform exact;

                if (bones.TryGetValue(names[i], out exact) && exact != null)
                    return exact;
            }

            foreach (KeyValuePair<string, Transform> pair in bones)
            {
                if (pair.Value == null || string.IsNullOrEmpty(pair.Key))
                    continue;

                for (int i = 0; i < names.Length; i++)
                {
                    if (pair.Key.EndsWith(
                        names[i], StringComparison.OrdinalIgnoreCase))
                        return pair.Value;
                }
            }

            return null;
        }

        private void AddBoneEsp(Target target, float projectedHeight)
        {
            _useScopeSkeletonColor = false;
            _currentSkeletonThickness =
                GetModelScaledSkeletonThickness(projectedHeight);
            AddBoneEsp(target, _camera, _canvasRect, _lines);
        }

        private float GetModelScaledSkeletonThickness(
            float projectedHeight)
        {
            float modelScale = Mathf.Clamp(
                projectedHeight / 220f,
                0.2f,
                1.25f);
            return Mathf.Max(
                0.35f,
                _boneThickness.Value * modelScale);
        }

        private void AddBoneEsp(
            Target target,
            Camera camera,
            RectTransform canvasRect,
            List<LineCommand> lines)
        {
            if (_showBones.Value)
            {
            AddBoneLine(target.Head, target.Neck, BoneColor(target, BoneVisibility.Neck),
                camera, canvasRect, lines);
            AddBoneLine(target.Neck, target.Chest, BoneColor(target, BoneVisibility.Chest),
                camera, canvasRect, lines);
            AddBoneLine(target.Chest, target.Pelvis, BoneColor(target, BoneVisibility.Pelvis),
                camera, canvasRect, lines);

            AddBoneLine(target.Chest, target.LeftShoulder, BoneColor(target, BoneVisibility.LeftShoulder),
                camera, canvasRect, lines);
            AddBoneLine(target.LeftShoulder, target.LeftElbow, BoneColor(target, BoneVisibility.LeftElbow),
                camera, canvasRect, lines);
            AddBoneLine(target.LeftElbow, target.LeftHand, BoneColor(target, BoneVisibility.LeftHand),
                camera, canvasRect, lines);

            AddBoneLine(target.Chest, target.RightShoulder, BoneColor(target, BoneVisibility.RightShoulder),
                camera, canvasRect, lines);
            AddBoneLine(target.RightShoulder, target.RightElbow, BoneColor(target, BoneVisibility.RightElbow),
                camera, canvasRect, lines);
            AddBoneLine(target.RightElbow, target.RightHand, BoneColor(target, BoneVisibility.RightHand),
                camera, canvasRect, lines);

            AddBoneLine(target.Pelvis, target.LeftHip, BoneColor(target, BoneVisibility.LeftHip),
                camera, canvasRect, lines);
            AddBoneLine(target.LeftHip, target.LeftKnee, BoneColor(target, BoneVisibility.LeftKnee),
                camera, canvasRect, lines);
            AddBoneLine(target.LeftKnee, target.LeftCalf, BoneColor(target, BoneVisibility.LeftKnee),
                camera, canvasRect, lines);
            AddBoneLine(target.LeftCalf, target.LeftFoot, BoneColor(target, BoneVisibility.LeftFoot),
                camera, canvasRect, lines);

            AddBoneLine(target.Pelvis, target.RightHip, BoneColor(target, BoneVisibility.RightHip),
                camera, canvasRect, lines);
            AddBoneLine(target.RightHip, target.RightKnee, BoneColor(target, BoneVisibility.RightKnee),
                camera, canvasRect, lines);
            AddBoneLine(target.RightKnee, target.RightCalf, BoneColor(target, BoneVisibility.RightKnee),
                camera, canvasRect, lines);
            AddBoneLine(target.RightCalf, target.RightFoot, BoneColor(target, BoneVisibility.RightFoot),
                camera, canvasRect, lines);
            AddHeadCircle(
                target,
                camera,
                canvasRect,
                lines,
                BoneColor(target, BoneVisibility.Head));
            }

            Vector3 direction = target.Player.LookDirection;

            if (_showAimLines.Value &&
                target.Head != null &&
                direction.sqrMagnitude > 0.001f)
            {
                AddWorldLine(
                    target.Head.position,
                    target.Head.position + direction.normalized * 2f,
                    Color.white, camera, canvasRect, lines,
                    _aimLineThickness.Value);
            }
        }

        private void AddBoneLine(
            Transform start,
            Transform end,
            Color color,
            Camera camera,
            RectTransform canvasRect,
            List<LineCommand> lines)
        {
            if (start != null && end != null)
                AddWorldLine(
                    start.position, end.position, color,
                    camera, canvasRect, lines,
                    _currentSkeletonThickness);
        }

        private void AddWorldLine(
            Vector3 start,
            Vector3 end,
            Color color,
            Camera camera,
            RectTransform canvasRect,
            List<LineCommand> lines,
            float thickness = 0f)
        {
            if (camera == null || canvasRect == null)
                return;

            Vector2 startLocal;
            Vector2 endLocal;
            if (!TryWorldPointToCanvas(
                    camera, canvasRect, start, out startLocal) ||
                !TryWorldPointToCanvas(
                    camera, canvasRect, end, out endLocal))
                return;

            lines.Add(new LineCommand(
                startLocal, endLocal, color, thickness));
        }

        private void AddHeadCircle(
            Target target,
            Camera camera,
            RectTransform canvasRect,
            List<LineCommand> lines,
            Color color)
        {
            if (target.Head == null || target.Neck == null)
                return;
            Vector2 center;
            Vector2 neckLocal;
            if (!TryWorldPointToCanvas(
                    camera, canvasRect,
                    target.Head.position, out center) ||
                !TryWorldPointToCanvas(
                    camera, canvasRect,
                    target.Neck.position, out neckLocal))
                return;
            AddScreenCircle(
                center,
                Mathf.Clamp(
                    Vector2.Distance(center, neckLocal) * 0.48f,
                    2.5f,
                    24f),
                color,
                lines,
                _currentSkeletonThickness);
        }

        private static void AddScreenCircle(
            Vector2 center,
            float radius,
            Color color,
            List<LineCommand> lines,
            float thickness)
        {
            const int segments = 14;
            Vector2 previous =
                center + new Vector2(radius, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 next = center + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius);
                lines.Add(new LineCommand(
                    previous, next, color, thickness));
                previous = next;
            }
        }

        private static bool TryScreenPointToCanvas(
            Camera camera,
            RectTransform canvasRect,
            Vector2 screen,
            out Vector2 local)
        {
            local = default(Vector2);
            if (canvasRect == null ||
                Screen.width <= 0 ||
                Screen.height <= 0)
                return false;

            // EFT's SSAAImpl shrinks Camera.rect to the internal DLSS render
            // ratio. WorldToScreenPoint is therefore low-resolution while a
            // screen-space overlay remains at the full output resolution.
            // This mirrors EFT's SSAA.RemapToHiRes conversion.
            if (camera != null &&
                camera.targetTexture == null &&
                camera.rect.width > 0f &&
                camera.rect.height > 0f)
            {
                screen.x /= camera.rect.width;
                screen.y /= camera.rect.height;
            }

            Rect rect = canvasRect.rect;
            local = new Vector2(
                rect.xMin + screen.x * rect.width / Screen.width,
                rect.yMin + screen.y * rect.height / Screen.height);
            return true;
        }

        private void AddScopeBoneEsp(
            Target target,
            Camera camera,
            List<LineCommand> lines,
            float projectedHeight,
            Color visibleColor,
            Color hiddenColor,
            BoneVisibility visibleBones)
        {
            _useScopeSkeletonColor = true;
            _scopeSkeletonVisibleColor = visibleColor;
            _scopeSkeletonHiddenColor = hiddenColor;
            _scopeVisibleBones = visibleBones;
            _currentSkeletonThickness =
                GetModelScaledSkeletonThickness(projectedHeight);
            if (_showBones.Value)
            {
            AddScopeBoneLine(target.Head, target.Neck, BoneColor(target, BoneVisibility.Neck), camera, lines);
            AddScopeBoneLine(target.Neck, target.Chest, BoneColor(target, BoneVisibility.Chest), camera, lines);
            AddScopeBoneLine(target.Chest, target.Pelvis, BoneColor(target, BoneVisibility.Pelvis), camera, lines);
            AddScopeBoneLine(target.Chest, target.LeftShoulder, BoneColor(target, BoneVisibility.LeftShoulder), camera, lines);
            AddScopeBoneLine(target.LeftShoulder, target.LeftElbow, BoneColor(target, BoneVisibility.LeftElbow), camera, lines);
            AddScopeBoneLine(target.LeftElbow, target.LeftHand, BoneColor(target, BoneVisibility.LeftHand), camera, lines);
            AddScopeBoneLine(target.Chest, target.RightShoulder, BoneColor(target, BoneVisibility.RightShoulder), camera, lines);
            AddScopeBoneLine(target.RightShoulder, target.RightElbow, BoneColor(target, BoneVisibility.RightElbow), camera, lines);
            AddScopeBoneLine(target.RightElbow, target.RightHand, BoneColor(target, BoneVisibility.RightHand), camera, lines);
            AddScopeBoneLine(target.Pelvis, target.LeftHip, BoneColor(target, BoneVisibility.LeftHip), camera, lines);
            AddScopeBoneLine(target.LeftHip, target.LeftKnee, BoneColor(target, BoneVisibility.LeftKnee), camera, lines);
            AddScopeBoneLine(target.LeftKnee, target.LeftCalf, BoneColor(target, BoneVisibility.LeftKnee), camera, lines);
            AddScopeBoneLine(target.LeftCalf, target.LeftFoot, BoneColor(target, BoneVisibility.LeftFoot), camera, lines);
            AddScopeBoneLine(target.Pelvis, target.RightHip, BoneColor(target, BoneVisibility.RightHip), camera, lines);
            AddScopeBoneLine(target.RightHip, target.RightKnee, BoneColor(target, BoneVisibility.RightKnee), camera, lines);
            AddScopeBoneLine(target.RightKnee, target.RightCalf, BoneColor(target, BoneVisibility.RightKnee), camera, lines);
            AddScopeBoneLine(target.RightCalf, target.RightFoot, BoneColor(target, BoneVisibility.RightFoot), camera, lines);
            AddScopeHeadCircle(
                target, camera, lines,
                BoneColor(target, BoneVisibility.Head));
            }

            Vector3 direction = target.Player.LookDirection;

            if (_showAimLines.Value &&
                target.Head != null &&
                direction.sqrMagnitude > 0.001f)
            {
                AddScopeWorldLine(
                    target.Head.position,
                    target.Head.position + direction.normalized * 2f,
                    Color.white,
                    camera,
                    lines,
                    _aimLineThickness.Value);
            }
        }

        private Color BoneColor(
            Target target,
            BoneVisibility bone)
        {
            if (_useScopeSkeletonColor)
                return (_scopeVisibleBones & bone) != 0
                    ? _scopeSkeletonVisibleColor
                    : _scopeSkeletonHiddenColor;

            Color color;
            if (!target.HasPerBoneVisibility)
                color = target.DisplayColor;
            else if ((target.VisibleBones & bone) != 0)
                color = target.Color;
            else
                color = GetRoleColor(target, true);

            return ApplyScreenLayerFade(
                color, target.ScreenLayerFade);
        }

        private void AddScopeBoneLine(
            Transform start,
            Transform end,
            Color color,
            Camera camera,
            List<LineCommand> lines)
        {
            if (start != null && end != null)
            {
                AddScopeWorldLine(
                    start.position, end.position, color, camera, lines,
                    _currentSkeletonThickness);
            }
        }

        private static void AddScopeWorldLine(
            Vector3 start,
            Vector3 end,
            Color color,
            Camera camera,
            List<LineCommand> lines,
            float thickness = 0f)
        {
            Vector3 startScreen = camera.WorldToScreenPoint(start);
            Vector3 endScreen = camera.WorldToScreenPoint(end);

            if (startScreen.z <= 0f || endScreen.z <= 0f)
                return;

            lines.Add(new LineCommand(
                startScreen, endScreen, color, thickness));
        }

        private void UpdateVisibility(
            Target target,
            bool highPriority,
            float now,
            bool requirePerBone = false)
        {
            if (!_visibilityCheck.Value)
            {
                target.HasVisibility = false;
                target.HasPerBoneVisibility = false;
                return;
            }

            if (_camera == null ||
                !EnsureBones(target) ||
                target.Chest == null)
                return;

            float playerDistance =
                (target.Chest.position - _camera.transform.position)
                .magnitude;
            bool priorityChanged =
                highPriority != target.WasHighVisibilityPriority;
            target.WasHighVisibilityPriority = highPriority;

            if (!priorityChanged &&
                now < target.NextVisibilityUpdate)
                return;

            float updateRate = highPriority
                ? 20f
                : playerDistance <= 100f
                    ? 10f
                    : 4f;
            target.NextVisibilityUpdate =
                now + 1f / updateRate +
                (target.Player.GetInstanceID() & 3) * 0.001f;

            bool visible;

            bool detailed = highPriority || requirePerBone;

            if (!detailed)
            {
                visible =
                    IsBoneVisible(target, target.Head, _camera) ||
                    IsBoneVisible(target, target.Chest, _camera);
            }
            else
            {
                BoneVisibility mask = BoneVisibility.None;
                SampleHead(target, target.Head, _camera, ref mask);
                SampleBone(target, target.Neck, BoneVisibility.Neck, ref mask);
                SampleBone(target, target.Chest, BoneVisibility.Chest, ref mask);
                SampleBone(target, target.Pelvis, BoneVisibility.Pelvis, ref mask);
                SampleBone(target, target.LeftShoulder, BoneVisibility.LeftShoulder, ref mask);
                SampleBone(target, target.LeftElbow, BoneVisibility.LeftElbow, ref mask);
                SampleBone(target, target.LeftHand, BoneVisibility.LeftHand, ref mask);
                SampleBone(target, target.RightShoulder, BoneVisibility.RightShoulder, ref mask);
                SampleBone(target, target.RightElbow, BoneVisibility.RightElbow, ref mask);
                SampleBone(target, target.RightHand, BoneVisibility.RightHand, ref mask);
                SampleBone(target, target.LeftHip, BoneVisibility.LeftHip, ref mask);
                SampleBone(target, target.LeftKnee, BoneVisibility.LeftKnee, ref mask);
                SampleBone(target, target.LeftFoot, BoneVisibility.LeftFoot, ref mask);
                SampleBone(target, target.RightHip, BoneVisibility.RightHip, ref mask);
                SampleBone(target, target.RightKnee, BoneVisibility.RightKnee, ref mask);
                SampleBone(target, target.RightFoot, BoneVisibility.RightFoot, ref mask);
                target.VisibleBones = mask;
                target.HasPerBoneVisibility = true;
                visible = mask != BoneVisibility.None;
            }

            if (!detailed)
            {
                target.HasPerBoneVisibility = false;
                target.VisibleBones = BoneVisibility.None;
            }

            target.IsVisible = visible;
            target.HasVisibility = true;
        }

        private bool TryGetMainScreenDistance(
            Target target,
            out float distance)
        {
            distance = float.MaxValue;
            bool found = false;
            AccumulateScreenDistance(target.Root, _camera, ref distance, ref found);
            AccumulateScreenDistance(target.Head, _camera, ref distance, ref found);
            AccumulateScreenDistance(target.Chest, _camera, ref distance, ref found);
            AccumulateScreenDistance(target.Pelvis, _camera, ref distance, ref found);
            AccumulateScreenDistance(target.LeftFoot, _camera, ref distance, ref found);
            AccumulateScreenDistance(target.RightFoot, _camera, ref distance, ref found);
            return found;
        }

        private static void AccumulateScreenDistance(
            Transform bone,
            Camera camera,
            ref float distance,
            ref bool found)
        {
            if (bone == null || camera == null)
                return;

            Vector3 viewport = camera.WorldToViewportPoint(bone.position);

            if (viewport.z <= 0f ||
                viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f)
                return;

            found = true;
            float next = Vector2.Distance(
                new Vector2(viewport.x, viewport.y),
                new Vector2(0.5f, 0.5f));

            if (next < distance)
                distance = next;
        }

        private void SampleBone(
            Target target,
            Transform bone,
            BoneVisibility flag,
            ref BoneVisibility mask)
        {
            SampleBone(target, bone, flag, _camera, ref mask);
        }

        private void SampleBone(
            Target target,
            Transform bone,
            BoneVisibility flag,
            Camera camera,
            ref BoneVisibility mask)
        {
            if (IsBoneVisible(target, bone, camera))
                mask |= flag;
        }

        private void SampleHead(
            Target target,
            Transform head,
            Camera camera,
            ref BoneVisibility mask)
        {
            if (head == null || camera == null)
                return;

            Vector3 center = head.position;
            Vector3 cameraRight = camera.transform.right * 0.09f;
            if (IsPointVisible(target, center, camera) ||
                IsPointVisible(target, center + Vector3.up * 0.12f, camera) ||
                IsPointVisible(target, center + cameraRight, camera) ||
                IsPointVisible(target, center - cameraRight, camera))
                mask |= BoneVisibility.Head;
        }

        private bool IsBoneVisible(
            Target target,
            Transform bone,
            Camera visibilityCamera)
        {
            return bone != null &&
                   IsPointVisible(target, bone.position, visibilityCamera);
        }

        private bool IsPointVisible(
            Target target,
            Vector3 point,
            Camera visibilityCamera)
        {
            if (visibilityCamera == null)
                return false;

            Vector3 origin = visibilityCamera.transform.position;
            Vector3 delta = point - origin;
            float distance = delta.magnitude;

            if (distance <= 0.05f)
                return true;

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                delta / distance,
                _visibilityHits,
                distance + 0.05f,
                VisibilityMask,
                QueryTriggerInteraction.Ignore);

            if (hitCount == 0)
                return true;

            float closestDistance = float.MaxValue;
            bool closestBelongsToTarget = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _visibilityHits[i].collider;

                if (collider == null)
                    continue;

                int colliderId = collider.GetInstanceID();
                if (_localPlayerColliderIds.Contains(colliderId) ||
                    IsPlayerCollider(collider, _localPlayer) ||
                    IsTransparentVisibilityCollider(
                        collider,
                        colliderId))
                    continue;

                float hitDistance = _visibilityHits[i].distance;

                if (hitDistance >= closestDistance)
                    continue;

                bool belongsToTarget =
                    target.ColliderIds.Contains(colliderId) ||
                    IsPlayerCollider(collider, target.Player);
                if (!belongsToTarget &&
                    IsAnyPlayerCollider(collider))
                    continue;

                closestDistance = hitDistance;
                closestBelongsToTarget = belongsToTarget;
            }

            return closestDistance == float.MaxValue ||
                   closestBelongsToTarget ||
                   closestDistance >= distance - 0.05f;
        }

        private BoneVisibility GetScopeVisibleBones(
            Target target,
            Camera visibilityCamera,
            bool detailed)
        {
            if (!_visibilityCheck.Value)
                return (BoneVisibility)(-1);
            if (visibilityCamera == null ||
                !EnsureBones(target))
                return BoneVisibility.None;

            int cameraId = visibilityCamera.GetInstanceID();
            float now = Time.unscaledTime;
            if (target.ScopeVisibilityCameraId == cameraId &&
                target.ScopeVisibilityDetailed == detailed &&
                now < target.NextScopeVisibilityUpdate)
                return target.ScopeVisibleBones;

            BoneVisibility mask = BoneVisibility.None;
            if (detailed)
            {
                SampleHead(target, target.Head, visibilityCamera, ref mask);
                SampleBone(target, target.Neck, BoneVisibility.Neck, visibilityCamera, ref mask);
                SampleBone(target, target.Chest, BoneVisibility.Chest, visibilityCamera, ref mask);
                SampleBone(target, target.Pelvis, BoneVisibility.Pelvis, visibilityCamera, ref mask);
                SampleBone(target, target.LeftShoulder, BoneVisibility.LeftShoulder, visibilityCamera, ref mask);
                SampleBone(target, target.LeftElbow, BoneVisibility.LeftElbow, visibilityCamera, ref mask);
                SampleBone(target, target.LeftHand, BoneVisibility.LeftHand, visibilityCamera, ref mask);
                SampleBone(target, target.RightShoulder, BoneVisibility.RightShoulder, visibilityCamera, ref mask);
                SampleBone(target, target.RightElbow, BoneVisibility.RightElbow, visibilityCamera, ref mask);
                SampleBone(target, target.RightHand, BoneVisibility.RightHand, visibilityCamera, ref mask);
                SampleBone(target, target.LeftHip, BoneVisibility.LeftHip, visibilityCamera, ref mask);
                SampleBone(target, target.LeftKnee, BoneVisibility.LeftKnee, visibilityCamera, ref mask);
                SampleBone(target, target.LeftFoot, BoneVisibility.LeftFoot, visibilityCamera, ref mask);
                SampleBone(target, target.RightHip, BoneVisibility.RightHip, visibilityCamera, ref mask);
                SampleBone(target, target.RightKnee, BoneVisibility.RightKnee, visibilityCamera, ref mask);
                SampleBone(target, target.RightFoot, BoneVisibility.RightFoot, visibilityCamera, ref mask);
            }
            else
            {
                SampleBone(target, target.Head, BoneVisibility.Head, visibilityCamera, ref mask);
                SampleBone(target, target.Chest, BoneVisibility.Chest, visibilityCamera, ref mask);
            }

            target.ScopeVisibleBones = mask;
            target.ScopeVisibilityCameraId = cameraId;
            target.ScopeVisibilityDetailed = detailed;
            target.NextScopeVisibilityUpdate =
                now + 1f / (detailed ? 15f : 5f) +
                (target.Player.GetInstanceID() & 3) * 0.001f;
            return mask;
        }

        private static bool IsPlayerCollider(
            Collider collider,
            Player player)
        {
            if (collider == null || player == null)
                return false;

            BodyPartCollider bodyPart =
                collider.GetComponentInParent<BodyPartCollider>();
            if (bodyPart != null)
            {
                IPlayer bodyPartPlayer = bodyPart.Player;
                if (ReferenceEquals(bodyPartPlayer, player) ||
                    (bodyPartPlayer != null &&
                     string.Equals(
                         bodyPartPlayer.ProfileId,
                         player.ProfileId,
                         StringComparison.Ordinal)))
                    return true;
            }

            Player colliderPlayer =
                collider.GetComponentInParent<Player>();
            if (ReferenceEquals(colliderPlayer, player))
                return true;

            Transform root = Original(player.Transform);
            Transform hit = collider.transform;

            return root != null &&
                   hit != null &&
                   (hit == root || hit.IsChildOf(root));
        }

        private static bool IsAnyPlayerCollider(Collider collider)
        {
            if (collider == null)
                return false;

            BodyPartCollider bodyPart =
                collider.GetComponentInParent<BodyPartCollider>();
            return (bodyPart != null && bodyPart.Player != null) ||
                   collider.GetComponentInParent<Player>() != null;
        }

        private bool IsTransparentVisibilityCollider(
            Collider collider,
            int colliderId)
        {
            if (_transparentVisibilityColliderIds.Contains(colliderId))
                return true;
            if (_opaqueVisibilityColliderIds.Contains(colliderId))
                return false;

            if (IsRendererlessBoxCollider(collider))
            {
                _transparentVisibilityColliderIds.Add(colliderId);
                return true;
            }

            _opaqueVisibilityColliderIds.Add(colliderId);
            return false;
        }

        private static bool IsRendererlessBoxCollider(Collider collider)
        {
            if (!(collider is BoxCollider))
                return false;

            Bounds colliderBounds = collider.bounds;
            Transform searchRoot = collider.transform;
            for (int depth = 0;
                 searchRoot != null && depth < 3;
                 depth++, searchRoot = searchRoot.parent)
            {
                Renderer[] renderers =
                    searchRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null ||
                        !renderer.enabled ||
                        !renderer.gameObject.activeInHierarchy ||
                        !renderer.bounds.Intersects(colliderBounds))
                        continue;

                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                        continue;
                    for (int j = 0; j < materials.Length; j++)
                    {
                        if (materials[j] != null)
                            return false;
                    }
                }
            }

            return true;
        }

        private bool TryWorldPointToCanvas(
            Camera camera,
            RectTransform canvasRect,
            Vector3 world,
            out Vector2 local)
        {
            local = default(Vector2);
            if (camera == null || canvasRect == null)
                return false;

            Vector3 cameraSpace =
                camera.transform.InverseTransformPoint(world);
            float safeNearPlane =
                Mathf.Max(0.01f, camera.nearClipPlane + 0.01f);
            if (cameraSpace.z <= safeNearPlane)
                return false;

            if (_legacyEspProjection != null &&
                _legacyEspProjection.Value)
            {
                Vector3 screen = camera.WorldToScreenPoint(world);
                if (!IsFiniteScreenPoint(screen) ||
                    !TryScreenPointToCanvas(
                        camera, canvasRect, screen, out local))
                    return false;
                return IsFiniteCanvasPoint(local);
            }

            Vector3 viewport = camera.WorldToViewportPoint(world);
            if (!IsFiniteScreenPoint(viewport))
                return false;

            Rect canvas = canvasRect.rect;
            local = new Vector2(
                canvas.xMin + viewport.x * canvas.width,
                canvas.yMin + viewport.y * canvas.height);
            return IsFiniteCanvasPoint(local);
        }

        private static bool IsFiniteScreenPoint(Vector3 point)
        {
            return !float.IsNaN(point.x) &&
                   !float.IsInfinity(point.x) &&
                   !float.IsNaN(point.y) &&
                   !float.IsInfinity(point.y) &&
                   !float.IsNaN(point.z) &&
                   !float.IsInfinity(point.z);
        }

        private static bool IsFiniteCanvasPoint(Vector2 point)
        {
            return !float.IsNaN(point.x) &&
                   !float.IsInfinity(point.x) &&
                   !float.IsNaN(point.y) &&
                   !float.IsInfinity(point.y);
        }

        private Color GetDisplayColor(Target target)
        {
            if (!_visibilityCheck.Value ||
                !target.HasVisibility ||
                target.IsVisible)
                return target.Color;

            return GetRoleColor(target, true);
        }

        private bool ShouldShow(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return _showPmc.Value;
                case EspKind.Scav:
                    return _showScav.Value;
                case EspKind.Boss:
                    return _showBoss.Value;
                default:
                    return false;
            }
        }

        private bool ShouldShowChams(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return _chamsShowPmc.Value;
                case EspKind.Scav:
                    return _chamsShowScav.Value;
                case EspKind.Boss:
                    return _chamsShowBoss.Value;
                default:
                    return false;
            }
        }

        private bool Classify(
            Player player,
            out EspKind kind,
            out Color color)
        {
            bool isAi =
                player.AIData != null &&
                player.AIData.IsAI;
            if (!isAi &&
                (player.Side == EPlayerSide.Bear ||
                 player.Side == EPlayerSide.Usec))
            {
                kind = EspKind.Pmc;
                color = GetVisualColor(kind);
                return true;
            }

            if (isAi || player.Side == EPlayerSide.Savage)
            {
                try
                {
                    WildSpawnType role = player.Profile.Info.Settings.Role;
                    kind = RoleKind(role);
                    color = GetVisualColor(kind);

                    return true;
                }
                catch
                {
                    kind = EspKind.Scav;
                    color = GetVisualColor(kind);
                    return true;
                }
            }

            kind = EspKind.Scav;
            color = Color.white;
            return false;
        }

        private static bool IsOrdinaryScavRole(WildSpawnType role)
        {
            switch (role)
            {
                case WildSpawnType.assault:
                case WildSpawnType.marksman:
                case WildSpawnType.cursedAssault:
                case WildSpawnType.assaultGroup:
                case WildSpawnType.crazyAssaultEvent:
                    return true;
                default:
                    return false;
            }
        }

        private void AddScopeHeadCircle(
            Target target,
            Camera camera,
            List<LineCommand> lines,
            Color color)
        {
            if (target.Head == null || target.Neck == null)
                return;
            Vector3 head = camera.WorldToScreenPoint(
                target.Head.position);
            Vector3 neck = camera.WorldToScreenPoint(
                target.Neck.position);
            if (head.z <= 0f || neck.z <= 0f)
                return;
            AddScreenCircle(
                head,
                Mathf.Clamp(
                    Vector2.Distance(
                        (Vector2)head, (Vector2)neck) * 0.48f,
                    2.5f,
                    24f),
                color,
                lines,
                _currentSkeletonThickness);
        }

        private static string FormatTargetEspText(
            Target target,
            float distance)
        {
            return target.RoleLabel + "\n" +
                   target.Name + " | " +
                   distance.ToString("0") + "m";
        }

        private void CreateOverlay()
        {
            GameObject canvasObject = new GameObject(
                "FieldKit Canvas",
                typeof(RectTransform),
                typeof(Canvas));

            DontDestroyOnLoad(canvasObject);

            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000;
            _canvas.enabled = false;
            _canvasRect = (RectTransform)canvasObject.transform;

            GameObject boxObject = new GameObject(
                "Boxes",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(BoxGraphic));

            boxObject.transform.SetParent(_canvasRect, false);

            RectTransform boxRect = (RectTransform)boxObject.transform;
            boxRect.anchorMin = Vector2.zero;
            boxRect.anchorMax = Vector2.one;
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;

            _boxGraphic = boxObject.GetComponent<BoxGraphic>();
            _boxGraphic.raycastTarget = false;

            if (_font == null)
                _font = LoadFont();
        }

        private bool EnsureOverlay()
        {
            if (_shuttingDown)
                return false;

            if (_canvas != null &&
                _canvasRect != null &&
                _boxGraphic != null)
                return true;
            _labels.Clear();
            _boxes.Clear();
            _lines.Clear();

            GameObject oldOverlay = null;

            if (_canvas != null)
                oldOverlay = _canvas.gameObject;
            else if (_canvasRect != null)
                oldOverlay = _canvasRect.gameObject;

            if (oldOverlay != null)
                Destroy(oldOverlay);

            _canvas = null;
            _canvasRect = null;
            _boxGraphic = null;

            CreateOverlay();

            return _canvas != null &&
                   _canvasRect != null &&
                   _boxGraphic != null;
        }

        private Text GetLabel(int index)
        {
            if (index < _labels.Count)
                return _labels[index];

            GameObject labelObject = new GameObject(
                "ESP Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(Outline));

            labelObject.transform.SetParent(_canvasRect, false);

            Text label = labelObject.GetComponent<Text>();
            label.font = _font;
            label.alignment = TextAnchor.LowerCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            Outline outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            float outlineSize = _textOutlineThickness.Value;
            outline.effectDistance =
                new Vector2(outlineSize, -outlineSize);
            outline.useGraphicAlpha = true;

            RectTransform rect = (RectTransform)labelObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(500f, 28f);

            _labels.Add(label);
            return label;
        }

        private Font LoadFont()
        {
            Font font = null;

            if (_espFontName != null &&
                string.Equals(
                    _espFontName.Value,
                    "Tarkov (Native)",
                    StringComparison.OrdinalIgnoreCase))
                font = FindTarkovMenuFont();

            try
            {
                if (font == null)
                {
                    font = Font.CreateDynamicFontFromOSFont(
                        _espFontName == null
                            ? "Segoe UI"
                            : _espFontName.Value,
                        16);
                }
            }
            catch { }

            if (font == null)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch { }
            }

            return font != null
                ? font
                : Font.CreateDynamicFontFromOSFont("Arial", 14);
        }

        private static int FindEspFontIndex(string fontName)
        {
            for (int i = 0; i < EspFontNames.Length; i++)
            {
                if (string.Equals(
                    EspFontNames[i],
                    fontName,
                    StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
        }

        private void OnEspFontSettingChanged(
            object sender,
            EventArgs args)
        {
            _font = LoadFont();
            for (int i = 0; i < _labels.Count; i++)
            {
                if (_labels[i] != null)
                    _labels[i].font = _font;
            }
            DestroyScopeOverlays();
        }

        private void OnEspOutlineSettingChanged(
            object sender,
            EventArgs args)
        {
            float outlineSize = _textOutlineThickness.Value;
            for (int i = 0; i < _labels.Count; i++)
            {
                Text label = _labels[i];
                if (label == null)
                    continue;
                Outline outline = label.GetComponent<Outline>();
                if (outline != null)
                    outline.effectDistance =
                        new Vector2(outlineSize, -outlineSize);
            }
        }

        private void ClearOverlay()
        {
            if (!_overlayHasContent)
            {
                if (_canvas != null && _canvas.enabled)
                    _canvas.enabled = false;
                return;
            }

            if (_boxGraphic != null)
                _boxGraphic.ClearBoxes();

            for (int i = 0; i < _scopeOverlays.Count; i++)
            {
                ScopeRenderPass pass = _scopeOverlays[i].Pass;

                if (pass != null)
                    pass.Clear();
            }

            for (int i = 0; i < _labels.Count; i++)
            {
                Text label = _labels[i];

                if (label != null)
                    label.gameObject.SetActive(false);
            }

            _overlayHasContent = false;
            if (_canvas != null)
                _canvas.enabled = false;
        }

    }
}
