using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Setup.Behaviors;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TUnit.Mocks;

public sealed partial class MockEngine<T> where T : class
{
    // ──────────────────────────────────────────────────────────────────────
    //  Behavior execution helpers — check IArgumentFreeBehavior, then
    //  ITypedBehavior<T...>, then fall back to store.ToArray().
    // ──────────────────────────────────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ExecuteBehavior<T1>(IBehavior b, in ArgumentStore<T1> store, T1 a1)
        => b is IArgumentFreeBehavior af ? af.Execute() : b is ITypedBehavior<T1> tb ? tb.Execute(a1) : b.Execute(store.ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ExecuteBehavior<T1, T2>(IBehavior b, in ArgumentStore<T1, T2> store, T1 a1, T2 a2)
        => b is IArgumentFreeBehavior af ? af.Execute() : b is ITypedBehavior<T1, T2> tb ? tb.Execute(a1, a2) : b.Execute(store.ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ExecuteBehavior<T1, T2, T3>(IBehavior b, in ArgumentStore<T1, T2, T3> store, T1 a1, T2 a2, T3 a3)
        => b is IArgumentFreeBehavior af ? af.Execute() : b is ITypedBehavior<T1, T2, T3> tb ? tb.Execute(a1, a2, a3) : b.Execute(store.ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ExecuteBehavior<T1, T2, T3, T4>(IBehavior b, in ArgumentStore<T1, T2, T3, T4> store, T1 a1, T2 a2, T3 a3, T4 a4)
        => b is IArgumentFreeBehavior af ? af.Execute() : b is ITypedBehavior<T1, T2, T3, T4> tb ? tb.Execute(a1, a2, a3, a4) : b.Execute(store.ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ExecuteBehavior<T1, T2, T3, T4, T5>(IBehavior b, in ArgumentStore<T1, T2, T3, T4, T5> store, T1 a1, T2 a2, T3 a3, T4 a4, T5 a5)
        => b is IArgumentFreeBehavior af ? af.Execute() : b is ITypedBehavior<T1, T2, T3, T4, T5> tb ? tb.Execute(a1, a2, a3, a4, a5) : b.Execute(store.ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ExecuteBehavior<T1, T2, T3, T4, T5, T6>(IBehavior b, in ArgumentStore<T1, T2, T3, T4, T5, T6> store, T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6)
        => b is IArgumentFreeBehavior af ? af.Execute() : b is ITypedBehavior<T1, T2, T3, T4, T5, T6> tb ? tb.Execute(a1, a2, a3, a4, a5, a6) : b.Execute(store.ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ExecuteBehavior<T1, T2, T3, T4, T5, T6, T7>(IBehavior b, in ArgumentStore<T1, T2, T3, T4, T5, T6, T7> store, T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7)
        => b is IArgumentFreeBehavior af ? af.Execute() : b is ITypedBehavior<T1, T2, T3, T4, T5, T6, T7> tb ? tb.Execute(a1, a2, a3, a4, a5, a6, a7) : b.Execute(store.ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ExecuteBehavior<T1, T2, T3, T4, T5, T6, T7, T8>(IBehavior b, in ArgumentStore<T1, T2, T3, T4, T5, T6, T7, T8> store, T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8)
        => b is IArgumentFreeBehavior af ? af.Execute() : b is ITypedBehavior<T1, T2, T3, T4, T5, T6, T7, T8> tb ? tb.Execute(a1, a2, a3, a4, a5, a6, a7, a8) : b.Execute(store.ToArray());
    // ──────────────────────────────────────────────────────────────────────
    //  Arity 1
    // ──────────────────────────────────────────────────────────────────────

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCall<T1>(int memberId, string memberName, T1 arg1)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1>(arg1);
        var callRecord = RecordCall(memberId, memberName, store);

        if (AutoTrackProperties && memberName.StartsWith("set_", StringComparison.Ordinal))
        {
            AutoTrackValues[memberName[4..]] = arg1;
        }

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return;

        callRecord.IsUnmatched = true;
        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1>(int memberId, string memberName, T1 arg1, TReturn defaultValue)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1>(arg1);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1);

        if (behavior is not null)
        {
            var result = ExecuteBehavior(behavior, store, arg1);
            if (result is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            if (result is RawReturn) return defaultValue;
            throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {result.GetType().Name}.");
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return defaultValue;

        callRecord.IsUnmatched = true;

        if (AutoTrackProperties && Volatile.Read(ref _autoTrackValues) is { } trackValues && memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            if (trackValues.TryGetValue(memberName[4..], out var trackedValue))
            {
                if (trackedValue is TReturn t) return t;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

#pragma warning disable IL3050, IL2026
        if (DefaultValueProvider is not null && DefaultValueProvider.CanProvide(typeof(TReturn)))
        {
            var customDefault = DefaultValueProvider.GetDefaultValue(typeof(TReturn));
            if (customDefault is TReturn typedCustom) return typedCustom;
            if (customDefault is null) return default(TReturn)!;
        }
#pragma warning restore IL3050, IL2026

        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            var autoMock = AutoMockCache.GetOrAdd(cacheKey, _ =>
            {
                MockRegistry.TryCreateAutoMock(typeof(TReturn), Behavior, out var m);
                return m;
            });
            if (autoMock is not null) return (TReturn)autoMock.ObjectInstance;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall<T1>(int memberId, string memberName, T1 arg1)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1>(arg1);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (!setupFound) callRecord.IsUnmatched = true;
        if (!setupFound && IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        return setupFound;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1>(int memberId, string memberName, T1 arg1, TReturn defaultValue, out TReturn result)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1>(arg1);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else if (behaviorResult is RawReturn) result = defaultValue;
            else throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {behaviorResult.GetType().Name}.");
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) { result = defaultValue; return true; }

        callRecord.IsUnmatched = true;
        if (IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        result = defaultValue;
        return false;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Arity 2
    // ──────────────────────────────────────────────────────────────────────

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCall<T1, T2>(int memberId, string memberName, T1 arg1, T2 arg2)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2>(arg1, arg2);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return;

        callRecord.IsUnmatched = true;
        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2>(int memberId, string memberName, T1 arg1, T2 arg2, TReturn defaultValue)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2>(arg1, arg2);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2);

        if (behavior is not null)
        {
            var result = ExecuteBehavior(behavior, store, arg1, arg2);
            if (result is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            if (result is RawReturn) return defaultValue;
            throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {result.GetType().Name}.");
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return defaultValue;

        callRecord.IsUnmatched = true;

        if (AutoTrackProperties && Volatile.Read(ref _autoTrackValues) is { } trackValues && memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            if (trackValues.TryGetValue(memberName[4..], out var trackedValue))
            {
                if (trackedValue is TReturn t) return t;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

#pragma warning disable IL3050, IL2026
        if (DefaultValueProvider is not null && DefaultValueProvider.CanProvide(typeof(TReturn)))
        {
            var customDefault = DefaultValueProvider.GetDefaultValue(typeof(TReturn));
            if (customDefault is TReturn typedCustom) return typedCustom;
            if (customDefault is null) return default(TReturn)!;
        }
#pragma warning restore IL3050, IL2026

        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            var autoMock = AutoMockCache.GetOrAdd(cacheKey, _ =>
            {
                MockRegistry.TryCreateAutoMock(typeof(TReturn), Behavior, out var m);
                return m;
            });
            if (autoMock is not null) return (TReturn)autoMock.ObjectInstance;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall<T1, T2>(int memberId, string memberName, T1 arg1, T2 arg2)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2>(arg1, arg2);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (!setupFound) callRecord.IsUnmatched = true;
        if (!setupFound && IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        return setupFound;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2>(int memberId, string memberName, T1 arg1, T2 arg2, TReturn defaultValue, out TReturn result)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2>(arg1, arg2);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else if (behaviorResult is RawReturn) result = defaultValue;
            else throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {behaviorResult.GetType().Name}.");
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) { result = defaultValue; return true; }

        callRecord.IsUnmatched = true;
        if (IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        result = defaultValue;
        return false;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Arity 3
    // ──────────────────────────────────────────────────────────────────────

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCall<T1, T2, T3>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3>(arg1, arg2, arg3);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return;

        callRecord.IsUnmatched = true;
        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, TReturn defaultValue)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3>(arg1, arg2, arg3);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3);

        if (behavior is not null)
        {
            var result = ExecuteBehavior(behavior, store, arg1, arg2, arg3);
            if (result is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            if (result is RawReturn) return defaultValue;
            throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {result.GetType().Name}.");
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return defaultValue;

        callRecord.IsUnmatched = true;

        if (AutoTrackProperties && Volatile.Read(ref _autoTrackValues) is { } trackValues && memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            if (trackValues.TryGetValue(memberName[4..], out var trackedValue))
            {
                if (trackedValue is TReturn t) return t;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

#pragma warning disable IL3050, IL2026
        if (DefaultValueProvider is not null && DefaultValueProvider.CanProvide(typeof(TReturn)))
        {
            var customDefault = DefaultValueProvider.GetDefaultValue(typeof(TReturn));
            if (customDefault is TReturn typedCustom) return typedCustom;
            if (customDefault is null) return default(TReturn)!;
        }
#pragma warning restore IL3050, IL2026

        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            var autoMock = AutoMockCache.GetOrAdd(cacheKey, _ =>
            {
                MockRegistry.TryCreateAutoMock(typeof(TReturn), Behavior, out var m);
                return m;
            });
            if (autoMock is not null) return (TReturn)autoMock.ObjectInstance;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall<T1, T2, T3>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3>(arg1, arg2, arg3);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (!setupFound) callRecord.IsUnmatched = true;
        if (!setupFound && IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        return setupFound;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, TReturn defaultValue, out TReturn result)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3>(arg1, arg2, arg3);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else if (behaviorResult is RawReturn) result = defaultValue;
            else throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {behaviorResult.GetType().Name}.");
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) { result = defaultValue; return true; }

        callRecord.IsUnmatched = true;
        if (IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        result = defaultValue;
        return false;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Arity 4
    // ──────────────────────────────────────────────────────────────────────

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCall<T1, T2, T3, T4>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4>(arg1, arg2, arg3, arg4);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return;

        callRecord.IsUnmatched = true;
        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, TReturn defaultValue)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4>(arg1, arg2, arg3, arg4);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4);

        if (behavior is not null)
        {
            var result = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4);
            if (result is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            if (result is RawReturn) return defaultValue;
            throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {result.GetType().Name}.");
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return defaultValue;

        callRecord.IsUnmatched = true;

        if (AutoTrackProperties && Volatile.Read(ref _autoTrackValues) is { } trackValues && memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            if (trackValues.TryGetValue(memberName[4..], out var trackedValue))
            {
                if (trackedValue is TReturn t) return t;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

#pragma warning disable IL3050, IL2026
        if (DefaultValueProvider is not null && DefaultValueProvider.CanProvide(typeof(TReturn)))
        {
            var customDefault = DefaultValueProvider.GetDefaultValue(typeof(TReturn));
            if (customDefault is TReturn typedCustom) return typedCustom;
            if (customDefault is null) return default(TReturn)!;
        }
#pragma warning restore IL3050, IL2026

        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            var autoMock = AutoMockCache.GetOrAdd(cacheKey, _ =>
            {
                MockRegistry.TryCreateAutoMock(typeof(TReturn), Behavior, out var m);
                return m;
            });
            if (autoMock is not null) return (TReturn)autoMock.ObjectInstance;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall<T1, T2, T3, T4>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4>(arg1, arg2, arg3, arg4);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (!setupFound) callRecord.IsUnmatched = true;
        if (!setupFound && IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        return setupFound;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, TReturn defaultValue, out TReturn result)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4>(arg1, arg2, arg3, arg4);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else if (behaviorResult is RawReturn) result = defaultValue;
            else throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {behaviorResult.GetType().Name}.");
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) { result = defaultValue; return true; }

        callRecord.IsUnmatched = true;
        if (IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        result = defaultValue;
        return false;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Arity 5
    // ──────────────────────────────────────────────────────────────────────

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCall<T1, T2, T3, T4, T5>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5>(arg1, arg2, arg3, arg4, arg5);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return;

        callRecord.IsUnmatched = true;
        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4, T5>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, TReturn defaultValue)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5>(arg1, arg2, arg3, arg4, arg5);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5);

        if (behavior is not null)
        {
            var result = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5);
            if (result is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            if (result is RawReturn) return defaultValue;
            throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {result.GetType().Name}.");
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return defaultValue;

        callRecord.IsUnmatched = true;

        if (AutoTrackProperties && Volatile.Read(ref _autoTrackValues) is { } trackValues && memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            if (trackValues.TryGetValue(memberName[4..], out var trackedValue))
            {
                if (trackedValue is TReturn t) return t;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

#pragma warning disable IL3050, IL2026
        if (DefaultValueProvider is not null && DefaultValueProvider.CanProvide(typeof(TReturn)))
        {
            var customDefault = DefaultValueProvider.GetDefaultValue(typeof(TReturn));
            if (customDefault is TReturn typedCustom) return typedCustom;
            if (customDefault is null) return default(TReturn)!;
        }
#pragma warning restore IL3050, IL2026

        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            var autoMock = AutoMockCache.GetOrAdd(cacheKey, _ =>
            {
                MockRegistry.TryCreateAutoMock(typeof(TReturn), Behavior, out var m);
                return m;
            });
            if (autoMock is not null) return (TReturn)autoMock.ObjectInstance;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall<T1, T2, T3, T4, T5>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5>(arg1, arg2, arg3, arg4, arg5);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (!setupFound) callRecord.IsUnmatched = true;
        if (!setupFound && IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        return setupFound;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4, T5>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, TReturn defaultValue, out TReturn result)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5>(arg1, arg2, arg3, arg4, arg5);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else if (behaviorResult is RawReturn) result = defaultValue;
            else throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {behaviorResult.GetType().Name}.");
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) { result = defaultValue; return true; }

        callRecord.IsUnmatched = true;
        if (IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        result = defaultValue;
        return false;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Arity 6
    // ──────────────────────────────────────────────────────────────────────

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCall<T1, T2, T3, T4, T5, T6>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6>(arg1, arg2, arg3, arg4, arg5, arg6);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return;

        callRecord.IsUnmatched = true;
        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, TReturn defaultValue)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6>(arg1, arg2, arg3, arg4, arg5, arg6);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6);

        if (behavior is not null)
        {
            var result = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6);
            if (result is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            if (result is RawReturn) return defaultValue;
            throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {result.GetType().Name}.");
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return defaultValue;

        callRecord.IsUnmatched = true;

        if (AutoTrackProperties && Volatile.Read(ref _autoTrackValues) is { } trackValues && memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            if (trackValues.TryGetValue(memberName[4..], out var trackedValue))
            {
                if (trackedValue is TReturn t) return t;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

#pragma warning disable IL3050, IL2026
        if (DefaultValueProvider is not null && DefaultValueProvider.CanProvide(typeof(TReturn)))
        {
            var customDefault = DefaultValueProvider.GetDefaultValue(typeof(TReturn));
            if (customDefault is TReturn typedCustom) return typedCustom;
            if (customDefault is null) return default(TReturn)!;
        }
#pragma warning restore IL3050, IL2026

        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            var autoMock = AutoMockCache.GetOrAdd(cacheKey, _ =>
            {
                MockRegistry.TryCreateAutoMock(typeof(TReturn), Behavior, out var m);
                return m;
            });
            if (autoMock is not null) return (TReturn)autoMock.ObjectInstance;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall<T1, T2, T3, T4, T5, T6>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6>(arg1, arg2, arg3, arg4, arg5, arg6);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (!setupFound) callRecord.IsUnmatched = true;
        if (!setupFound && IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        return setupFound;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, TReturn defaultValue, out TReturn result)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6>(arg1, arg2, arg3, arg4, arg5, arg6);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else if (behaviorResult is RawReturn) result = defaultValue;
            else throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {behaviorResult.GetType().Name}.");
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) { result = defaultValue; return true; }

        callRecord.IsUnmatched = true;
        if (IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        result = defaultValue;
        return false;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Arity 7
    // ──────────────────────────────────────────────────────────────────────

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCall<T1, T2, T3, T4, T5, T6, T7>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6, T7>(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return;

        callRecord.IsUnmatched = true;
        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6, T7>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, TReturn defaultValue)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6, T7>(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        if (behavior is not null)
        {
            var result = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            if (result is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            if (result is RawReturn) return defaultValue;
            throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {result.GetType().Name}.");
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return defaultValue;

        callRecord.IsUnmatched = true;

        if (AutoTrackProperties && Volatile.Read(ref _autoTrackValues) is { } trackValues && memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            if (trackValues.TryGetValue(memberName[4..], out var trackedValue))
            {
                if (trackedValue is TReturn t) return t;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

#pragma warning disable IL3050, IL2026
        if (DefaultValueProvider is not null && DefaultValueProvider.CanProvide(typeof(TReturn)))
        {
            var customDefault = DefaultValueProvider.GetDefaultValue(typeof(TReturn));
            if (customDefault is TReturn typedCustom) return typedCustom;
            if (customDefault is null) return default(TReturn)!;
        }
#pragma warning restore IL3050, IL2026

        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            var autoMock = AutoMockCache.GetOrAdd(cacheKey, _ =>
            {
                MockRegistry.TryCreateAutoMock(typeof(TReturn), Behavior, out var m);
                return m;
            });
            if (autoMock is not null) return (TReturn)autoMock.ObjectInstance;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall<T1, T2, T3, T4, T5, T6, T7>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6, T7>(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (!setupFound) callRecord.IsUnmatched = true;
        if (!setupFound && IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        return setupFound;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6, T7>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, TReturn defaultValue, out TReturn result)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6, T7>(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else if (behaviorResult is RawReturn) result = defaultValue;
            else throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {behaviorResult.GetType().Name}.");
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) { result = defaultValue; return true; }

        callRecord.IsUnmatched = true;
        if (IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        result = defaultValue;
        return false;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Arity 8
    // ──────────────────────────────────────────────────────────────────────

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCall<T1, T2, T3, T4, T5, T6, T7, T8>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6, T7, T8>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return;

        callRecord.IsUnmatched = true;
        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6, T7, T8>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, TReturn defaultValue)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6, T7, T8>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        if (behavior is not null)
        {
            var result = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            if (result is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            if (result is RawReturn) return defaultValue;
            throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {result.GetType().Name}.");
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) return defaultValue;

        callRecord.IsUnmatched = true;

        if (AutoTrackProperties && Volatile.Read(ref _autoTrackValues) is { } trackValues && memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            if (trackValues.TryGetValue(memberName[4..], out var trackedValue))
            {
                if (trackedValue is TReturn t) return t;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

#pragma warning disable IL3050, IL2026
        if (DefaultValueProvider is not null && DefaultValueProvider.CanProvide(typeof(TReturn)))
        {
            var customDefault = DefaultValueProvider.GetDefaultValue(typeof(TReturn));
            if (customDefault is TReturn typedCustom) return typedCustom;
            if (customDefault is null) return default(TReturn)!;
        }
#pragma warning restore IL3050, IL2026

        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            var autoMock = AutoMockCache.GetOrAdd(cacheKey, _ =>
            {
                MockRegistry.TryCreateAutoMock(typeof(TReturn), Behavior, out var m);
                return m;
            });
            if (autoMock is not null) return (TReturn)autoMock.ObjectInstance;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall<T1, T2, T3, T4, T5, T6, T7, T8>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6, T7, T8>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (!setupFound) callRecord.IsUnmatched = true;
        if (!setupFound && IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        return setupFound;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6, T7, T8>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, TReturn defaultValue, out TReturn result)
    {
        RawReturnContext.Clear();
        var store = new ArgumentStore<T1, T2, T3, T4, T5, T6, T7, T8>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        var callRecord = RecordCall(memberId, memberName, store);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        if (behavior is not null)
        {
            var behaviorResult = ExecuteBehavior(behavior, store, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            if (behaviorResult is RawReturn raw) RawReturnContext.Set(raw);
            try { ApplyMatchedSetup(matchedSetup); }
            catch { RawReturnContext.Clear(); throw; }
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else if (behaviorResult is RawReturn) result = defaultValue;
            else throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {behaviorResult.GetType().Name}.");
            return true;
        }

        ApplyMatchedSetup(matchedSetup);
        if (setupFound) { result = defaultValue; return true; }

        callRecord.IsUnmatched = true;
        if (IsWrapMock && Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }
        result = defaultValue;
        return false;
    }
}
