
namespace FieldKit
{
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private void PrintLoadedMessage()
        {
            ConsoleColor previous = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(
                    "[FieldKit] Loaded - Insert menu, Home ESP");
            }
            finally
            {
                Console.ForegroundColor = previous;
            }

            LogSource.LogInfo("FieldKit loaded.");
        }

        private static string KindName(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return "PMC";
                case EspKind.Scav:
                    return "SCAV";
                case EspKind.Boss:
                    return "BOSS";
                default:
                    return "UNKNOWN";
            }
        }

        private enum EspKind
        {
            Pmc,
            Scav,
            Boss
        }

        [Flags]
        private enum BoneVisibility
        {
            None = 0,
            Head = 1 << 0,
            Neck = 1 << 1,
            Chest = 1 << 2,
            Pelvis = 1 << 3,
            LeftShoulder = 1 << 4,
            LeftElbow = 1 << 5,
            LeftHand = 1 << 6,
            RightShoulder = 1 << 7,
            RightElbow = 1 << 8,
            RightHand = 1 << 9,
            LeftHip = 1 << 10,
            LeftKnee = 1 << 11,
            LeftFoot = 1 << 12,
            RightHip = 1 << 13,
            RightKnee = 1 << 14,
            RightFoot = 1 << 15
        }

        private sealed class LootCategory
        {
            public string Id;
            public string ParentId;
            public string Name;
            public int Order;
            public int TotalItems;
            public int SelectedItems;
            public bool Expanded;
            public readonly List<LootCategory> Children =
                new List<LootCategory>();
            public readonly List<LootCatalogItem> Items =
                new List<LootCatalogItem>();
        }

        private sealed class LootCatalogItem
        {
            public string Id;
            public string Name;
            public float BasePrice;
            public bool IsQuestItem;
            public bool CanSellOnFlea;
        }

        private sealed class Target
        {
            public Player Player;
            public Transform Root;
            public IHealthController HealthController;
            public Action<EBodyPart, float, DamageInfoStruct>
                HealthChangedHandler;
            public EspKind Kind;
            public string RoleKey;
            public string RoleLabel;
            public Color Color;
            public Color DisplayColor;
            public string Name;
            public string CachedTextPrefix;
            public string CachedText;
            public float NextTextUpdate;
            public float NextBoneRefresh;
            public float NextVisibilityUpdate;
            public float NextScreenCheck;
            public float NextRuntimeRefresh;
            public float HealthRatio;
            public bool HealthDirty;
            public bool IsAlive;
            public bool HasVisibility;
            public bool IsVisible;
            public bool IsOnMainScreen;
            public bool HasSmoothedScreenRect;
            public Rect SmoothedScreenRect;
            public float LastScreenRectTime;
            public float ScreenLayerFade = 1f;
            public bool WasHighVisibilityPriority;
            public bool ChamsActive;
            public bool HasPerBoneVisibility;
            public BoneVisibility VisibleBones;
            public BoneVisibility ScopeVisibleBones;
            public float NextScopeVisibilityUpdate;
            public int ScopeVisibilityCameraId;
            public bool ScopeVisibilityDetailed;
            public Renderer[] ChamRenderers;
            public Material[][] ChamOriginalMaterials;
            public Material[][] ChamVisibleMaterials;
            public Material[][] ChamOccludedMaterials;
            public bool[] ChamApplied;
            public bool[] ChamAppliedVisible;
            public bool[] ChamOriginalOcclusion;
            public readonly List<LimbChamSkin> LimbChamSkins =
                new List<LimbChamSkin>();
            public readonly HashSet<int> ColliderIds =
                new HashSet<int>();
            public Transform Head;
            public Transform Neck;
            public Transform Chest;
            public Transform Pelvis;
            public Transform LeftShoulder;
            public Transform LeftElbow;
            public Transform LeftHand;
            public Transform RightShoulder;
            public Transform RightElbow;
            public Transform RightHand;
            public Transform LeftHip;
            public Transform LeftKnee;
            public Transform LeftCalf;
            public Transform LeftFoot;
            public Transform RightHip;
            public Transform RightKnee;
            public Transform RightCalf;
            public Transform RightFoot;
        }

        private sealed class LimbChamSkin
        {
            public SkinnedMeshRenderer Renderer;
            public Mesh OriginalMesh;
            public Material[] OriginalMaterials;
            public Mesh InstanceMesh;
            public BoneVisibility[] SubmeshLimbs;
            public Material[] AppliedMaterials;
            public bool OriginalOcclusion;
        }

        private sealed class ScopeOverlay
        {
            public Camera Camera;
            public ScopeRenderPass Pass;
            public readonly List<BoxCommand> Boxes =
                new List<BoxCommand>(48);
            public readonly List<LineCommand> Lines =
                new List<LineCommand>(768);
            public readonly List<TextCommand> Text =
                new List<TextCommand>(48);
            public bool Seen;
            public float NextDebugLog;
            public Renderer LensRenderer;
            public float NextLensResolve;
            public Rect LastLensScreenRect;
            public bool HasLensScreenRect;
            public float LastSeenTime;
        }

        private enum WorldChamKind
        {
            Corpse,
            Loot,
            Count
        }

        private sealed class WorldChamState
        {
            public Renderer Renderer;
            public WorldChamKind Kind;
            public EFT.Interactive.LootItem LootSource;
            public Material[] OriginalMaterials;
            public Material[] ChamMaterials;
            public bool Applied;
        }

        private sealed class VegetationManagerState
        {
            public GPUInstancer.GPUInstancerDetailManager Manager;
            public bool WasEnabled;
        }

        private sealed class WorldChamMaterialSet
        {
            public readonly Material Material;

            public WorldChamMaterialSet(string name)
            {
                Material = ChamMaterialSet.CreateChamMaterial(
                    "Internal SPT World " + name,
                    CompareFunction.LessEqual);
            }

            public void Update(Color color)
            {
                Material.SetColor("_Color", color);
            }

            public void Dispose()
            {
                UnityEngine.Object.Destroy(Material);
            }
        }

        private sealed class ChamMaterialSet
        {
            public readonly Material Visible;
            public readonly Material Occluded;

            public ChamMaterialSet(string name)
            {
                Visible = CreateChamMaterial(
                    "Internal SPT " + name + " Visible",
                    CompareFunction.Always);
                Occluded = CreateChamMaterial(
                    "Internal SPT " + name + " Occluded",
                    CompareFunction.Always);
            }

            public void Update(
                Color visibleColor,
                Color occludedColor,
                float opacity)
            {
                Color visible = Color.Lerp(
                    visibleColor, Color.white, 0.2f);
                visible.a = visibleColor.a * opacity;

                Color occluded = occludedColor;
                occluded.a *= opacity;

                Visible.SetColor("_Color", visible);
                Occluded.SetColor("_Color", occluded);
            }

            public void Dispose()
            {
                UnityEngine.Object.Destroy(Visible);
                UnityEngine.Object.Destroy(Occluded);
            }

            internal static Material CreateChamMaterial(
                string name,
                CompareFunction zTest)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");

                if (shader == null)
                    throw new InvalidOperationException(
                        "Hidden/Internal-Colored shader was not found.");

                Material material = new Material(shader)
                {
                    name = name,
                    hideFlags = HideFlags.HideAndDontSave,
                    renderQueue = 3000
                };

                material.SetInt(
                    "_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt(
                    "_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_Cull", (int)CullMode.Back);
                material.SetInt("_ZWrite", 0);
                material.SetInt("_ZTest", (int)zTest);
                return material;
            }
        }
    }

    internal struct BoxCommand
    {
        public readonly Rect Rect;
        public readonly Color Color;

        public BoxCommand(Rect rect, Color color)
        {
            Rect = rect;
            Color = color;
        }
    }

    internal struct LineCommand
    {
        public readonly Vector2 Start;
        public readonly Vector2 End;
        public readonly Color Color;
        public readonly float Thickness;

        public LineCommand(
            Vector2 start,
            Vector2 end,
            Color color,
            float thickness = 0f)
        {
            Start = start;
            End = end;
            Color = color;
            Thickness = thickness;
        }
    }

    internal struct FilledPolygonCommand
    {
        public readonly IList<Vector2> Points;
        public readonly Color Color;

        public FilledPolygonCommand(
            IList<Vector2> points,
            Color color)
        {
            Points = points;
            Color = color;
        }
    }

    internal struct TextCommand
    {
        public readonly Vector2 Position;
        public readonly string Text;
        public readonly Color Color;

        public TextCommand(Vector2 position, string text, Color color)
        {
            Position = position;
            Text = text;
            Color = color;
        }
    }

    internal sealed class ScopeRenderPass
    {
        private readonly Camera _camera;
        private readonly Font _font;
        private readonly Mesh _mesh;
        private readonly Mesh _textMesh;
        private readonly Material _material;
        private readonly Material _textMaterial;
        private readonly CommandBuffer _command;
        private readonly List<Vector3> _vertices = new List<Vector3>(4096);
        private readonly List<Color> _colors = new List<Color>(4096);
        private readonly List<int> _indices = new List<int>(6144);
        private readonly List<Vector3> _textVertices =
            new List<Vector3>(1024);
        private readonly List<Vector2> _textUvs =
            new List<Vector2>(1024);
        private readonly List<Color> _textColors =
            new List<Color>(1024);
        private readonly List<int> _textIndices =
            new List<int>(1536);
        private readonly StringBuilder _glyphBuffer =
            new StringBuilder(512);
        private int _glyphHash;

        public ScopeRenderPass(Camera camera, Font font)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            _camera = camera;
            _font = font;

            Shader shader = Shader.Find("Hidden/Internal-Colored");

            if (shader == null)
                throw new InvalidOperationException(
                    "Hidden/Internal-Colored shader was not found.");

            _material = new Material(shader)
            {
                name = "FieldKit Scope Material",
                hideFlags = HideFlags.HideAndDontSave
            };

            _material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_Cull", (int)CullMode.Off);
            _material.SetInt("_ZWrite", 0);
            _material.SetInt("_ZTest", (int)CompareFunction.Always);

            _mesh = new Mesh
            {
                name = "FieldKit Scope Mesh",
                hideFlags = HideFlags.HideAndDontSave
            };
            _mesh.MarkDynamic();
            _textMesh = new Mesh
            {
                name = "FieldKit Scope Text Mesh",
                hideFlags = HideFlags.HideAndDontSave
            };
            _textMesh.MarkDynamic();
            _textMaterial = new Material(_font.material)
            {
                name = "FieldKit Scope Text Material",
                hideFlags = HideFlags.HideAndDontSave
            };

            _command = new CommandBuffer
            {
                name = "FieldKit Scope Pass"
            };

            _camera.AddCommandBuffer(CameraEvent.AfterEverything, _command);
        }

        public void SetGeometry(
            IList<BoxCommand> boxes,
            IList<LineCommand> lines,
            IList<TextCommand> text,
            float thickness,
            float colorBrightness,
            int fontSize)
        {
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();
            _textVertices.Clear();
            _textUvs.Clear();
            _textColors.Clear();
            _textIndices.Clear();

            for (int i = 0; i < boxes.Count; i++)
            {
                BoxCommand box = boxes[i];
                Rect rect = box.Rect;
                Color color = AdjustColor(
                    box.Color, colorBrightness);

                AddQuad(
                    new Rect(rect.xMin, rect.yMin, rect.width, thickness),
                    color);
                AddQuad(
                    new Rect(
                        rect.xMin,
                        rect.yMax - thickness,
                        rect.width,
                        thickness),
                    color);
                AddQuad(
                    new Rect(rect.xMin, rect.yMin, thickness, rect.height),
                    color);
                AddQuad(
                    new Rect(
                        rect.xMax - thickness,
                        rect.yMin,
                        thickness,
                        rect.height),
                    color);
            }

            for (int i = 0; i < lines.Count; i++)
            {
                LineCommand line = lines[i];
                Color color = AdjustColor(
                    line.Color, colorBrightness);
                AddLine(
                    line.Start,
                    line.End,
                    line.Thickness > 0f ? line.Thickness : thickness,
                    color);
            }

            _glyphBuffer.Length = 0;

            for (int i = 0; i < text.Count; i++)
                _glyphBuffer.Append(text[i].Text);

            int glyphHash = 17 + fontSize;

            for (int i = 0; i < _glyphBuffer.Length; i++)
            {
                unchecked
                {
                    glyphHash = glyphHash * 31 + _glyphBuffer[i];
                }
            }

            if (_glyphBuffer.Length > 0 && glyphHash != _glyphHash)
            {
                _glyphHash = glyphHash;
                _font.RequestCharactersInTexture(
                    _glyphBuffer.ToString(),
                    fontSize,
                    FontStyle.Normal);
            }

            for (int i = 0; i < text.Count; i++)
                AddText(text[i], fontSize, colorBrightness);

            _mesh.Clear(false);
            _textMesh.Clear(false);

            if (_vertices.Count == 0 && _textVertices.Count == 0)
            {
                _command.Clear();
                return;
            }

            if (_vertices.Count > 0)
            {
                _mesh.SetVertices(_vertices);
                _mesh.SetColors(_colors);
                _mesh.SetTriangles(_indices, 0, false);
                _mesh.RecalculateBounds();
            }

            if (_textVertices.Count > 0)
            {
                _textMesh.SetVertices(_textVertices);
                _textMesh.SetUVs(0, _textUvs);
                _textMesh.SetColors(_textColors);
                _textMesh.SetTriangles(_textIndices, 0, false);
                _textMesh.RecalculateBounds();
                _textMaterial.mainTexture = _font.material.mainTexture;
            }

            int width = _camera.targetTexture != null
                ? _camera.targetTexture.width
                : _camera.pixelWidth;
            int height = _camera.targetTexture != null
                ? _camera.targetTexture.height
                : _camera.pixelHeight;

            _command.Clear();
            _command.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            _command.SetViewProjectionMatrices(
                Matrix4x4.identity,
                Matrix4x4.Ortho(0f, width, 0f, height, -1f, 1f));
            if (_vertices.Count > 0)
                _command.DrawMesh(
                    _mesh, Matrix4x4.identity, _material, 0, 0);

            if (_textVertices.Count > 0)
                _command.DrawMesh(
                    _textMesh, Matrix4x4.identity, _textMaterial, 0, 0);
        }

        public bool EnsureAttached()
        {
            if (_camera == null)
                return false;

            CommandBuffer[] buffers =
                _camera.GetCommandBuffers(CameraEvent.AfterEverything);

            for (int i = 0; i < buffers.Length; i++)
            {
                if (ReferenceEquals(buffers[i], _command))
                    return false;
            }

            _camera.AddCommandBuffer(
                CameraEvent.AfterEverything, _command);
            return true;
        }

        private static Color AdjustColor(Color color, float brightness)
        {
            return new Color(
                Mathf.Clamp01(color.r * brightness),
                Mathf.Clamp01(color.g * brightness),
                Mathf.Clamp01(color.b * brightness),
                color.a);
        }

        public void Clear()
        {
            _mesh.Clear(false);
            _textMesh.Clear(false);
            _command.Clear();
        }

        public void Dispose()
        {
            if (_camera != null)
                _camera.RemoveCommandBuffer(
                    CameraEvent.AfterEverything, _command);

            _command.Release();
            UnityEngine.Object.Destroy(_mesh);
            UnityEngine.Object.Destroy(_textMesh);
            UnityEngine.Object.Destroy(_material);
            UnityEngine.Object.Destroy(_textMaterial);
        }

        private void AddText(
            TextCommand command,
            int fontSize,
            float brightness)
        {
            if (_font == null || string.IsNullOrEmpty(command.Text))
                return;

            int lineCount = 1;
            for (int i = 0; i < command.Text.Length; i++)
            {
                if (command.Text[i] == '\n')
                    lineCount++;
            }

            Color color = AdjustColor(command.Color, brightness);
            int lineStart = 0;
            int lineIndex = 0;
            for (int i = 0; i <= command.Text.Length; i++)
            {
                if (i < command.Text.Length &&
                    command.Text[i] != '\n')
                    continue;
                float y = command.Position.y +
                          (lineCount - lineIndex - 1) *
                          (fontSize + 2f);
                AddTextLine(
                    command.Text,
                    lineStart,
                    i - lineStart,
                    command.Position.x,
                    y,
                    fontSize,
                    color);
                lineStart = i + 1;
                lineIndex++;
            }
        }

        private void AddTextLine(
            string text,
            int start,
            int length,
            float centerX,
            float positionY,
            int fontSize,
            Color color)
        {
            float width = 0f;
            CharacterInfo character;

            for (int i = start; i < start + length; i++)
            {
                if (_font.GetCharacterInfo(
                    text[i],
                    out character,
                    fontSize,
                    FontStyle.Normal))
                    width += character.advance;
            }

            float cursor = centerX - width * 0.5f;

            for (int i = start; i < start + length; i++)
            {
                if (!_font.GetCharacterInfo(
                    text[i],
                    out character,
                    fontSize,
                    FontStyle.Normal))
                    continue;

                int vertex = _textVertices.Count;
                float left = cursor + character.minX;
                float right = cursor + character.maxX;
                float bottom = positionY + character.minY;
                float top = positionY + character.maxY;

                _textVertices.Add(new Vector3(left, bottom));
                _textVertices.Add(new Vector3(left, top));
                _textVertices.Add(new Vector3(right, top));
                _textVertices.Add(new Vector3(right, bottom));
                _textUvs.Add(character.uvBottomLeft);
                _textUvs.Add(character.uvTopLeft);
                _textUvs.Add(character.uvTopRight);
                _textUvs.Add(character.uvBottomRight);

                for (int j = 0; j < 4; j++)
                    _textColors.Add(color);

                _textIndices.Add(vertex);
                _textIndices.Add(vertex + 1);
                _textIndices.Add(vertex + 2);
                _textIndices.Add(vertex);
                _textIndices.Add(vertex + 2);
                _textIndices.Add(vertex + 3);
                cursor += character.advance;
            }
        }

        private void AddQuad(Rect rect, Color color)
        {
            int start = _vertices.Count;

            _vertices.Add(new Vector3(rect.xMin, rect.yMin));
            _vertices.Add(new Vector3(rect.xMin, rect.yMax));
            _vertices.Add(new Vector3(rect.xMax, rect.yMax));
            _vertices.Add(new Vector3(rect.xMax, rect.yMin));

            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);

            _indices.Add(start);
            _indices.Add(start + 1);
            _indices.Add(start + 2);
            _indices.Add(start);
            _indices.Add(start + 2);
            _indices.Add(start + 3);
        }

        private void AddLine(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color)
        {
            Vector2 delta = end - start;

            if (delta.sqrMagnitude < 0.01f)
                return;

            Vector2 normal =
                new Vector2(-delta.y, delta.x).normalized * thickness * 0.5f;
            int vertex = _vertices.Count;

            _vertices.Add(start - normal);
            _vertices.Add(start + normal);
            _vertices.Add(end + normal);
            _vertices.Add(end - normal);

            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);

            _indices.Add(vertex);
            _indices.Add(vertex + 1);
            _indices.Add(vertex + 2);
            _indices.Add(vertex);
            _indices.Add(vertex + 2);
            _indices.Add(vertex + 3);
        }
    }

    internal sealed class BoxGraphic : Graphic
    {
        private IList<BoxCommand> _boxes;
        private IList<LineCommand> _lines;
        private IList<FilledPolygonCommand> _filledPolygons;
        private float _thickness = 2f;

        public void SetGeometry(
            IList<BoxCommand> boxes,
            IList<LineCommand> lines,
            IList<FilledPolygonCommand> filledPolygons,
            float thickness)
        {
            _boxes = boxes;
            _lines = lines;
            _filledPolygons = filledPolygons;
            _thickness = thickness;
            SetVerticesDirty();
        }

        public void ClearBoxes()
        {
            _boxes = null;
            _lines = null;
            _filledPolygons = null;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_filledPolygons != null)
            {
                for (int i = 0;
                     i < _filledPolygons.Count;
                     i++)
                    AddFilledPolygon(
                        vh, _filledPolygons[i]);
            }

            if (_boxes != null)
            {
                for (int i = 0; i < _boxes.Count; i++)
                {
                    BoxCommand box = _boxes[i];
                    Rect r = box.Rect;
                    float t = _thickness;

                    AddQuad(vh, new Rect(r.xMin, r.yMin, r.width, t), box.Color);
                    AddQuad(vh, new Rect(r.xMin, r.yMax - t, r.width, t), box.Color);
                    AddQuad(vh, new Rect(r.xMin, r.yMin, t, r.height), box.Color);
                    AddQuad(vh, new Rect(r.xMax - t, r.yMin, t, r.height), box.Color);
                }
            }

            if (_lines != null)
            {
                for (int i = 0; i < _lines.Count; i++)
                {
                    LineCommand line = _lines[i];
                    AddLine(
                        vh,
                        line.Start,
                        line.End,
                        line.Thickness > 0f
                            ? line.Thickness
                            : _thickness,
                        line.Color);
                }
            }
        }

        private static void AddFilledPolygon(
            VertexHelper vh,
            FilledPolygonCommand polygon)
        {
            if (polygon.Points == null ||
                polygon.Points.Count < 3)
                return;

            int start = vh.currentVertCount;
            for (int i = 0; i < polygon.Points.Count; i++)
                vh.AddVert(
                    polygon.Points[i],
                    polygon.Color,
                    Vector2.zero);

            for (int i = 1;
                 i < polygon.Points.Count - 1;
                 i++)
                vh.AddTriangle(
                    start, start + i, start + i + 1);
        }

        private static void AddLine(
            VertexHelper vh,
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color)
        {
            Vector2 delta = end - start;

            if (delta.sqrMagnitude < 0.01f)
                return;

            Vector2 normal =
                new Vector2(-delta.y, delta.x).normalized * thickness * 0.5f;
            int vertex = vh.currentVertCount;

            vh.AddVert(start - normal, color, Vector2.zero);
            vh.AddVert(start + normal, color, Vector2.zero);
            vh.AddVert(end + normal, color, Vector2.zero);
            vh.AddVert(end - normal, color, Vector2.zero);

            vh.AddTriangle(vertex, vertex + 1, vertex + 2);
            vh.AddTriangle(vertex, vertex + 2, vertex + 3);
        }

        private static void AddQuad(
            VertexHelper vh,
            Rect rect,
            Color color)
        {
            int start = vh.currentVertCount;

            vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
