
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private readonly List<LiveEntityEntry> _liveEntityEntries =
            new List<LiveEntityEntry>(64);
        private readonly List<LiveLootEntry> _liveLootEntries =
            new List<LiveLootEntry>(512);
        private Vector2 _entityListScroll;
        private int _entityListSection;
        private string _entitySearch = "";
        private float _nextEntityListRefresh;
        private static readonly RaycastHit[] EntityGroundHits =
            new RaycastHit[32];

        private sealed class LiveEntityEntry
        {
            public Player Player;
            public string Name;
            public string Kind;
            public float Distance;
        }

        private sealed class LiveLootEntry
        {
            public LootItem Loot;
            public string Name;
            public string TemplateId;
            public float Distance;
            public float Price;
        }
    }
}
