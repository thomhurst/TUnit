using Microsoft.Extensions.Logging;
using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Logging;

/// <summary>
/// Fluent verification builder for mock logger entries.
/// </summary>
public sealed class LogVerification
{
    private readonly IReadOnlyList<LogEntry> _entries;
    private LogLevel? _level;
    private string? _messageContains;
    private string? _messageEquals;
    private Type? _exceptionType;

    internal LogVerification(IReadOnlyList<LogEntry> entries)
    {
        _entries = entries;
    }

    /// <summary>Filter to entries at the specified log level.</summary>
    public LogVerification AtLevel(LogLevel level)
    {
        _level = level;
        return this;
    }

    /// <summary>Filter to entries whose message contains the specified text.</summary>
    public LogVerification ContainingMessage(string text)
    {
        _messageContains = text;
        return this;
    }

    /// <summary>Filter to entries whose message equals the specified text exactly.</summary>
    public LogVerification WithMessage(string message)
    {
        _messageEquals = message;
        return this;
    }

    /// <summary>Filter to entries that have an exception of the specified type.</summary>
    public LogVerification WithException<TException>() where TException : Exception
    {
        _exceptionType = typeof(TException);
        return this;
    }

    /// <summary>Filter to entries that have an exception of the specified type.</summary>
    public LogVerification WithException(Type exceptionType)
    {
        _exceptionType = exceptionType;
        return this;
    }

    /// <summary>Verify matching entries were logged the expected number of times.</summary>
    public void WasCalled(Times times)
    {
        var matching = GetMatchingEntries();
        if (!times.Matches(matching.Count))
        {
            var description = BuildDescription();
            throw new MockVerificationException(
                $"Log({description})",
                times,
                matching.Count,
                matching.Select(e => e.ToString()).ToList(),
                null);
        }
    }

    /// <summary>Verify no matching entries were logged.</summary>
    public void WasNeverCalled()
    {
        WasCalled(Times.Never);
    }

    /// <summary>Get all entries matching the current filter criteria.</summary>
    public IReadOnlyList<LogEntry> GetMatchingEntries()
    {
        var results = new List<LogEntry>();
        foreach (var entry in _entries)
        {
            if (_level.HasValue && entry.LogLevel != _level.Value) continue;
            if (_messageContains != null && !entry.Message.Contains(_messageContains)) continue;
            if (_messageEquals != null && entry.Message != _messageEquals) continue;
            if (_exceptionType != null && (entry.Exception == null || !_exceptionType.IsInstanceOfType(entry.Exception))) continue;
            results.Add(entry);
        }
        return results;
    }

    private string BuildDescription()
    {
        var parts = new List<string>();
        if (_level.HasValue) parts.Add($"level={_level.Value}");
        if (_messageContains != null) parts.Add($"contains=\"{_messageContains}\"");
        if (_messageEquals != null) parts.Add($"message=\"{_messageEquals}\"");
        if (_exceptionType != null) parts.Add($"exception={_exceptionType.Name}");
        return parts.Count > 0 ? string.Join(", ", parts) : "any";
    }
}
