using System.ComponentModel;
using System.Threading;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Setup.Behaviors;

namespace TUnit.Mocks;

public sealed partial class MockEngine<T> where T : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1>(int memberId, string memberName, T1 arg1, TReturn defaultValue, Func<MockBehavior, IMock>? autoMockFactory)
    {
        if (autoMockFactory is null)
        {
            return HandleCallWithReturn(memberId, memberName, arg1, defaultValue);
        }

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
                if (trackedValue is TReturn typed) return typed;
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

        if (TryGetLooseAutoMockResult(memberName, autoMockFactory, out TReturn autoMockResult))
        {
            return autoMockResult;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1>(int memberId, string memberName, T1 arg1, TReturn defaultValue, out TReturn result, Func<MockBehavior, IMock>? autoMockFactory)
        => TryHandleCallWithReturn(memberId, memberName, arg1, defaultValue, out result);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2>(int memberId, string memberName, T1 arg1, T2 arg2, TReturn defaultValue, Func<MockBehavior, IMock>? autoMockFactory)
    {
        if (autoMockFactory is null)
        {
            return HandleCallWithReturn(memberId, memberName, arg1, arg2, defaultValue);
        }

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
                if (trackedValue is TReturn typed) return typed;
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

        if (TryGetLooseAutoMockResult(memberName, autoMockFactory, out TReturn autoMockResult))
        {
            return autoMockResult;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2>(int memberId, string memberName, T1 arg1, T2 arg2, TReturn defaultValue, out TReturn result, Func<MockBehavior, IMock>? autoMockFactory)
        => TryHandleCallWithReturn(memberId, memberName, arg1, arg2, defaultValue, out result);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, TReturn defaultValue, Func<MockBehavior, IMock>? autoMockFactory)
    {
        if (autoMockFactory is null)
        {
            return HandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, defaultValue);
        }

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
                if (trackedValue is TReturn typed) return typed;
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

        if (TryGetLooseAutoMockResult(memberName, autoMockFactory, out TReturn autoMockResult))
        {
            return autoMockResult;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, TReturn defaultValue, out TReturn result, Func<MockBehavior, IMock>? autoMockFactory)
        => TryHandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, defaultValue, out result);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, TReturn defaultValue, Func<MockBehavior, IMock>? autoMockFactory)
    {
        if (autoMockFactory is null)
        {
            return HandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, defaultValue);
        }

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
                if (trackedValue is TReturn typed) return typed;
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

        if (TryGetLooseAutoMockResult(memberName, autoMockFactory, out TReturn autoMockResult))
        {
            return autoMockResult;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, TReturn defaultValue, out TReturn result, Func<MockBehavior, IMock>? autoMockFactory)
        => TryHandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, defaultValue, out result);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4, T5>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, TReturn defaultValue, Func<MockBehavior, IMock>? autoMockFactory)
    {
        if (autoMockFactory is null)
        {
            return HandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, arg5, defaultValue);
        }

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
                if (trackedValue is TReturn typed) return typed;
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

        if (TryGetLooseAutoMockResult(memberName, autoMockFactory, out TReturn autoMockResult))
        {
            return autoMockResult;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4, T5>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, TReturn defaultValue, out TReturn result, Func<MockBehavior, IMock>? autoMockFactory)
        => TryHandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, arg5, defaultValue, out result);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, TReturn defaultValue, Func<MockBehavior, IMock>? autoMockFactory)
    {
        if (autoMockFactory is null)
        {
            return HandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, arg5, arg6, defaultValue);
        }

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
                if (trackedValue is TReturn typed) return typed;
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

        if (TryGetLooseAutoMockResult(memberName, autoMockFactory, out TReturn autoMockResult))
        {
            return autoMockResult;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, TReturn defaultValue, out TReturn result, Func<MockBehavior, IMock>? autoMockFactory)
        => TryHandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, arg5, arg6, defaultValue, out result);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6, T7>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, TReturn defaultValue, Func<MockBehavior, IMock>? autoMockFactory)
    {
        if (autoMockFactory is null)
        {
            return HandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, defaultValue);
        }

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
                if (trackedValue is TReturn typed) return typed;
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

        if (TryGetLooseAutoMockResult(memberName, autoMockFactory, out TReturn autoMockResult))
        {
            return autoMockResult;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6, T7>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, TReturn defaultValue, out TReturn result, Func<MockBehavior, IMock>? autoMockFactory)
        => TryHandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, defaultValue, out result);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TReturn HandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6, T7, T8>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, TReturn defaultValue, Func<MockBehavior, IMock>? autoMockFactory)
    {
        if (autoMockFactory is null)
        {
            return HandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, defaultValue);
        }

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
                if (trackedValue is TReturn typed) return typed;
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

        if (TryGetLooseAutoMockResult(memberName, autoMockFactory, out TReturn autoMockResult))
        {
            return autoMockResult;
        }

        if (Behavior == MockBehavior.Strict)
        {
            throw new MockStrictBehaviorException(FormatCall(memberName, store));
        }

        return defaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn, T1, T2, T3, T4, T5, T6, T7, T8>(int memberId, string memberName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, TReturn defaultValue, out TReturn result, Func<MockBehavior, IMock>? autoMockFactory)
        => TryHandleCallWithReturn(memberId, memberName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, defaultValue, out result);
}
