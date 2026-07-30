
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void InstallOtherPatches()
        {
            try
            {
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(GamePlayerOwner),
                        nameof(GamePlayerOwner.InteractionsChangedHandler)),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(AddLivingAiInteraction))));

                _harmony.Patch(
                    AccessTools.Method(
                        typeof(Player.PlayerInventoryController),
                        nameof(
                            Player.PlayerInventoryController
                                .CheckItemAction),
                        new[]
                        {
                            typeof(Item),
                            typeof(ItemAddress)
                        }),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ProtectLivingAiHandsItem))));

                _harmony.Patch(
                    AccessTools.Method(
                        typeof(GamePlayerOwner),
                        nameof(GamePlayerOwner.TranslateCommand)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(BlockGameCommandWhileMenuOpen))));

                _harmony.Patch(
                    AccessTools.Method(
                        typeof(GamePlayerOwner),
                        nameof(GamePlayerOwner.TranslateAxes)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(BlockGameAxesWhileMenuOpen))));

                LogSource.LogInfo(
                    "Other-tool patches installed.");
            }
            catch (Exception exception)
            {
                LogSource.LogError(
                    "Failed to install living-AI loot patches: " +
                    exception);
            }
        }

        private static bool BlockGameCommandWhileMenuOpen(
            ref InputNode.ETranslateResult __result)
        {
            Plugin plugin = _instance;
            if (plugin == null || !plugin._menuOpen)
                return true;

            __result = InputNode.ETranslateResult.BlockAll;
            return false;
        }

        private static bool BlockGameAxesWhileMenuOpen(
            ref float[] axes)
        {
            Plugin plugin = _instance;
            if (plugin == null || !plugin._menuOpen)
                return true;

            if (axes != null)
                Array.Clear(axes, 0, axes.Length);
            return false;
        }

    }
}
