namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void ApplyTargetLimbChams(Target target)
        {
            if (!EnsureBones(target))
                return;

            EnsureTargetLimbChamMeshes(target);
            UpdateTargetLimbChamMaterials(target);
        }

        private void EnsureTargetLimbChamMeshes(Target target)
        {
            if (target.LimbChamSkins.Count > 0 &&
                AreLimbChamSkinsValid(target.LimbChamSkins))
                return;

            DestroyTargetLimbChams(target);
            if (target.Player == null ||
                target.Player.PlayerBody == null)
                return;

            _bodyRenderers.Clear();
            try
            {
                target.Player.PlayerBody.GetRenderersNonAlloc(
                    _bodyRenderers);
            }
            catch
            {
                return;
            }

            for (int groupIndex = 0;
                 groupIndex < _bodyRenderers.Count;
                 groupIndex++)
            {
                SkinnedMeshRenderer renderer =
                    _bodyRenderers[groupIndex] as SkinnedMeshRenderer;
                LimbChamSkin skin =
                    BuildLimbChamSkin(target, renderer);
                if (skin != null)
                    target.LimbChamSkins.Add(skin);
            }
        }

        private LimbChamSkin BuildLimbChamSkin(
            Target target,
            SkinnedMeshRenderer renderer)
        {
            if (renderer == null ||
                renderer.sharedMesh == null ||
                renderer.bones == null ||
                renderer.bones.Length == 0)
                return null;

            Mesh original = renderer.sharedMesh;
            BoneWeight[] weights;
            try
            {
                weights = original.boneWeights;
                if (weights == null ||
                    weights.Length != original.vertexCount)
                    return null;
            }
            catch
            {
                return null;
            }

            BoneVisibility[] boneLimbs =
                new BoneVisibility[renderer.bones.Length];
            for (int i = 0; i < boneLimbs.Length; i++)
            {
                boneLimbs[i] = ClassifySkinBone(
                    target, renderer.bones[i]);
            }

            Dictionary<BoneVisibility, List<int>> triangles =
                new Dictionary<BoneVisibility, List<int>>();
            float[] limbScores = new float[16];
            try
            {
                for (int submesh = 0;
                     submesh < original.subMeshCount;
                     submesh++)
                {
                    int[] source = original.GetTriangles(submesh);
                    for (int i = 0; i + 2 < source.Length; i += 3)
                    {
                        BoneVisibility limb = ClassifyTriangle(
                            source[i],
                            source[i + 1],
                            source[i + 2],
                            weights,
                            boneLimbs,
                            limbScores);
                        List<int> destination;
                        if (!triangles.TryGetValue(
                                limb, out destination))
                        {
                            destination = new List<int>();
                            triangles.Add(limb, destination);
                        }
                        destination.Add(source[i]);
                        destination.Add(source[i + 1]);
                        destination.Add(source[i + 2]);
                    }
                }
            }
            catch
            {
                return null;
            }

            if (triangles.Count == 0)
                return null;

            Mesh instance = Object.Instantiate(original);
            instance.name =
                "FieldKit Limb Chams - " + original.name;
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.subMeshCount = triangles.Count;

            BoneVisibility[] submeshLimbs =
                new BoneVisibility[triangles.Count];
            int write = 0;
            foreach (KeyValuePair<BoneVisibility, List<int>> pair
                     in triangles)
            {
                submeshLimbs[write] = pair.Key;
                instance.SetTriangles(pair.Value, write, false);
                write++;
            }
            instance.RecalculateBounds();

            LimbChamSkin skin = new LimbChamSkin
            {
                Renderer = renderer,
                OriginalMesh = original,
                OriginalMaterials = renderer.sharedMaterials,
                InstanceMesh = instance,
                SubmeshLimbs = submeshLimbs,
                AppliedMaterials =
                    new Material[submeshLimbs.Length],
                OriginalOcclusion =
                    renderer.allowOcclusionWhenDynamic
            };

            renderer.sharedMesh = instance;
            renderer.allowOcclusionWhenDynamic = false;
            return skin;
        }

        private static BoneVisibility ClassifyTriangle(
            int vertex0,
            int vertex1,
            int vertex2,
            BoneWeight[] weights,
            BoneVisibility[] boneLimbs,
            float[] scores)
        {
            Array.Clear(scores, 0, scores.Length);
            ScoreVertex(vertex0, weights, boneLimbs, scores);
            ScoreVertex(vertex1, weights, boneLimbs, scores);
            ScoreVertex(vertex2, weights, boneLimbs, scores);

            BoneVisibility best = BoneVisibility.Chest;
            float bestScore = float.MinValue;
            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i] <= bestScore)
                    continue;
                best = (BoneVisibility)(1 << i);
                bestScore = scores[i];
            }
            return best;
        }

        private static void ScoreVertex(
            int vertex,
            BoneWeight[] weights,
            BoneVisibility[] boneLimbs,
            float[] scores)
        {
            if (vertex < 0 || vertex >= weights.Length)
                return;

            BoneWeight weight = weights[vertex];
            AddBoneScore(
                weight.boneIndex0, weight.weight0,
                boneLimbs, scores);
            AddBoneScore(
                weight.boneIndex1, weight.weight1,
                boneLimbs, scores);
            AddBoneScore(
                weight.boneIndex2, weight.weight2,
                boneLimbs, scores);
            AddBoneScore(
                weight.boneIndex3, weight.weight3,
                boneLimbs, scores);
        }

        private static void AddBoneScore(
            int boneIndex,
            float weight,
            BoneVisibility[] boneLimbs,
            float[] scores)
        {
            if (weight <= 0f ||
                boneIndex < 0 ||
                boneIndex >= boneLimbs.Length)
                return;

            BoneVisibility limb = boneLimbs[boneIndex];
            int limbIndex = 0;
            int limbValue = (int)limb;
            while (limbIndex < scores.Length - 1 &&
                   (limbValue & (1 << limbIndex)) == 0)
                limbIndex++;
            scores[limbIndex] += weight;
        }

        private static BoneVisibility ClassifySkinBone(
            Target target,
            Transform bone)
        {
            if (bone == null)
                return BoneVisibility.Chest;

            if (IsBoneUnder(bone, target.LeftHand))
                return BoneVisibility.LeftHand;
            if (IsBoneUnder(bone, target.LeftElbow))
                return BoneVisibility.LeftElbow;
            if (IsBoneUnder(bone, target.LeftShoulder))
                return BoneVisibility.LeftShoulder;
            if (IsBoneUnder(bone, target.RightHand))
                return BoneVisibility.RightHand;
            if (IsBoneUnder(bone, target.RightElbow))
                return BoneVisibility.RightElbow;
            if (IsBoneUnder(bone, target.RightShoulder))
                return BoneVisibility.RightShoulder;
            if (IsBoneUnder(bone, target.LeftFoot))
                return BoneVisibility.LeftFoot;
            if (IsBoneUnder(bone, target.LeftCalf) ||
                IsBoneUnder(bone, target.LeftKnee))
                return BoneVisibility.LeftKnee;
            if (IsBoneUnder(bone, target.LeftHip))
                return BoneVisibility.LeftHip;
            if (IsBoneUnder(bone, target.RightFoot))
                return BoneVisibility.RightFoot;
            if (IsBoneUnder(bone, target.RightCalf) ||
                IsBoneUnder(bone, target.RightKnee))
                return BoneVisibility.RightKnee;
            if (IsBoneUnder(bone, target.RightHip))
                return BoneVisibility.RightHip;
            if (IsBoneUnder(bone, target.Head))
                return BoneVisibility.Head;
            if (IsBoneUnder(bone, target.Neck))
                return BoneVisibility.Neck;
            if (IsBoneUnder(bone, target.Pelvis))
                return BoneVisibility.Pelvis;
            return BoneVisibility.Chest;
        }

        private static bool IsBoneUnder(
            Transform bone,
            Transform limbRoot)
        {
            if (bone == null || limbRoot == null)
                return false;

            for (Transform current = bone;
                 current != null;
                 current = current.parent)
            {
                if (ReferenceEquals(current, limbRoot))
                    return true;
            }
            return false;
        }

        private void UpdateTargetLimbChamMaterials(
            Target target)
        {
            EspRoleSettings role = GetRoleSettings(target.RoleKey);
            if (role != null)
                EnsureRoleChamMaterials(role);
            ChamMaterialSet materials = role != null
                ? role.ChamMaterials
                : GetChamMaterials(target.Kind);
            if (materials == null)
                return;

            for (int i = 0; i < target.LimbChamSkins.Count; i++)
            {
                LimbChamSkin skin = target.LimbChamSkins[i];
                if (skin == null || skin.Renderer == null)
                    continue;

                bool changed = false;
                for (int part = 0;
                     part < skin.SubmeshLimbs.Length;
                     part++)
                {
                    bool visible =
                        !_visibilityCheck.Value ||
                        (target.HasPerBoneVisibility
                            ? (target.VisibleBones &
                               skin.SubmeshLimbs[part]) != 0
                            : !target.HasVisibility ||
                              target.IsVisible);
                    Material material = visible
                        ? materials.Visible
                        : materials.Occluded;
                    if (skin.AppliedMaterials[part] == material)
                        continue;
                    skin.AppliedMaterials[part] = material;
                    changed = true;
                }

                if (changed)
                    skin.Renderer.sharedMaterials =
                        skin.AppliedMaterials;
            }
        }

        private static bool AreLimbChamSkinsValid(
            List<LimbChamSkin> skins)
        {
            if (skins.Count == 0)
                return false;
            for (int i = 0; i < skins.Count; i++)
            {
                LimbChamSkin skin = skins[i];
                if (skin == null ||
                    skin.Renderer == null ||
                    skin.InstanceMesh == null ||
                    skin.Renderer.sharedMesh != skin.InstanceMesh)
                    return false;
            }
            return true;
        }

        private static void DestroyTargetLimbChams(
            Target target)
        {
            if (target == null)
                return;

            for (int i = 0; i < target.LimbChamSkins.Count; i++)
            {
                LimbChamSkin skin = target.LimbChamSkins[i];
                if (skin == null)
                    continue;

                if (skin.Renderer != null &&
                    skin.Renderer.sharedMesh == skin.InstanceMesh)
                {
                    skin.Renderer.sharedMesh = skin.OriginalMesh;
                    skin.Renderer.sharedMaterials =
                        skin.OriginalMaterials;
                    skin.Renderer.allowOcclusionWhenDynamic =
                        skin.OriginalOcclusion;
                }

                if (skin.InstanceMesh != null)
                    Object.Destroy(skin.InstanceMesh);
            }
            target.LimbChamSkins.Clear();
        }
    }
}
