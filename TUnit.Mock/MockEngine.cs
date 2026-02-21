using TUnit.Mock.Exceptions;
using TUnit.Mock.Setup;
using TUnit.Mock.Setup.Behaviors;
using TUnit.Mock.Verification;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace TUnit.Mock;

/// <summary>
/// Provides a globally unique sequence counter for ordering calls across all mock types.
/// </summary>
internal static class MockCallSequence
{
    private static long _counter;

    internal static long Next() => Interlocked.Increment(ref _counter);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MockEngine<T> where T : class
{
    private readonly List<MethodSetup> _setups = new();
    private readonly ReaderWriterLockSlim _setupLock = new();
    private readonly ConcurrentQueue<CallRecord> _callHistory = new();

    public MockBehavior Behavior { get; }

    public MockEngine(MockBehavior behavior)
    {
        Behavior = behavior;
    }

    /// <summary>
    /// Registers a new setup. Thread-safe via writer lock.
    /// </summary>
    public void AddSetup(MethodSetup setup)
    {
        _setupLock.EnterWriteLock();
        try
        {
            _setups.Add(setup);
        }
        finally
        {
            _setupLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Handles a void method call. Records the call and executes matching setup behavior.
    /// </summary>
    public void HandleCall(int memberId, string memberName, object?[] args)
    {
        RecordCall(memberId, memberName, args);

        var (setupFound, behavior) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            behavior.Execute(args);
            return;
        }

        // A matching setup with no explicit behavior means "allow this call" (e.g., void setup with no callback)
        if (setupFound)
        {
            return;
        }

        if (Behavior == MockBehavior.Strict)
        {
            var callDesc = FormatCall(memberName, args);
            throw new MockStrictBehaviorException(callDesc);
        }
    }

    /// <summary>
    /// Handles a method call with a return value. Records the call, executes matching setup,
    /// or returns default/throws for strict mode.
    /// </summary>
    public TReturn HandleCallWithReturn<TReturn>(int memberId, string memberName, object?[] args, TReturn defaultValue)
    {
        RecordCall(memberId, memberName, args);

        var (setupFound, behavior) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            var result = behavior.Execute(args);
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            return defaultValue;
        }

        // A matching setup with no explicit behavior returns the default value
        if (setupFound)
        {
            return defaultValue;
        }

        if (Behavior == MockBehavior.Strict)
        {
            var callDesc = FormatCall(memberName, args);
            throw new MockStrictBehaviorException(callDesc);
        }

        return defaultValue;
    }

    /// <summary>
    /// Handles a void method call for a partial mock virtual method.
    /// Records the call, executes matching setup behavior if found.
    /// Returns true if a setup was found (caller should NOT call base), false otherwise (caller should call base).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall(int memberId, string memberName, object?[] args)
    {
        RecordCall(memberId, memberName, args);

        var (setupFound, behavior) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            behavior.Execute(args);
            return true;
        }

        return setupFound;
    }

    /// <summary>
    /// Handles a method call with a return value for a partial mock virtual method.
    /// Records the call, executes matching setup if found.
    /// Returns true if a setup was found (result is set), false otherwise (caller should call base).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn>(int memberId, string memberName, object?[] args, TReturn defaultValue, out TReturn result)
    {
        RecordCall(memberId, memberName, args);

        var (setupFound, behavior) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            var behaviorResult = behavior.Execute(args);
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else result = defaultValue;
            return true;
        }

        if (setupFound)
        {
            result = defaultValue;
            return true;
        }

        result = defaultValue;
        return false;
    }

    /// <summary>
    /// Gets all recorded calls for a specific member, optionally filtered by matchers.
    /// </summary>
    public IReadOnlyList<CallRecord> GetCallsFor(int memberId)
    {
        var result = new List<CallRecord>();
        foreach (var record in _callHistory)
        {
            if (record.MemberId == memberId)
            {
                result.Add(record);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets all recorded calls.
    /// </summary>
    public IReadOnlyList<CallRecord> GetAllCalls()
    {
        return _callHistory.ToArray();
    }

    /// <summary>
    /// Clears all setups and call history.
    /// </summary>
    public void Reset()
    {
        _setupLock.EnterWriteLock();
        try
        {
            _setups.Clear();
        }
        finally
        {
            _setupLock.ExitWriteLock();
        }

        // Drain the queue
        while (_callHistory.TryDequeue(out _)) { }
    }

    private void RecordCall(int memberId, string memberName, object?[] args)
    {
        var seq = MockCallSequence.Next();
        _callHistory.Enqueue(new CallRecord(memberId, memberName, args, DateTime.UtcNow, seq));
    }

    private (bool SetupFound, IBehavior? Behavior) FindMatchingSetup(int memberId, object?[] args)
    {
        _setupLock.EnterReadLock();
        try
        {
            // Iterate last-added-first to implement "last wins" semantics
            for (int i = _setups.Count - 1; i >= 0; i--)
            {
                var setup = _setups[i];
                if (setup.MemberId == memberId && setup.Matches(args))
                {
                    return (true, setup.GetNextBehavior());
                }
            }
        }
        finally
        {
            _setupLock.ExitReadLock();
        }

        return (false, null);
    }

    private static string FormatCall(string memberName, object?[] args)
    {
        var formattedArgs = string.Join(", ", args.Select(a => a?.ToString() ?? "null"));
        return $"{memberName}({formattedArgs})";
    }
}
