
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void UpdateOtherTools()
        {
            if (_lootLivingAi == null)
                return;

            UpdateForcedVisionModes();
            UpdateFriendlyAi();

            if (_lastLootLivingAi != _lootLivingAi.Value)
            {
                _lastLootLivingAi = _lootLivingAi.Value;

                if (!_lootLivingAi.Value)
                    CloseLivingAiInventory();

                if (_localPlayer != null)
                    _localPlayer.ForceInteractionsChanged();
            }

            if (_livingLootTarget == null)
                return;

            if (!IsValidLivingAiTarget(_livingLootTarget) ||
                _livingLootOwner == null ||
                _livingLootOwner.Player == null)
            {
                CloseLivingAiInventory();
                return;
            }

            if (_livingLootOpening)
            {
                ContinueOpeningLivingAiInventory();
                return;
            }

            if (!_livingLootOwner.Player.IsInventoryOpened)
            {
                CloseLivingAiInventory();
                return;
            }

            if (!_holdLivingAiStill.Value)
                return;

            try
            {
                BotOwner botOwner = _livingLootTarget.AIData.BotOwner;
                if (botOwner != null && !botOwner.IsDead)
                {
                    botOwner.StopMove();
                    botOwner.MovementPause(0.25f);
                }
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not pause living loot target: " +
                    exception.Message);
            }
        }

        private static bool PreventAiEnemyAddition(ref bool __result)
        {
            Plugin plugin = _instance;
            if (plugin == null ||
                plugin._allAiFriendly == null ||
                !plugin._allAiFriendly.Value)
                return true;

            __result = false;
            return false;
        }

        private void UpdateFriendlyAi()
        {
            bool enabled =
                _allAiFriendly != null &&
                _allAiFriendly.Value;
            if (_lastAllAiFriendly && !enabled)
                ReleaseFriendlyAi();
            _lastAllAiFriendly = enabled;

            if (_allAiFriendly == null ||
                !enabled ||
                _world == null ||
                _world.RegisteredPlayers == null ||
                !_friendlyAiRefreshRequested)
                return;

            _friendlyAiRefreshRequested = false;
            _perfFriendlyAiRefreshes++;

            try
            {
                foreach (IPlayer botPerson in _world.RegisteredPlayers)
                {
                    Player botPlayer = botPerson as Player;
                    if (botPlayer == null ||
                        botPlayer.AIData == null ||
                        !botPlayer.AIData.IsAI)
                        continue;

                    BotOwner botOwner = botPlayer.AIData.BotOwner;
                    if (botOwner == null || botOwner.IsDead)
                        continue;

                    BotsGroup group = botOwner.BotsGroup;
                    foreach (IPlayer otherPerson
                        in _world.RegisteredPlayers)
                    {
                        if (otherPerson == null ||
                            ReferenceEquals(otherPerson, botPerson))
                            continue;

                        if (group != null &&
                            group.IsEnemy(otherPerson))
                        {
                            group.RemoveEnemy(
                                otherPerson,
                                EBotEnemyCause.pairLogic);
                        }

                        if (botOwner.Memory != null)
                            botOwner.Memory.DeleteInfoAboutEnemy(
                                otherPerson);
                    }

                    if (botOwner.Memory != null)
                    {
                        botOwner.Memory.LoseVisionCurrentEnemy();
                        botOwner.Memory.IsPeace = true;
                    }
                }
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not refresh friendly AI state: " +
                    exception.Message);
            }
        }

        private void ReleaseFriendlyAi()
        {
            if (_world == null ||
                _world.RegisteredPlayers == null)
                return;

            foreach (IPlayer person in _world.RegisteredPlayers)
            {
                Player player = person as Player;
                BotOwner owner =
                    player == null ||
                    player.AIData == null ||
                    !player.AIData.IsAI
                        ? null
                        : player.AIData.BotOwner;
                if (owner != null &&
                    owner.Memory != null)
                    owner.Memory.IsPeace = false;
            }

            _friendlyAiRefreshRequested = true;
        }

        private void UnlockAllDoors()
        {
            if (_world == null)
            {
                _doorToolStatus = "Enter a raid before unlocking doors.";
                return;
            }

            int unlocked = 0;
            int failed = 0;

            EFT.Interactive.WorldInteractiveObject[] doors =
                UnityEngine.Object.FindObjectsOfType<
                    EFT.Interactive.WorldInteractiveObject>();

            for (int i = 0; i < doors.Length; i++)
            {
                EFT.Interactive.WorldInteractiveObject door =
                    doors[i];
                if (door == null ||
                    door.DoorState !=
                        EFT.Interactive.EDoorState.Locked)
                    continue;

                try
                {
                    door.Unlock();
                    unlocked++;
                }
                catch (Exception exception)
                {
                    failed++;
                    LogSource.LogWarning(
                        "Could not unlock world door '" +
                        door.name + "': " + exception.Message);
                }
            }

            _doorToolStatus = unlocked + " door" +
                (unlocked == 1 ? "" : "s") + " unlocked.";
            if (failed > 0)
                _doorToolStatus += " " + failed + " failed.";

            LogSource.LogInfo(_doorToolStatus);
        }

    }
}
