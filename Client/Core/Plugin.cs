
namespace FieldKit
{
    [BepInPlugin(
        "com.fieldkit.spt",
        "FieldKit — Developer Tools for SPT",
        "1.7.0")]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource LogSource;
        private static Plugin _instance;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<bool> _showPmc;
        private ConfigEntry<bool> _showScav;
        private ConfigEntry<bool> _showBoss;
        private ConfigEntry<bool> _showBoxes;
        private ConfigEntry<bool> _showBones;
        private ConfigEntry<bool> _showAimLines;
        private ConfigEntry<bool> _showExtractions;
        private ConfigEntry<string> _extractionColor;
        private ConfigEntry<string> _usableExtractionColor;
        private ConfigEntry<bool> _visibilityCheck;
        private ConfigEntry<bool> _cameraDebug;
        private ConfigEntry<float> _scopeColorBrightness;
        private ConfigEntry<string> _pmcVisualColor;
        private ConfigEntry<string> _scavVisualColor;
        private ConfigEntry<string> _bossVisualColor;
        private ConfigEntry<string> _pmcOccludedColor;
        private ConfigEntry<string> _scavOccludedColor;
        private ConfigEntry<string> _bossOccludedColor;
        private ConfigEntry<string> _pmcChamColor;
        private ConfigEntry<string> _scavChamColor;
        private ConfigEntry<string> _bossChamColor;
        private ConfigEntry<string> _pmcChamOccludedColor;
        private ConfigEntry<string> _scavChamOccludedColor;
        private ConfigEntry<string> _bossChamOccludedColor;
        private ConfigEntry<bool> _godMode;
        private ConfigEntry<bool> _infiniteStamina;
        private ConfigEntry<bool> _noWeight;
        private ConfigEntry<bool> _chamsEnabled;
        private ConfigEntry<bool> _chamsCharacters;
        private ConfigEntry<bool> _chamsShowPmc;
        private ConfigEntry<bool> _chamsShowScav;
        private ConfigEntry<bool> _chamsShowBoss;
        private ConfigEntry<float> _chamsMaxDistance;
        private ConfigEntry<float> _chamsOpacity;
        private ConfigEntry<bool> _chamsCorpses;
        private ConfigEntry<bool> _chamsLoot;
        private ConfigEntry<bool> _cullGrass;
        private ConfigEntry<float> _lootRenderDistance;
        private ConfigEntry<string> _chamsCorpseColor;
        private ConfigEntry<string> _chamsLootColor;
        private ConfigEntry<float> _maxDistance;
        private ConfigEntry<float> _lineThickness;
        private ConfigEntry<float> _boneThickness;
        private ConfigEntry<float> _aimLineThickness;
        private ConfigEntry<int> _fontSize;
        private ConfigEntry<string> _espFontName;
        private ConfigEntry<float> _textOutlineThickness;
        private ConfigEntry<KeyboardShortcut> _menuKey;
        private ConfigEntry<KeyboardShortcut> _espKey;
        private ConfigEntry<KeyboardShortcut> _godModeKey;
        private ConfigEntry<KeyboardShortcut> _staminaKey;
        private ConfigEntry<KeyboardShortcut> _noWeightKey;
        private ConfigEntry<KeyboardShortcut> _chamsKey;
        private bool _menuShortcutLatched;
        private bool _guiThemeRefreshRequested;

        private readonly List<Target> _targets = new List<Target>(48);
        private readonly List<BoxCommand> _boxes = new List<BoxCommand>(48);
        private readonly List<LineCommand> _lines = new List<LineCommand>(768);
        private readonly List<FilledPolygonCommand> _filledPolygons =
            new List<FilledPolygonCommand>(64);
        private readonly List<Text> _labels = new List<Text>(48);
        private readonly List<ExfiltrationPoint> _extractionPoints =
            new List<ExfiltrationPoint>(32);
        private readonly HashSet<int> _usableExtractionIds =
            new HashSet<int>();
        private float _nextExtractionRefresh;
        private readonly List<ScopeOverlay> _scopeOverlays =
            new List<ScopeOverlay>(2);
        private readonly RaycastHit[] _visibilityHits = new RaycastHit[64];
        private readonly HashSet<int> _localPlayerColliderIds =
            new HashSet<int>();
        private readonly HashSet<int> _transparentVisibilityColliderIds =
            new HashSet<int>();
        private readonly HashSet<int> _opaqueVisibilityColliderIds =
            new HashSet<int>();
        private static readonly int VisibilityMask =
            Physics.DefaultRaycastLayers &
            ~(int)LayerMaskClass.Grass &
            ~(int)LayerMaskClass.Foliage;
        private readonly List<BodyRendererDataStruct> _bodyRenderers =
            new List<BodyRendererDataStruct>(16);
        private static readonly EBodyPart[] HealthParts =
        {
            EBodyPart.Head,
            EBodyPart.Chest,
            EBodyPart.Stomach,
            EBodyPart.LeftArm,
            EBodyPart.RightArm,
            EBodyPart.LeftLeg,
            EBodyPart.RightLeg
        };
        private static readonly FieldInfo LootRenderersField =
            AccessTools.Field(
                typeof(EFT.Interactive.LootItem),
                "_renderers");
        private static readonly FieldInfo MainMenuControllerField =
            AccessTools.Field(
                typeof(TarkovApplication),
                "mainMenuControllerClass");
        private static readonly FieldInfo ItemUiInventoryControllerField =
            AccessTools.Field(
                typeof(ItemUiContext),
                "inventoryController_0");

        private GameWorld _world;
        private Player _localPlayer;
        private Camera _camera;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private BoxGraphic _boxGraphic;
        private Font _font;
        private Harmony _harmony;
        private ChamMaterialSet _pmcChamMaterials;
        private ChamMaterialSet _scavChamMaterials;
        private ChamMaterialSet _bossChamMaterials;
        private string _lastPmcChamVisible;
        private string _lastPmcChamHidden;
        private string _lastScavChamVisible;
        private string _lastScavChamHidden;
        private string _lastBossChamVisible;
        private string _lastBossChamHidden;
        private float _lastChamOpacity = -1f;
        private string _lastCorpseChamColor;
        private string _lastLootChamColor;
        private readonly Dictionary<int, WorldChamState> _worldChamStates =
            new Dictionary<int, WorldChamState>(4096);
        private readonly HashSet<int> _seenLootIds =
            new HashSet<int>();
        private readonly HashSet<int> _knownLootIds =
            new HashSet<int>();
        private readonly HashSet<int> _seenCorpseIds =
            new HashSet<int>();
        private readonly HashSet<int> _knownCorpseIds =
            new HashSet<int>();
        private readonly List<int> _staleWorldChamIds =
            new List<int>(256);
        private readonly List<VegetationManagerState>
            _vegetationManagerStates =
                new List<VegetationManagerState>(4);
        private readonly HashSet<int> _knownVegetationManagers =
            new HashSet<int>();
        private WorldChamMaterialSet[] _worldChamMaterials;
        private bool _corpseChamDiscoveryDirty = true;
        private bool _lootChamDiscoveryDirty = true;
        private bool _worldChamPassDirty = true;
        private bool _lastCorpseChamsEnabled;
        private bool _lastLootChamsEnabled;
        private float _lastLootChamDistance = -1f;
        private Vector3 _lastWorldChamPassPosition;
        private bool _hasWorldChamPassPosition;
        private float _nextVegetationManagerScan;
        private float _nextChamUpdate;
        private float _currentSkeletonThickness = 1f;
        private bool _useScopeSkeletonColor;
        private Color _scopeSkeletonVisibleColor;
        private Color _scopeSkeletonHiddenColor;
        private BoneVisibility _scopeVisibleBones;
        private bool _chamsActive;
        private Player _weightOverridePlayer;
        private bool _previousEncumberDisabled;
        private bool _weightOverrideApplied;
        private bool _wasGodMode;

        private int _lastRenderFrame = -1;
        private bool _overlayHasContent;
        private float _nextWorldRefresh;
        private bool _scopeRefreshRequested;
        private bool _shuttingDown;
        private readonly List<LootCategory> _lootRoots =
            new List<LootCategory>(32);
        private readonly Dictionary<string, LootCategory> _lootCategories =
            new Dictionary<string, LootCategory>(256);
        private readonly Dictionary<string, float> _lootPrices =
            new Dictionary<string, float>(4096);
        private HandbookClass _lootHandbook;
        private ItemFactoryClass _lootItemFactory;
        private RagFairClass _lootRagfair;
        private int _lootItemCount;
        private int _lootHandbookItemCount;
        private float _nextLootCatalogCheck;
        private bool _lootPriceRefreshRequested;
        private Vector2 _lootScroll;

    }
}
