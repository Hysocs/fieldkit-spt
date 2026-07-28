
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void UpdateEntityTools()
        {
            if (!_menuOpen ||
                _menuTab != 1 ||
                _entityListSection == 0)
                return;

            if (_world == null || _localPlayer == null)
            {
                _liveEntityEntries.Clear();
                _liveLootEntries.Clear();
                return;
            }

            if (Time.unscaledTime < _nextEntityListRefresh)
                return;

            _nextEntityListRefresh = Time.unscaledTime + 0.25f;
            if (_entityListSection == 1)
                RefreshLiveEntities();
            else
                RefreshLiveLoot();
        }

        private void RefreshLiveEntities()
        {
            _liveEntityEntries.Clear();
            Vector3 origin = _localPlayer.Position;

            AddLiveEntityEntry(
                _localPlayer, "You", origin);
            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                Player player = target.Player;
                if (player == null ||
                    player.HealthController == null ||
                    !player.HealthController.IsAlive)
                    continue;

                AddLiveEntityEntry(
                    player, KindName(target.Kind), origin);
            }

            _liveEntityEntries.Sort(
                (left, right) =>
                    left.Distance.CompareTo(right.Distance));
        }

        private void AddLiveEntityEntry(
            Player player,
            string kind,
            Vector3 origin)
        {
            if (player == null ||
                player.HealthController == null ||
                !player.HealthController.IsAlive)
                return;

            string name =
                player.Profile == null ||
                player.Profile.Info == null ||
                string.IsNullOrWhiteSpace(
                    player.Profile.Info.Nickname)
                    ? player.ProfileId
                    : player.Profile.Info.Nickname;

            _liveEntityEntries.Add(
                new LiveEntityEntry
                {
                    Player = player,
                    Name = name,
                    Kind = kind,
                    Distance = Vector3.Distance(
                        origin,
                        player.Position)
                });
        }

        private void RefreshLiveLoot()
        {
            _liveLootEntries.Clear();
            if (_world == null || _world.LootItems == null)
                return;

            Vector3 origin = _localPlayer.Position;
            int count = _world.LootItems.Count;
            for (int i = 0; i < count; i++)
            {
                LootItem loot = _world.LootItems.GetByIndex(i);
                if (!IsLooseWorldLoot(loot) || loot.Item == null)
                    continue;

                string templateId =
                    loot.Item.TemplateId.ToString();
                LootCatalogItem catalogItem;
                _lootCatalogItems.TryGetValue(
                    templateId,
                    out catalogItem);
                string name = catalogItem == null
                    ? loot.Item.Template == null
                        ? templateId
                        : loot.Item.Template.Name
                    : catalogItem.Name;
                float price = GetLootPrice(
                    templateId,
                    catalogItem == null
                        ? 0f
                        : catalogItem.BasePrice);

                _liveLootEntries.Add(
                    new LiveLootEntry
                    {
                        Loot = loot,
                        Name = name,
                        TemplateId = templateId,
                        Distance = Vector3.Distance(
                            origin,
                            loot.transform.position),
                        Price = price
                    });
            }

            _liveLootEntries.Sort(
                (left, right) =>
                    left.Distance.CompareTo(right.Distance));
        }

        private bool EntitySearchMatches(
            string name,
            string secondary)
        {
            if (string.IsNullOrWhiteSpace(_entitySearch))
                return true;

            return (!string.IsNullOrEmpty(name) &&
                    name.IndexOf(
                        _entitySearch,
                        StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (!string.IsNullOrEmpty(secondary) &&
                    secondary.IndexOf(
                        _entitySearch,
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
