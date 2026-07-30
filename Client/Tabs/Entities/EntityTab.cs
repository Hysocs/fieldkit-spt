
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private readonly List<LiveEntityEntry> _liveEntityEntries =
            new List<LiveEntityEntry>(64);
        private readonly List<LiveLootEntry> _liveLootEntries =
            new List<LiveLootEntry>(512);
        private Vector2 _entityListScroll;
        private float _entityListViewportHeight = 500f;
        private int _entityListSection;
        private int _liveEntitySubTab;
        private string _entitySearch = "";
        private string _spawnEntitySearch = "";
        private Vector2 _spawnEntityScroll;
        private bool _spawnEntityAiDisabled;
        private bool _spawnEntityIgnoreNavMesh;
        private bool _spawnEntityInProgress;
        private string _spawnEntityStatus =
            "Aim at a navigable location, then choose an AI type.";
        private float _nextSpawnEntityCatalogRefresh;
        private int _entitySpawnGeneration;
        private CancellationTokenSource _entitySpawnCancellation;
        private BotOwner _lastSpawnedEntityBot;
        private int _lastSpawnedEntitySerial;
        private readonly HashSet<string> _fieldKitSpawnProfileIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int>
            _fieldKitResourceCapacity =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int>
            _fieldKitResourceUsage =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string[]>
            _fieldKitSpawnResources =
                new Dictionary<string, string[]>(
                    StringComparer.Ordinal);
        private BotSpawner _fieldKitCapacitySpawner;
        private int _fieldKitCapacityHeadroom;
        private readonly List<SpawnableAiEntry>
            _spawnableAiEntries =
                new List<SpawnableAiEntry>(64);
        private float _nextEntityListRefresh;
        private readonly Dictionary<BotOwner, EBotState>
            _disabledEntityAi =
                new Dictionary<BotOwner, EBotState>();
        private readonly HashSet<BotOwner> _friendlyEntityAi =
            new HashSet<BotOwner>();
        private float _nextFriendlyEntityRefresh;
        private readonly Dictionary<string, bool>
            _liveEntityGroupsExpanded =
                new Dictionary<string, bool>(
                    StringComparer.OrdinalIgnoreCase);
        private static readonly RaycastHit[] EntityGroundHits =
            new RaycastHit[32];

        private sealed class LiveEntityEntry
        {
            public Player Player;
            public string Name;
            public string Kind;
            public float Distance;
            public BotOwner BotOwner;
        }

        private sealed class LiveLootEntry
        {
            public LootItem Loot;
            public string Name;
            public string TemplateId;
            public float Distance;
            public float Price;
        }

        private sealed class SpawnableAiEntry
        {
            public WildSpawnType Role;
            public string Name;
            public string Group;
        }

    }
}
