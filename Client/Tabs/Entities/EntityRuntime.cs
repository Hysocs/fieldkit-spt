
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
                {
                    int kindComparison = string.Compare(
                        left.Kind,
                        right.Kind,
                        StringComparison.OrdinalIgnoreCase);
                    return kindComparison != 0
                        ? kindComparison
                        : left.Distance.CompareTo(right.Distance);
                });

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
                    BotOwner =
                        player.AIData == null ||
                        !player.AIData.IsAI
                            ? null
                            : player.AIData.BotOwner,
                    Distance = Vector3.Distance(
                        origin,
                        player.Position)
                });
        }

        private void SetEntityAiEnabled(
            BotOwner botOwner,
            bool enabled)
        {
            if (botOwner == null || botOwner.IsDead)
                return;

            if (!enabled)
            {
                if (_disabledEntityAi.ContainsKey(botOwner))
                    return;

                EBotState state =
                    ReferenceEquals(
                        botOwner,
                        _livingLootPausedBot)
                        ? _livingLootPausedBotState
                        : botOwner.BotState;
                _disabledEntityAi.Add(botOwner, state);
                botOwner.StopMove();
                if (botOwner.BotState != EBotState.NonActive)
                    botOwner.Disable();
                return;
            }

            EBotState originalState;
            if (!_disabledEntityAi.TryGetValue(
                    botOwner,
                    out originalState))
                return;

            _disabledEntityAi.Remove(botOwner);
            if (ReferenceEquals(
                    botOwner,
                    _livingLootPausedBot))
                return;

            RestoreEntityBotState(botOwner, originalState);
        }

        private void SetAllEntityAiEnabled(bool enabled)
        {
            for (int i = 0; i < _liveEntityEntries.Count; i++)
            {
                BotOwner owner = _liveEntityEntries[i].BotOwner;
                if (owner != null)
                    SetEntityAiEnabled(owner, enabled);
            }
        }

        private bool AreAllEntityAiEnabled()
        {
            for (int i = 0; i < _liveEntityEntries.Count; i++)
            {
                BotOwner owner = _liveEntityEntries[i].BotOwner;
                if (owner != null &&
                    _disabledEntityAi.ContainsKey(owner))
                    return false;
            }
            return true;
        }

        private void SetEntityFriendly(
            BotOwner botOwner,
            bool friendly)
        {
            if (botOwner == null || botOwner.IsDead)
                return;

            if (friendly)
                _friendlyEntityAi.Add(botOwner);
            else
                _friendlyEntityAi.Remove(botOwner);

            ApplyEntityFriendlyState(botOwner, friendly);
        }

        private void SetAllEntitiesFriendly(bool friendly)
        {
            for (int i = 0; i < _liveEntityEntries.Count; i++)
            {
                BotOwner owner = _liveEntityEntries[i].BotOwner;
                if (owner != null)
                    SetEntityFriendly(owner, friendly);
            }
        }

        private bool AreAllEntitiesFriendly()
        {
            bool foundAi = false;
            for (int i = 0; i < _liveEntityEntries.Count; i++)
            {
                BotOwner owner = _liveEntityEntries[i].BotOwner;
                if (owner == null)
                    continue;

                foundAi = true;
                if (!_friendlyEntityAi.Contains(owner))
                    return false;
            }
            return foundAi;
        }

        private void KillEntityBot(Player player)
        {
            if (player == null ||
                player.IsYourPlayer ||
                player.AIData == null ||
                !player.AIData.IsAI ||
                player.HealthController == null ||
                !player.HealthController.IsAlive)
                return;

            try
            {
                player.KillMe(
                    EBodyPartColliderType.HeadCommon,
                    100000f);
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not kill entity: " +
                    exception.Message);
            }
        }

        private void KillAllEntityBots()
        {
            Player[] bots = _liveEntityEntries
                .Where(entry => entry.BotOwner != null)
                .Select(entry => entry.Player)
                .ToArray();
            for (int i = 0; i < bots.Length; i++)
                KillEntityBot(bots[i]);
        }

        private void RestoreEntityBotState(
            BotOwner botOwner,
            EBotState originalState)
        {
            if (botOwner == null ||
                botOwner.IsDead ||
                botOwner.BotState != EBotState.NonActive ||
                originalState == EBotState.NonActive ||
                originalState == EBotState.Disposed ||
                SetBotStateMethod == null)
                return;

            try
            {
                SetBotStateMethod.Invoke(
                    botOwner,
                    new object[] { originalState });
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not restore entity AI: " +
                    exception.Message);
            }
        }

        private void ReleaseEntityAiOverrides()
        {
            foreach (
                KeyValuePair<BotOwner, EBotState> disabled
                in _disabledEntityAi)
            {
                if (!ReferenceEquals(
                        disabled.Key,
                        _livingLootPausedBot))
                {
                    RestoreEntityBotState(
                        disabled.Key,
                        disabled.Value);
                }
            }

            _disabledEntityAi.Clear();
        }

        private void HandleEntityAiRemoved(Player player)
        {
            BotOwner owner =
                player == null ||
                player.AIData == null
                    ? null
                    : player.AIData.BotOwner;
            if (owner != null)
            {
                _disabledEntityAi.Remove(owner);
                _friendlyEntityAi.Remove(owner);
            }
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
