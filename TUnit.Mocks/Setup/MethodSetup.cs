using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    private Lock BehaviorLock => _behaviorLock ?? EnsureBehaviorLock();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Lock EnsureBehaviorLock()
    {
        Interlocked.CompareExchange(ref _behaviorLock, new Lock(), null);
        return _behaviorLock!;
    }
    /// <summary>Fast path for the common single-behavior case. Avoids list + lock on read.</summary>
    private IBehavior? _singleBehavior;
    private List<IBehavior>? _behaviors;
    private List<EventRaiseInfo>? _eventRaises;
    private EventRaiseInfo[]? _eventRaisesSnapshot;
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
        // Lock-free fast path: CAS for the common single-behavior case.
        // Avoids allocating the Lock object entirely when only one behavior is registered.
        if (Volatile.Read(ref _behaviors) is null
            && Interlocked.CompareExchange(ref _singleBehavior, behavior, null) is null)
        {
            // Double-check: if a concurrent AddBehaviorSlow promoted to list between our
            // _behaviors read and the CAS, fall through to slow path to reconcile.
            if (Volatile.Read(ref _behaviors) is null)
            {
                return;
            }
        }

        AddBehaviorSlow(behavior);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddBehaviorSlow(IBehavior behavior)
    {
        lock (BehaviorLock)
        {
            // Promote to list on second behavior. Write _behaviors before clearing
            // _singleBehavior so that a lock-free reader in GetNextBehavior that sees
            // _singleBehavior == null will also see the updated _behaviors reference.
            if (Volatile.Read(ref _behaviors) is null)
            {
                var current = Volatile.Read(ref _singleBehavior);
                Volatile.Write(ref _behaviors, current is not null ? [current] : []);
            }

            _behaviors!.Add(behavior);
            Volatile.Write(ref _singleBehavior, null);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchSingle<T>(IArgumentMatcher matcher, T value)
    {
        if (matcher is IArgumentMatcher<T> typed)
            return typed.Matches(value);
        return matcher.Matches(value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Matches<T1>(T1 arg1)
    {
        if (_matchers.Length != 1) return false;
        return MatchSingle(_matchers[0], arg1);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Matches<T1, T2>(T1 arg1, T2 arg2)
    {
        if (_matchers.Length != 2) return false;
        return MatchSingle(_matchers[0], arg1) && MatchSingle(_matchers[1], arg2);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Matches<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
    {
        if (_matchers.Length != 3) return false;
        return MatchSingle(_matchers[0], arg1) && MatchSingle(_matchers[1], arg2) && MatchSingle(_matchers[2], arg3);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Matches<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (_matchers.Length != 4) return false;
        return MatchSingle(_matchers[0], arg1) && MatchSingle(_matchers[1], arg2) && MatchSingle(_matchers[2], arg3) && MatchSingle(_matchers[3], arg4);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Matches<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (_matchers.Length != 5) return false;
        return MatchSingle(_matchers[0], arg1) && MatchSingle(_matchers[1], arg2) && MatchSingle(_matchers[2], arg3) && MatchSingle(_matchers[3], arg4) && MatchSingle(_matchers[4], arg5);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Matches<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (_matchers.Length != 6) return false;
        return MatchSingle(_matchers[0], arg1) && MatchSingle(_matchers[1], arg2) && MatchSingle(_matchers[2], arg3) && MatchSingle(_matchers[3], arg4) && MatchSingle(_matchers[4], arg5) && MatchSingle(_matchers[5], arg6);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Matches<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (_matchers.Length != 7) return false;
        return MatchSingle(_matchers[0], arg1) && MatchSingle(_matchers[1], arg2) && MatchSingle(_matchers[2], arg3) && MatchSingle(_matchers[3], arg4) && MatchSingle(_matchers[4], arg5) && MatchSingle(_matchers[5], arg6) && MatchSingle(_matchers[6], arg7);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Matches<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (_matchers.Length != 8) return false;
        return MatchSingle(_matchers[0], arg1) && MatchSingle(_matchers[1], arg2) && MatchSingle(_matchers[2], arg3) && MatchSingle(_matchers[3], arg4) && MatchSingle(_matchers[4], arg5) && MatchSingle(_matchers[5], arg6) && MatchSingle(_matchers[6], arg7) && MatchSingle(_matchers[7], arg8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CaptureSingle<T>(IArgumentMatcher matcher, T value)
    {
        if (matcher is ICapturingMatcher<T> typed)
            typed.ApplyCapture(value);
        else if (matcher is ICapturingMatcher untyped)
            untyped.ApplyCapture(value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyCaptures<T1>(T1 arg1)
    {
        if (_matchers.Length >= 1) CaptureSingle(_matchers[0], arg1);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyCaptures<T1, T2>(T1 arg1, T2 arg2)
    {
        if (_matchers.Length >= 1) CaptureSingle(_matchers[0], arg1);
        if (_matchers.Length >= 2) CaptureSingle(_matchers[1], arg2);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyCaptures<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
    {
        if (_matchers.Length >= 1) CaptureSingle(_matchers[0], arg1);
        if (_matchers.Length >= 2) CaptureSingle(_matchers[1], arg2);
        if (_matchers.Length >= 3) CaptureSingle(_matchers[2], arg3);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyCaptures<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (_matchers.Length >= 1) CaptureSingle(_matchers[0], arg1);
        if (_matchers.Length >= 2) CaptureSingle(_matchers[1], arg2);
        if (_matchers.Length >= 3) CaptureSingle(_matchers[2], arg3);
        if (_matchers.Length >= 4) CaptureSingle(_matchers[3], arg4);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyCaptures<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (_matchers.Length >= 1) CaptureSingle(_matchers[0], arg1);
        if (_matchers.Length >= 2) CaptureSingle(_matchers[1], arg2);
        if (_matchers.Length >= 3) CaptureSingle(_matchers[2], arg3);
        if (_matchers.Length >= 4) CaptureSingle(_matchers[3], arg4);
        if (_matchers.Length >= 5) CaptureSingle(_matchers[4], arg5);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyCaptures<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (_matchers.Length >= 1) CaptureSingle(_matchers[0], arg1);
        if (_matchers.Length >= 2) CaptureSingle(_matchers[1], arg2);
        if (_matchers.Length >= 3) CaptureSingle(_matchers[2], arg3);
        if (_matchers.Length >= 4) CaptureSingle(_matchers[3], arg4);
        if (_matchers.Length >= 5) CaptureSingle(_matchers[4], arg5);
        if (_matchers.Length >= 6) CaptureSingle(_matchers[5], arg6);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyCaptures<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (_matchers.Length >= 1) CaptureSingle(_matchers[0], arg1);
        if (_matchers.Length >= 2) CaptureSingle(_matchers[1], arg2);
        if (_matchers.Length >= 3) CaptureSingle(_matchers[2], arg3);
        if (_matchers.Length >= 4) CaptureSingle(_matchers[3], arg4);
        if (_matchers.Length >= 5) CaptureSingle(_matchers[4], arg5);
        if (_matchers.Length >= 6) CaptureSingle(_matchers[5], arg6);
        if (_matchers.Length >= 7) CaptureSingle(_matchers[6], arg7);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyCaptures<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (_matchers.Length >= 1) CaptureSingle(_matchers[0], arg1);
        if (_matchers.Length >= 2) CaptureSingle(_matchers[1], arg2);
        if (_matchers.Length >= 3) CaptureSingle(_matchers[2], arg3);
        if (_matchers.Length >= 4) CaptureSingle(_matchers[3], arg4);
        if (_matchers.Length >= 5) CaptureSingle(_matchers[4], arg5);
        if (_matchers.Length >= 6) CaptureSingle(_matchers[5], arg6);
        if (_matchers.Length >= 7) CaptureSingle(_matchers[6], arg7);
        if (_matchers.Length >= 8) CaptureSingle(_matchers[7], arg8);
    }

    public void AddEventRaise(EventRaiseInfo raiseInfo)
    {
        lock (BehaviorLock)
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

        lock (BehaviorLock)
        {
            return _eventRaisesSnapshot ??= _eventRaises!.ToArray();
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
        lock (BehaviorLock)
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
            lock (BehaviorLock)
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
        // Fast path: single behavior (most common case — no lock needed)
        if (Volatile.Read(ref _singleBehavior) is { } single)
        {
            return single;
        }

        if (Volatile.Read(ref _behaviors) is null)
        {
            return null;
        }

        lock (BehaviorLock)
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
