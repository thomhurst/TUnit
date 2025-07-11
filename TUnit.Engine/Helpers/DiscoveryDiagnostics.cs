using TUnit.Engine.Services;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Provides diagnostics and monitoring for test discovery to detect hanging issues
/// </summary>
internal static class DiscoveryDiagnostics
{
    private static readonly object _lock = new();
    private static readonly List<DiscoveryEvent> _events =
    [
    ];
    private static VerbosityService? _verbosityService;

    public static bool IsEnabled { get; set; } = Environment.GetEnvironmentVariable("TUNIT_DISCOVERY_DIAGNOSTICS") == "1";

    public static void Initialize(VerbosityService verbosityService)
    {
        _verbosityService = verbosityService;
        // Override environment variable setting with verbosity service
        IsEnabled = IsEnabled || (_verbosityService?.EnableDiscoveryDiagnostics ?? false);
    }

    public static void RecordEvent(string eventName, string details = "")
    {
        if (!IsEnabled)
        {
            return;
        }

        lock (_lock)
        {
            _events.Add(new DiscoveryEvent
            {
                Timestamp = DateTime.UtcNow,
                EventName = eventName,
                Details = details,
                ThreadId = Environment.CurrentManagedThreadId
            });
        }
    }

    public static void RecordDataSourceStart(string sourceName, int itemCount = -1)
    {
        RecordEvent("DataSourceStart", $"Source: {sourceName}, Items: {itemCount}");
    }

    public static void RecordDataSourceEnd(string sourceName, int itemCount)
    {
        RecordEvent("DataSourceEnd", $"Source: {sourceName}, Items: {itemCount}");
    }

    public static void RecordTestExpansion(string testName, int combinationCount)
    {
        RecordEvent("TestExpansion", $"Test: {testName}, Combinations: {combinationCount}");
    }

    public static void RecordCartesianProductDepth(int depth, int setCount)
    {
        RecordEvent("CartesianProduct", $"Depth: {depth}, Sets: {setCount}");
    }

    public static void RecordHangDetection(string location, int elapsedSeconds)
    {
        RecordEvent("PotentialHang", $"Location: {location}, Elapsed: {elapsedSeconds}s");

        // Also write to console for immediate visibility if verbosity allows
        if (_verbosityService == null || !_verbosityService.HideTestOutput)
        {
            Console.Error.WriteLine($"[TUnit] WARNING: Potential hang detected at {location} after {elapsedSeconds} seconds");
        }
    }

    public static void DumpDiagnostics()
    {
        if (!IsEnabled)
        {
            return;
        }

        // Only output to console if verbosity allows
        if (_verbosityService != null && _verbosityService.HideTestOutput)
        {
            return;
        }

        lock (_lock)
        {
            Console.Error.WriteLine("[TUnit] Discovery Diagnostics:");
            Console.Error.WriteLine($"Total events: {_events.Count}");

            foreach (var evt in _events.OrderBy(e => e.Timestamp))
            {
                Console.Error.WriteLine($"  [{evt.Timestamp:HH:mm:ss.fff}] Thread {evt.ThreadId}: {evt.EventName} - {evt.Details}");
            }

            // Analyze for potential issues
            var dataSourceStarts = _events.Where(e => e.EventName == "DataSourceStart").ToList();
            var dataSourceEnds = _events.Where(e => e.EventName == "DataSourceEnd").ToList();

            if (dataSourceStarts.Count > dataSourceEnds.Count)
            {
                Console.Error.WriteLine($"[TUnit] WARNING: {dataSourceStarts.Count - dataSourceEnds.Count} data sources did not complete!");
            }

            var largeExpansions = _events
                .Where(e => e.EventName == "TestExpansion")
                .Where(e =>
                {
                    var match = System.Text.RegularExpressions.Regex.Match(e.Details, @"Combinations: (\d+)");
                    return match.Success && int.Parse(match.Groups[1].Value) > 1000;
                })
                .ToList();

            if (largeExpansions.Any())
            {
                Console.Error.WriteLine($"[TUnit] WARNING: {largeExpansions.Count} tests generated over 1000 combinations!");
            }
        }
    }

    private record DiscoveryEvent
    {
        public DateTime Timestamp { get; init; }
        public string EventName { get; init; } = "";
        public string Details { get; init; } = "";
        public int ThreadId { get; init; }
    }
}
