
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
            {
                ResumeLivingLootTarget();
                return;
            }

            try
            {
                PauseLivingLootTarget();
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not pause living loot target: " +
                    exception.Message);
            }
        }

        private void PauseLivingLootTarget()
        {
            BotOwner botOwner =
                _livingLootTarget == null ||
                _livingLootTarget.AIData == null
                    ? null
                    : _livingLootTarget.AIData.BotOwner;
            if (botOwner == null ||
                botOwner.IsDead ||
                ReferenceEquals(botOwner, _livingLootPausedBot))
                return;

            ResumeLivingLootTarget();

            _livingLootPausedBot = botOwner;
            _livingLootPausedBotState = botOwner.BotState;
            botOwner.StopMove();

            Animator[] animators =
                _livingLootTarget.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator == null)
                    continue;

                _livingLootPausedAnimators.Add(
                    new KeyValuePair<Animator, float>(
                        animator,
                        animator.speed));
                animator.speed = 0f;
            }

            // Disable only changes BotState to NonActive. It does not tear
            // down the bot, so restoring the saved state resumes the exact
            // same brain and decision rather than reinitializing the AI.
            botOwner.Disable();
        }

        private void ResumeLivingLootTarget()
        {
            BotOwner botOwner = _livingLootPausedBot;
            EBotState originalState = _livingLootPausedBotState;
            _livingLootPausedBot = null;
            _livingLootPausedBotState = EBotState.NonActive;

            for (int i = 0;
                 i < _livingLootPausedAnimators.Count;
                 i++)
            {
                KeyValuePair<Animator, float> saved =
                    _livingLootPausedAnimators[i];
                if (saved.Key != null)
                {
                    try
                    {
                        saved.Key.speed = saved.Value;
                    }
                    catch { }
                }
            }
            _livingLootPausedAnimators.Clear();

            if (botOwner == null ||
                botOwner.IsDead ||
                botOwner.BotState != EBotState.NonActive ||
                _disabledEntityAi.ContainsKey(botOwner) ||
                originalState == EBotState.NonActive ||
                originalState == EBotState.Disposed)
                return;

            if (SetBotStateMethod == null)
            {
                LogSource.LogWarning(
                    "Could not restore paused living AI: " +
                    "BotState setter was not found.");
                return;
            }

            try
            {
                SetBotStateMethod.Invoke(
                    botOwner,
                    new object[] { originalState });
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not restore paused living AI: " +
                    exception.Message);
            }
        }

        private void ApplyEntityFriendlyState(
            BotOwner botOwner,
            bool friendly)
        {
            if (botOwner == null || botOwner.Memory == null)
                return;

            botOwner.Memory.IsPeace = friendly;
            if (!friendly)
                return;

            botOwner.Memory.LoseVisionCurrentEnemy();
            if (_world == null ||
                _world.RegisteredPlayers == null)
                return;

            foreach (IPlayer person in _world.RegisteredPlayers)
            {
                if (person != null)
                    botOwner.Memory.DeleteInfoAboutEnemy(person);
            }
        }

        private void UpdateFriendlyAi()
        {
            if (_friendlyEntityAi.Count == 0 ||
                Time.unscaledTime < _nextFriendlyEntityRefresh)
                return;

            _nextFriendlyEntityRefresh =
                Time.unscaledTime + 0.1f;
            _perfFriendlyAiRefreshes++;

            BotOwner[] friendlyBots =
                _friendlyEntityAi.ToArray();
            for (int i = 0; i < friendlyBots.Length; i++)
            {
                BotOwner botOwner = friendlyBots[i];
                if (botOwner == null || botOwner.IsDead)
                {
                    _friendlyEntityAi.Remove(botOwner);
                    continue;
                }

                try
                {
                    ApplyEntityFriendlyState(botOwner, true);
                }
                catch (Exception exception)
                {
                    LogSource.LogWarning(
                        "Could not refresh friendly AI state: " +
                        exception.Message);
                }
            }
        }

        private void ReleaseFriendlyAi()
        {
            BotOwner[] friendlyBots =
                _friendlyEntityAi.ToArray();
            _friendlyEntityAi.Clear();
            for (int i = 0; i < friendlyBots.Length; i++)
            {
                try
                {
                    ApplyEntityFriendlyState(
                        friendlyBots[i],
                        false);
                }
                catch { }
            }
            _nextFriendlyEntityRefresh = 0f;
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
