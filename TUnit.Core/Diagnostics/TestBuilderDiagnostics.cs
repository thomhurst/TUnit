using System.Diagnostics;
using System.Text;

namespace TUnit.Core.Diagnostics;

/// <summary>
/// Provides diagnostic capabilities for TestBuilder to help with debugging and performance analysis.
/// </summary>
public class TestBuilderDiagnostics
{
    private readonly bool _isEnabled;
    private readonly TextWriter _output;
    private readonly Stopwatch _stopwatch;
    private readonly List<DiagnosticEntry> _entries;
    
    public TestBuilderDiagnostics(bool isEnabled = false, TextWriter? output = null)
    {
        _isEnabled = isEnabled || IsDebugModeEnabled();
        _output = output ?? Console.Out;
        _stopwatch = new Stopwatch();
        _entries = new List<DiagnosticEntry>();
    }
    
    /// <summary>
    /// Gets whether diagnostics are enabled.
    /// </summary>
    public bool IsEnabled => _isEnabled;
    
    /// <summary>
    /// Gets all diagnostic entries collected.
    /// </summary>
    public IReadOnlyList<DiagnosticEntry> Entries => _entries;
    
    /// <summary>
    /// Starts a new diagnostic scope.
    /// </summary>
    public IDisposable BeginScope(string scopeName, params (string Key, object? Value)[] properties)
    {
        if (!_isEnabled) return new NoOpScope();
        
        var entry = new DiagnosticEntry
        {
            Type = DiagnosticType.ScopeStart,
            Message = scopeName,
            Timestamp = DateTime.UtcNow,
            Properties = properties.ToDictionary(p => p.Key, p => p.Value)
        };
        
        _entries.Add(entry);
        WriteEntry(entry);
        
        return new DiagnosticScope(this, scopeName, _stopwatch.ElapsedMilliseconds);
    }
    
    /// <summary>
    /// Logs a diagnostic message.
    /// </summary>
    public void Log(string message, DiagnosticLevel level = DiagnosticLevel.Info, params (string Key, object? Value)[] properties)
    {
        if (!_isEnabled) return;
        
        var entry = new DiagnosticEntry
        {
            Type = DiagnosticType.Message,
            Level = level,
            Message = message,
            Timestamp = DateTime.UtcNow,
            Properties = properties.ToDictionary(p => p.Key, p => p.Value)
        };
        
        _entries.Add(entry);
        WriteEntry(entry);
    }
    
    /// <summary>
    /// Logs test metadata information.
    /// </summary>
    public void LogTestMetadata(TestMetadata metadata)
    {
        if (!_isEnabled) return;
        
        Log($"Processing TestMetadata: {metadata.TestIdTemplate}", DiagnosticLevel.Info,
            ("TestClass", metadata.TestClassType.Name),
            ("TestMethod", metadata.TestMethod.Name),
            ("ClassDataSources", metadata.ClassDataSources.Count),
            ("MethodDataSources", metadata.MethodDataSources.Count),
            ("PropertyDataSources", metadata.PropertyDataSources.Count),
            ("RepeatCount", metadata.RepeatCount),
            ("IsAsync", metadata.IsAsync),
            ("IsSkipped", metadata.IsSkipped));
    }
    
    /// <summary>
    /// Logs data source enumeration details.
    /// </summary>
    public void LogDataSourceEnumeration(string sourceName, int itemCount, TimeSpan elapsed)
    {
        if (!_isEnabled) return;
        
        Log($"Enumerated data source: {sourceName}", DiagnosticLevel.Debug,
            ("ItemCount", itemCount),
            ("ElapsedMs", elapsed.TotalMilliseconds));
    }
    
    /// <summary>
    /// Logs test combination generation.
    /// </summary>
    public void LogCombinationGeneration(int classDataCount, int methodDataCount, int totalCombinations)
    {
        if (!_isEnabled) return;
        
        Log($"Generated test combinations", DiagnosticLevel.Info,
            ("ClassDataCount", classDataCount),
            ("MethodDataCount", methodDataCount),
            ("TotalCombinations", totalCombinations));
    }
    
    /// <summary>
    /// Logs tuple unwrapping operations.
    /// </summary>
    public void LogTupleUnwrapping(Type tupleType, int resultCount)
    {
        if (!_isEnabled) return;
        
        Log($"Unwrapped tuple", DiagnosticLevel.Debug,
            ("TupleType", tupleType.Name),
            ("ResultCount", resultCount));
    }
    
    /// <summary>
    /// Logs errors during test building.
    /// </summary>
    public void LogError(string context, Exception exception)
    {
        if (!_isEnabled) return;
        
        Log($"Error in {context}: {exception.Message}", DiagnosticLevel.Error,
            ("ExceptionType", exception.GetType().Name),
            ("StackTrace", exception.StackTrace));
    }
    
    /// <summary>
    /// Generates a summary report of the diagnostic data.
    /// </summary>
    public string GenerateSummaryReport()
    {
        if (!_isEnabled || !_entries.Any())
            return "No diagnostic data available.";
        
        var sb = new StringBuilder();
        sb.AppendLine("=== TestBuilder Diagnostic Summary ===");
        
        // Group by scope
        var scopes = new Stack<(string Name, long StartTime)>();
        var scopeStats = new Dictionary<string, ScopeStatistics>();
        
        foreach (var entry in _entries)
        {
            if (entry.Type == DiagnosticType.ScopeStart)
            {
                scopes.Push((entry.Message, entry.ElapsedMs ?? 0));
            }
            else if (entry.Type == DiagnosticType.ScopeEnd && scopes.Any())
            {
                var (name, startTime) = scopes.Pop();
                var duration = (entry.ElapsedMs ?? 0) - startTime;
                
                if (!scopeStats.ContainsKey(name))
                    scopeStats[name] = new ScopeStatistics { Name = name };
                
                scopeStats[name].Count++;
                scopeStats[name].TotalMs += duration;
                scopeStats[name].MinMs = Math.Min(scopeStats[name].MinMs, duration);
                scopeStats[name].MaxMs = Math.Max(scopeStats[name].MaxMs, duration);
            }
        }
        
        // Write scope statistics
        sb.AppendLine("\n## Scope Performance:");
        foreach (var stat in scopeStats.Values.OrderByDescending(s => s.TotalMs))
        {
            sb.AppendLine($"- {stat.Name}:");
            sb.AppendLine($"  Count: {stat.Count}");
            sb.AppendLine($"  Total: {stat.TotalMs:F2}ms");
            sb.AppendLine($"  Average: {stat.TotalMs / stat.Count:F2}ms");
            sb.AppendLine($"  Min: {stat.MinMs:F2}ms, Max: {stat.MaxMs:F2}ms");
        }
        
        // Write error summary
        var errors = _entries.Where(e => e.Level == DiagnosticLevel.Error).ToList();
        if (errors.Any())
        {
            sb.AppendLine($"\n## Errors ({errors.Count}):");
            foreach (var error in errors)
            {
                sb.AppendLine($"- {error.Message}");
            }
        }
        
        // Write statistics
        var totalTime = _entries.LastOrDefault()?.ElapsedMs ?? 0;
        sb.AppendLine($"\n## Overall Statistics:");
        sb.AppendLine($"- Total Time: {totalTime:F2}ms");
        sb.AppendLine($"- Total Entries: {_entries.Count}");
        
        return sb.ToString();
    }
    
    private void WriteEntry(DiagnosticEntry entry)
    {
        var prefix = entry.Type switch
        {
            DiagnosticType.ScopeStart => ">>>",
            DiagnosticType.ScopeEnd => "<<<",
            _ => "   "
        };
        
        var level = entry.Level switch
        {
            DiagnosticLevel.Debug => "DBG",
            DiagnosticLevel.Info => "INF",
            DiagnosticLevel.Warning => "WRN",
            DiagnosticLevel.Error => "ERR",
            _ => "   "
        };
        
        var message = $"{prefix} [{entry.Timestamp:HH:mm:ss.fff}] [{level}] {entry.Message}";
        
        if (entry.Properties.Any())
        {
            var props = string.Join(", ", entry.Properties.Select(p => $"{p.Key}={p.Value}"));
            message += $" ({props})";
        }
        
        _output.WriteLine(message);
    }
    
    private static bool IsDebugModeEnabled()
    {
        return Environment.GetEnvironmentVariable("TUNIT_TESTBUILDER_DIAGNOSTICS") == "true" ||
               Debugger.IsAttached;
    }
    
    private void EndScope(string scopeName, long startTime)
    {
        if (!_isEnabled) return;
        
        var entry = new DiagnosticEntry
        {
            Type = DiagnosticType.ScopeEnd,
            Message = scopeName,
            Timestamp = DateTime.UtcNow,
            ElapsedMs = _stopwatch.ElapsedMilliseconds,
            Properties = new Dictionary<string, object?>
            {
                ["DurationMs"] = _stopwatch.ElapsedMilliseconds - startTime
            }
        };
        
        _entries.Add(entry);
        WriteEntry(entry);
    }
    
    private class DiagnosticScope : IDisposable
    {
        private readonly TestBuilderDiagnostics _diagnostics;
        private readonly string _scopeName;
        private readonly long _startTime;
        
        public DiagnosticScope(TestBuilderDiagnostics diagnostics, string scopeName, long startTime)
        {
            _diagnostics = diagnostics;
            _scopeName = scopeName;
            _startTime = startTime;
        }
        
        public void Dispose()
        {
            _diagnostics.EndScope(_scopeName, _startTime);
        }
    }
    
    private class NoOpScope : IDisposable
    {
        public void Dispose() { }
    }
    
    private class ScopeStatistics
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public long TotalMs { get; set; }
        public long MinMs { get; set; } = long.MaxValue;
        public long MaxMs { get; set; }
    }
}

/// <summary>
/// Represents a single diagnostic entry.
/// </summary>
public class DiagnosticEntry
{
    public DiagnosticType Type { get; set; }
    public DiagnosticLevel Level { get; set; }
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public long? ElapsedMs { get; set; }
    public Dictionary<string, object?> Properties { get; set; } = new();
}

/// <summary>
/// Type of diagnostic entry.
/// </summary>
public enum DiagnosticType
{
    Message,
    ScopeStart,
    ScopeEnd
}

/// <summary>
/// Diagnostic message level.
/// </summary>
public enum DiagnosticLevel
{
    Debug,
    Info,
    Warning,
    Error
}