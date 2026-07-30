
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private const float LootPriceMaximumLimit = 50000000f;
        private ConfigEntry<bool> _lootEspEnabled;
        private ConfigEntry<bool> _lootEspBoxes;
        private ConfigEntry<bool> _lootEspNames;
        private ConfigEntry<bool> _lootEspDistance;
        private ConfigEntry<bool> _lootEspPrices;
        private ConfigEntry<bool> _lootContainerEsp;
        private ConfigEntry<bool> _lootProximityGrouping;
        private ConfigEntry<bool> _lootValueGradient;
        private ConfigEntry<float> _lootEspCullDistance;
        private ConfigEntry<float> _lootContainerCullDistance;
        private ConfigEntry<float> _lootGroupingDistance;
        private ConfigEntry<float> _lootGroupCullDistance;
        private ConfigEntry<float> _lootProximityRadius;
        private ConfigEntry<float> _lootProximityHeight;
        private ConfigEntry<int> _lootItemFontSize;
        private ConfigEntry<int> _lootContainerFontSize;
        private ConfigEntry<int> _lootGroupFontSize;
        private ConfigEntry<string> _lootEspColor;
        private ConfigEntry<string> _lootEspPriceColor;
        private ConfigEntry<string> _lootContainerEspColor;
        private ConfigEntry<string> _lootLowValueColor;
        private ConfigEntry<string> _lootLowMidValueColor;
        private ConfigEntry<string> _lootMidValueColor;
        private ConfigEntry<string> _lootHighMidValueColor;
        private ConfigEntry<string> _lootHighValueColor;
        private ConfigEntry<string> _lootQuestItemColor;
        private ConfigEntry<bool> _lootPriceRangeEnabled;
        private ConfigEntry<bool> _lootPriceRangeSelectedOnly;
        private ConfigEntry<float> _lootPriceMinimum;
        private ConfigEntry<float> _lootPriceMaximum;
        private ConfigEntry<string> _lootSelectedItemSetting;
        private readonly HashSet<string> _lootSelectedItems =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, LootCatalogItem>
            _lootCatalogItems = new Dictionary<string, LootCatalogItem>(4096);
        private readonly List<LootEspEntry> _lootEspEntries =
            new List<LootEspEntry>(256);
        private readonly List<LootItem> _looseWorldLootItems =
            new List<LootItem>(512);
        private readonly List<LootEspCluster> _lootEspClusters =
            new List<LootEspCluster>(64);
        private readonly Dictionary<long, ScreenLootLabelGroup>
            _screenLootLabelGroups =
                new Dictionary<long, ScreenLootLabelGroup>(128);
        private readonly List<ScreenLootLabelGroup>
            _activeScreenLootLabelGroups =
                new List<ScreenLootLabelGroup>(128);
        private readonly List<ScreenLootLabelGroup>
            _screenLootLabelGroupPool =
                new List<ScreenLootLabelGroup>(128);
        private bool _lootWorldCacheDirty = true;
        private bool _lootEntryBuildActive;
        private int _lootEntryBuildCursor;
        private bool _containerCacheDirty = true;
        private bool _containerCacheBuildActive;
        private int _containerCacheBuildCursor;
        private float _lootEspMinimumPrice;
        private float _lootEspMaximumPrice;
        private readonly List<LootableContainer> _lootContainers =
            new List<LootableContainer>(256);
        private readonly Dictionary<int, Renderer[]>
            _lootContainerRenderers =
                new Dictionary<int, Renderer[]>(256);
        private readonly Dictionary<int, Bounds>
            _lootContainerBounds =
                new Dictionary<int, Bounds>(256);
        private readonly HashSet<int> _lootContainersWithBounds =
            new HashSet<int>();
        private readonly List<string> _containerMatchingNames =
            new List<string>(8);
        private readonly List<ContainerEspEntry> _containerEspEntries =
            new List<ContainerEspEntry>(64);
        private readonly List<ContainerEspEntry>
            _containerEspEntryPool =
                new List<ContainerEspEntry>(256);
        private readonly List<TraderControllerClass>
            _lootContainerOwners =
                new List<TraderControllerClass>(256);
        private Vector2 _lootCategoryScroll;
        private Vector2 _lootSettingsScroll;
        private float _lootListViewportHeight = 500f;
        private int _lootMenuSection;
        private int _selectedLootRoot;
        private string _lootSearch = "";
        private bool _lootSelectedOnly;
        private GUIStyle _lootCategoryButtonStyle;
        private GUIStyle _lootFoldoutButtonStyle;
        private bool _lootSelectionCountsDirty = true;
        private bool _lootCatalogHasUnresolvedNames;
        private float _nextLootNameRefresh;
        private int _lootNameRefreshAttempts;
        private readonly HashSet<string> _lootItemActionsInProgress =
            new HashSet<string>(StringComparer.Ordinal);
        private Rect _lootQuantityPopupRect =
            new Rect(0f, 0f, 390f, 205f);
        private LootCatalogItem _lootQuantityItem;
        private string _lootQuantityText = "1";

        private static bool IsUnresolvedLootName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ||
                   name.Equals(
                       "Unknown",
                       StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith(
                       "Unknown [",
                       StringComparison.OrdinalIgnoreCase);
        }

        private void ConfigureLootTools()
        {
            _lootEspEnabled = Config.Bind(
                "Loot ESP", "Enabled", false,
                "Draw selected loose world loot.");
            _lootEspBoxes = Config.Bind(
                "Loot ESP", "Boxes", false,
                "Draw a box around matching loose loot.");
            _lootEspNames = Config.Bind(
                "Loot ESP", "Names", false, "Show the item name.");
            _lootEspDistance = Config.Bind(
                "Loot ESP", "Distance", false,
                "Show distance to the item.");
            _lootEspPrices = Config.Bind(
                "Loot ESP", "Prices", false,
                "Show the cached flea price or handbook price.");
            _lootContainerEsp = Config.Bind(
                "Loot ESP", "Containers", false,
                "Render containers that hold selected or price-matching items.");
            _lootProximityGrouping = Config.Bind(
                "Loot ESP", "Proximity Grouping", false,
                "Group nearby distant loose items into an area marker.");
            _lootValueGradient = Config.Bind(
                "Loot ESP", "Value Gradient", false,
                "Color matched loot between the lowest and highest displayed value.");
            _lootEspCullDistance = Config.Bind(
                "Loot ESP", "Cull Distance", 50f,
                new ConfigDescription(
                    "Maximum loose-loot ESP distance.",
                    new AcceptableValueRange<float>(10f, 1000f)));
            _lootContainerCullDistance = Config.Bind(
                "Loot ESP", "Container Cull Distance", 25f,
                new ConfigDescription(
                    "Maximum container ESP and content-scan distance.",
                    new AcceptableValueRange<float>(10f, 1000f)));
            _lootGroupingDistance = Config.Bind(
                "Loot ESP", "Grouping Distance", 35f,
                new ConfigDescription(
                    "Start grouping nearby loose items beyond this distance.",
                    new AcceptableValueRange<float>(0f, 300f)));
            _lootGroupCullDistance = Config.Bind(
                "Loot ESP", "Group Cull Distance", 75f,
                new ConfigDescription(
                    "Maximum distance for grouped loot areas.",
                    new AcceptableValueRange<float>(25f, 1000f)));
            _lootProximityRadius = Config.Bind(
                "Loot ESP", "Proximity Radius", 5f,
                new ConfigDescription(
                    "Maximum world-space distance between grouped items.",
                    new AcceptableValueRange<float>(0.5f, 25f)));
            _lootProximityHeight = Config.Bind(
                "Loot ESP", "Proximity Height", 2f,
                new ConfigDescription(
                    "Maximum vertical separation between grouped items.",
                    new AcceptableValueRange<float>(0.25f, 25f)));
            _lootItemFontSize = Config.Bind(
                "Loot ESP", "Item Font Size", 12,
                new ConfigDescription(
                    "Font size used by individual loot labels.",
                    new AcceptableValueRange<int>(8, 24)));
            _lootContainerFontSize = Config.Bind(
                "Loot ESP", "Container Font Size", 12,
                new ConfigDescription(
                    "Font size used by container names and contents.",
                    new AcceptableValueRange<int>(8, 24)));
            _lootGroupFontSize = Config.Bind(
                "Loot ESP", "Group Font Size", 10,
                new ConfigDescription(
                    "Font size used by grouped-loot summaries.",
                    new AcceptableValueRange<int>(8, 24)));
            _lootEspColor = Config.Bind(
                "Loot ESP", "Selected Item Color", "#22D3EEFF",
                "RGBA color for explicitly selected items.");
            _lootEspPriceColor = Config.Bind(
                "Loot ESP", "Price Match Color", "#FACC15FF",
                "RGBA color for items included by the price range.");
            _lootContainerEspColor = Config.Bind(
                "Loot ESP", "Container Color", "#FB923CFF",
                "RGBA color for containers holding matching items.");
            _lootLowValueColor = Config.Bind(
                "Loot ESP", "Low Value Color", "#94A3B8FF",
                "RGBA color for the lowest-priced displayed loot.");
            _lootLowMidValueColor = Config.Bind(
                "Loot ESP", "Low-Mid Value Color", "#38BDF8FF",
                "RGBA color for lower-middle displayed loot.");
            _lootMidValueColor = Config.Bind(
                "Loot ESP", "Mid Value Color", "#22C55EFF",
                "RGBA color for middle-value displayed loot.");
            _lootHighMidValueColor = Config.Bind(
                "Loot ESP", "High-Mid Value Color", "#FACC15FF",
                "RGBA color for upper-middle displayed loot.");
            _lootHighValueColor = Config.Bind(
                "Loot ESP", "High Value Color", "#22C55EFF",
                "RGBA color for the highest-priced displayed loot.");
            _lootQuestItemColor = Config.Bind(
                "Loot ESP", "Quest Item Color", "#C084FCFF",
                "RGBA override color for quest items.");
            _lootPriceRangeEnabled = Config.Bind(
                "Loot ESP", "Price Range Enabled", false,
                "Always show items whose cached value is inside the range.");
            _lootPriceRangeSelectedOnly = Config.Bind(
                "Loot ESP",
                "Price Range Selected Items Only",
                true,
                "Apply price-range matching only to templates checked in the item list.");
            _lootPriceMinimum = Config.Bind(
                "Loot ESP", "Minimum Price", 100000f,
                new ConfigDescription(
                    "Minimum inclusive loot ESP price.",
                    new AcceptableValueRange<float>(
                        0f, LootPriceMaximumLimit)));
            _lootPriceMaximum = Config.Bind(
                "Loot ESP", "Maximum Price", LootPriceMaximumLimit,
                new ConfigDescription(
                    "Maximum inclusive loot ESP price.",
                    new AcceptableValueRange<float>(
                        0f, LootPriceMaximumLimit)));
            _lootSelectedItemSetting = Config.Bind(
                "Loot ESP", "Selected Template IDs", "",
                "Comma-separated item template IDs selected for loot ESP.");
            LoadLootSelections();
            _lootEspEnabled.SettingChanged += OnLootFilterSettingChanged;
            _lootContainerEsp.SettingChanged += OnLootFilterSettingChanged;
            _lootValueGradient.SettingChanged += OnLootFilterSettingChanged;
            _lootPriceRangeEnabled.SettingChanged +=
                OnLootFilterSettingChanged;
            _lootPriceRangeSelectedOnly.SettingChanged +=
                OnLootFilterSettingChanged;
            _lootPriceMinimum.SettingChanged += OnLootFilterSettingChanged;
            _lootPriceMaximum.SettingChanged += OnLootFilterSettingChanged;
            _lootEspPrices.SettingChanged += OnLootFilterSettingChanged;
            _lootProximityGrouping.SettingChanged +=
                OnLootFilterSettingChanged;
            _lootGroupingDistance.SettingChanged +=
                OnLootFilterSettingChanged;
            _lootGroupCullDistance.SettingChanged +=
                OnLootFilterSettingChanged;
            _lootProximityRadius.SettingChanged +=
                OnLootFilterSettingChanged;
            _lootProximityHeight.SettingChanged +=
                OnLootFilterSettingChanged;
        }

        private void ClearLootEspCaches()
        {
            DetachLootContainerEvents();
            _lootEspEntries.Clear();
            _looseWorldLootItems.Clear();
            _lootEspClusters.Clear();
            _lootContainers.Clear();
            _lootContainerRenderers.Clear();
            _lootContainerBounds.Clear();
            _lootContainersWithBounds.Clear();
            RecycleContainerEspEntries();
            _lootWorldCacheDirty = true;
            _containerCacheDirty = true;
            _lootEntryBuildActive = false;
            _lootEntryBuildCursor = 0;
            _containerCacheBuildActive = false;
            _containerCacheBuildCursor = 0;
        }

        private void OnLootFilterSettingChanged(
            object sender,
            EventArgs args)
        {
            InvalidateLootCaches();
        }

        private void InvalidateLootCaches()
        {
            _lootWorldCacheDirty = true;
            _containerCacheDirty = true;
        }

        private void LoadLootSelections()
        {
            _lootSelectedItems.Clear();
            if (_lootSelectedItemSetting == null ||
                string.IsNullOrWhiteSpace(_lootSelectedItemSetting.Value))
                return;

            string[] ids = _lootSelectedItemSetting.Value.Split(',');
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i].Trim();
                if (!string.IsNullOrEmpty(id))
                    _lootSelectedItems.Add(id);
            }
        }

        private void SaveLootSelections()
        {
            List<string> ids = new List<string>(_lootSelectedItems);
            ids.Sort(StringComparer.Ordinal);
            _lootSelectedItemSetting.Value = string.Join(",", ids);
            InvalidateLootCaches();
        }

    }
}
