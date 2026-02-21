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
    private readonly object _behaviorLock = new();
    private readonly List<IBehavior> _behaviors = new();
    private readonly List<EventRaiseInfo> _eventRaises = new();
    private Dictionary<int, object?>? _outRefAssignments;
    private int _callIndex;

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
        lock (_behaviorLock)
        {
            _behaviors.Add(behavior);
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
        lock (_behaviorLock)
        {
            _eventRaises.Add(raiseInfo);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IReadOnlyList<EventRaiseInfo> GetEventRaises()
    {
        lock (_behaviorLock)
        {
            return _eventRaises.ToList();
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
        lock (_behaviorLock)
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
            lock (_behaviorLock)
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
        lock (_behaviorLock)
        {
            if (_behaviors.Count == 0)
                return null;

            var index = _callIndex++;
            // Clamp to last behavior (last one repeats)
            return _behaviors[Math.Min(index, _behaviors.Count - 1)];
        }
    }
}
