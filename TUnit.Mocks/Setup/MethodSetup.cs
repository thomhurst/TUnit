using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Setup.Behaviors;

namespace TUnit.Mocks.Setup;

/// <summary>
/// Stores setup configuration for a single method. Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MethodSetup
{
    private readonly IArgumentMatcher[] _matchers;
    private Lock? _behaviorLock;
    private List<IBehavior>? _behaviors;
    private List<EventRaiseInfo>? _eventRaises;
    private EventRaiseInfo[]? _eventRaisesSnapshot;
    private Dictionary<int, object?>? _outRefAssignments;
    private int _callIndex;

    private Lock EnsureBehaviorLock() => LazyInitializer.EnsureInitialized(ref _behaviorLock)!;

    public int MemberId { get; }

    /// <summary>
    /// If non-null, this setup only matches when the engine's current state equals this value.
    /// Used for state machine mocking.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? RequiredState { get; set; }

    /// <summary>
    /// If non-null, the engine transitions to this state after the behavior executes.
    /// Used for state machine mocking.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? TransitionTarget { get; set; }

    /// <summary>
    /// The number of times this setup has been matched and invoked.
    /// Used by <see cref="Mock{T}.VerifyAll"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int InvokeCount => _invokeCount;
    private int _invokeCount;

    /// <summary>
    /// Describes the setup for error messages.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string MemberName { get; }

    public MethodSetup(int memberId, IArgumentMatcher[] matchers, string memberName = "")
    {
        MemberId = memberId;
        _matchers = matchers;
        MemberName = memberName;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void IncrementInvokeCount() => Interlocked.Increment(ref _invokeCount);

    public void AddBehavior(IBehavior behavior)
    {
        lock (EnsureBehaviorLock())
        {
            var list = _behaviors ??= new();
            list.Add(behavior);
        }
    }

    public bool Matches(object?[] actualArgs)
    {
        if (actualArgs.Length != _matchers.Length)
            return false;

        for (int i = 0; i < _matchers.Length; i++)
        {
            if (!_matchers[i].Matches(actualArgs[i]))
                return false;
        }
        return true;
    }

    public void AddEventRaise(EventRaiseInfo raiseInfo)
    {
        lock (EnsureBehaviorLock())
        {
            var list = _eventRaises ??= new();
            list.Add(raiseInfo);
            _eventRaisesSnapshot = null;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IReadOnlyList<EventRaiseInfo> GetEventRaises()
    {
        if (Volatile.Read(ref _eventRaises) is null)
        {
            return [];
        }

        if (Volatile.Read(ref _eventRaisesSnapshot) is { } snapshot)
        {
            return snapshot;
        }

        lock (_behaviorLock!)
        {
            return _eventRaisesSnapshot = _eventRaises!.ToArray();
        }
    }

    /// <summary>
    /// Applies deferred captures to all CapturingMatcher instances after a full match is confirmed.
    /// Called by <see cref="MockEngine{T}"/> after all matchers have passed.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyCaptures(object?[] args)
    {
        for (int i = 0; i < _matchers.Length && i < args.Length; i++)
        {
            if (_matchers[i] is ICapturingMatcher capturing)
            {
                capturing.ApplyCapture(args[i]);
            }
        }
    }

    /// <summary>
    /// Sets the value to assign to an out or ref parameter when this setup matches.
    /// </summary>
    /// <param name="paramIndex">The zero-based index of the parameter in the full method signature.</param>
    /// <param name="value">The value to assign.</param>
    public void SetOutRefValue(int paramIndex, object? value)
    {
        lock (EnsureBehaviorLock())
        {
            _outRefAssignments ??= new Dictionary<int, object?>();
            _outRefAssignments[paramIndex] = value;
        }
    }

    /// <summary>
    /// Gets the out/ref parameter assignments, if any.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<int, object?>? OutRefAssignments
    {
        get
        {
            var lck = Volatile.Read(ref _behaviorLock);
            if (lck is null)
            {
                return Volatile.Read(ref _outRefAssignments);
            }

            lock (lck)
            {
                return _outRefAssignments;
            }
        }
    }

    /// <summary>
    /// Returns human-readable descriptions of all argument matchers, for diagnostics.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string[] GetMatcherDescriptions()
    {
        var descriptions = new string[_matchers.Length];
        for (int i = 0; i < _matchers.Length; i++)
        {
            descriptions[i] = _matchers[i].Describe();
        }
        return descriptions;
    }

    public IBehavior? GetNextBehavior()
    {
        if (Volatile.Read(ref _behaviors) is null)
        {
            return null;
        }

        lock (_behaviorLock!)
        {
            if (_behaviors is not { Count: > 0 } behaviors)
            {
                return null;
            }

            var index = _callIndex;
            if (_callIndex < int.MaxValue) _callIndex++;
            // Clamp to last behavior (last one repeats)
            return behaviors[Math.Min(index, behaviors.Count - 1)];
        }
    }
}
