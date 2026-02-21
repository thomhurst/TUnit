using System.ComponentModel;
using TUnit.Mock.Arguments;
using TUnit.Mock.Setup.Behaviors;

namespace TUnit.Mock.Setup;

/// <summary>
/// Stores setup configuration for a single method. Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MethodSetup
{
    private readonly IArgumentMatcher[] _matchers;
    private readonly System.Threading.Lock _behaviorLock = new();
    private readonly List<IBehavior> _behaviors = new();
    private readonly List<EventRaiseInfo> _eventRaises = new();
    private int _callIndex;

    public int MemberId { get; }

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

    public IBehavior? GetNextBehavior()
    {
        lock (_behaviorLock)
        {
            if (_behaviors.Count == 0)
                return null;

            var index = Interlocked.Increment(ref _callIndex) - 1;
            // Clamp to last behavior (last one repeats)
            return _behaviors[Math.Min(index, _behaviors.Count - 1)];
        }
    }
}
