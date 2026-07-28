
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private long _perfWindowStarted;
        private long _perfUpdateTicks;
        private long _perfEspTicks;
        private long _perfLootTicks;
        private long _perfGuiTicks;
        private long _perfWorldTicks;
        private long _perfCacheBuildTicks;
        private long _perfWorldRefreshTicks;
        private long _perfChamsTicks;
        private long _perfCatalogTicks;
        private long _perfUpdateMaxTicks;
        private long _perfEspMaxTicks;
        private long _perfLootMaxTicks;
        private long _perfGuiMaxTicks;
        private long _perfWorldMaxTicks;
        private long _perfCacheBuildMaxTicks;
        private long _perfWorldRefreshMaxTicks;
        private long _perfChamsMaxTicks;
        private long _perfCatalogMaxTicks;
        private int _perfUpdateCalls;
        private int _perfEspCalls;
        private int _perfLootCalls;
        private int _perfGuiCalls;
        private int _perfWorldCalls;
        private int _perfCacheBuildCalls;
        private int _perfWorldRefreshCalls;
        private int _perfChamsCalls;
        private int _perfCatalogCalls;
        private float _perfUpdateMs;
        private float _perfEspMs;
        private float _perfLootMs;
        private float _perfGuiMs;
        private float _perfWorldMs;
        private float _perfCacheBuildMs;
        private float _perfUpdateMaxMs;
        private float _perfEspMaxMs;
        private float _perfLootMaxMs;
        private float _perfGuiMaxMs;
        private float _perfWorldMaxMs;
        private float _perfCacheBuildMaxMs;
        private float _perfWorldRefreshMaxMs;
        private float _perfChamsMaxMs;
        private float _perfCatalogMaxMs;
        private float _perfUpdateRate;
        private float _perfEspRate;
        private float _perfLootRate;
        private float _perfGuiRate;
        private float _perfCorePercent;
        private int _perfLootInvalidations;
        private int _perfContainerInvalidations;
        private int _perfFriendlyAiRefreshes;
        private int _perfChamDiscoveryPasses;

        private static long PerfTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        private static void RecordPerf(
            long started,
            ref long ticks,
            ref int calls,
            ref long maximumTicks)
        {
            long elapsed = Stopwatch.GetTimestamp() - started;
            ticks += elapsed;
            calls++;
            if (elapsed > maximumTicks)
                maximumTicks = elapsed;
        }

        private void PublishPerformanceTelemetry()
        {
            long now = Stopwatch.GetTimestamp();
            if (_perfWindowStarted == 0)
            {
                _perfWindowStarted = now;
                return;
            }

            double seconds =
                (now - _perfWindowStarted) /
                (double)Stopwatch.Frequency;
            if (seconds < 1.0)
                return;

            double millisecondsPerTick =
                1000.0 / Stopwatch.Frequency;
            _perfUpdateMs = AveragePerfMilliseconds(
                _perfUpdateTicks,
                _perfUpdateCalls,
                millisecondsPerTick);
            _perfEspMs = AveragePerfMilliseconds(
                _perfEspTicks,
                _perfEspCalls,
                millisecondsPerTick);
            _perfLootMs = AveragePerfMilliseconds(
                _perfLootTicks,
                _perfLootCalls,
                millisecondsPerTick);
            _perfGuiMs = AveragePerfMilliseconds(
                _perfGuiTicks,
                _perfGuiCalls,
                millisecondsPerTick);
            _perfWorldMs = AveragePerfMilliseconds(
                _perfWorldTicks,
                _perfWorldCalls,
                millisecondsPerTick);
            _perfCacheBuildMs = AveragePerfMilliseconds(
                _perfCacheBuildTicks,
                _perfCacheBuildCalls,
                millisecondsPerTick);
            _perfUpdateMaxMs =
                (float)(_perfUpdateMaxTicks * millisecondsPerTick);
            _perfEspMaxMs =
                (float)(_perfEspMaxTicks * millisecondsPerTick);
            _perfLootMaxMs =
                (float)(_perfLootMaxTicks * millisecondsPerTick);
            _perfGuiMaxMs =
                (float)(_perfGuiMaxTicks * millisecondsPerTick);
            _perfWorldMaxMs =
                (float)(_perfWorldMaxTicks * millisecondsPerTick);
            _perfCacheBuildMaxMs =
                (float)(_perfCacheBuildMaxTicks * millisecondsPerTick);
            _perfWorldRefreshMaxMs =
                (float)(_perfWorldRefreshMaxTicks * millisecondsPerTick);
            _perfChamsMaxMs =
                (float)(_perfChamsMaxTicks * millisecondsPerTick);
            _perfCatalogMaxMs =
                (float)(_perfCatalogMaxTicks * millisecondsPerTick);
            _perfUpdateRate =
                (float)(_perfUpdateCalls / seconds);
            _perfEspRate =
                (float)(_perfEspCalls / seconds);
            _perfLootRate =
                (float)(_perfLootCalls / seconds);
            _perfGuiRate =
                (float)(_perfGuiCalls / seconds);
            double ownMilliseconds =
                (_perfUpdateTicks +
                 _perfEspTicks +
                 _perfWorldTicks +
                 _perfGuiTicks) *
                millisecondsPerTick;
            _perfCorePercent =
                (float)(ownMilliseconds /
                        (seconds * 1000.0) * 100.0);

            _perfWindowStarted = now;
            _perfUpdateTicks = 0;
            _perfEspTicks = 0;
            _perfLootTicks = 0;
            _perfGuiTicks = 0;
            _perfWorldTicks = 0;
            _perfCacheBuildTicks = 0;
            _perfWorldRefreshTicks = 0;
            _perfChamsTicks = 0;
            _perfCatalogTicks = 0;
            _perfUpdateMaxTicks = 0;
            _perfEspMaxTicks = 0;
            _perfLootMaxTicks = 0;
            _perfGuiMaxTicks = 0;
            _perfWorldMaxTicks = 0;
            _perfCacheBuildMaxTicks = 0;
            _perfWorldRefreshMaxTicks = 0;
            _perfChamsMaxTicks = 0;
            _perfCatalogMaxTicks = 0;
            _perfUpdateCalls = 0;
            _perfEspCalls = 0;
            _perfLootCalls = 0;
            _perfGuiCalls = 0;
            _perfWorldCalls = 0;
            _perfCacheBuildCalls = 0;
            _perfWorldRefreshCalls = 0;
            _perfChamsCalls = 0;
            _perfCatalogCalls = 0;
        }

        private static float AveragePerfMilliseconds(
            long ticks,
            int calls,
            double millisecondsPerTick)
        {
            return calls == 0
                ? 0f
                : (float)(ticks * millisecondsPerTick / calls);
        }
    }
}
