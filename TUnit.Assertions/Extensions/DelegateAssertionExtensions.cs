using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for delegate-specific assertions
/// </summary>
public static class DelegateAssertionExtensions
{
    // === For DelegateAssertionBuilder<Action> ===
    public static CustomAssertion<Action> CompletesWithin(
        this DelegateAssertionBuilder<Action> builder, TimeSpan timeout)
    {
        return new CustomAssertion<Action>(builder.DelegateProvider,
            action =>
            {
                if (action == null) return false;

                var sw = Stopwatch.StartNew();
                try
                {
                    var task = Task.Run(() => action());
                    return task.Wait(timeout);
                }
                catch
                {
                    // If exception occurs, we still check if it completed within timeout
                    return sw.Elapsed <= timeout;
                }
                finally
                {
                    sw.Stop();
                }
            },
            $"Expected delegate to complete within {timeout.TotalMilliseconds}ms");
    }

    public static CustomAssertion<Action> DoesNotCompleteWithin(
        this DelegateAssertionBuilder<Action> builder, TimeSpan timeout)
    {
        return new CustomAssertion<Action>(builder.DelegateProvider,
            action =>
            {
                if (action == null) return true;

                try
                {
                    var task = Task.Run(() => action());
                    return !task.Wait(timeout);
                }
                catch
                {
                    // If exception occurs quickly, it did complete
                    return false;
                }
            },
            $"Expected delegate to not complete within {timeout.TotalMilliseconds}ms");
    }

    // === For DelegateAssertionBuilder<Func<Task>> ===
    public static CustomAssertion<Func<Task>> CompletesWithin(
        this DelegateAssertionBuilder<Func<Task>> builder, TimeSpan timeout)
    {
        return new CustomAssertion<Func<Task>>(builder.DelegateProvider,
            asyncAction =>
            {
                if (asyncAction == null) return false;

                try
                {
                    var task = asyncAction();
                    return task.Wait(timeout);
                }
                catch
                {
                    // If exception occurs, we still check if it completed within timeout
                    return true;
                }
            },
            $"Expected async delegate to complete within {timeout.TotalMilliseconds}ms");
    }

    public static CustomAssertion<Func<Task>> DoesNotCompleteWithin(
        this DelegateAssertionBuilder<Func<Task>> builder, TimeSpan timeout)
    {
        return new CustomAssertion<Func<Task>>(builder.DelegateProvider,
            asyncAction =>
            {
                if (asyncAction == null) return true;

                try
                {
                    var task = asyncAction();
                    return !task.Wait(timeout);
                }
                catch
                {
                    // If exception occurs quickly, it did complete
                    return false;
                }
            },
            $"Expected async delegate to not complete within {timeout.TotalMilliseconds}ms");
    }

    // === For DualAssertionBuilder (value-returning delegates) ===
    public static CustomAssertion<T> CompletesWithin<T>(
        this DualAssertionBuilder<T> builder, TimeSpan timeout)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            _ =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var task = builder.ActualValueProvider();
                    var completed = task.Wait(timeout);
                    sw.Stop();
                    return completed;
                }
                catch
                {
                    sw.Stop();
                    return sw.Elapsed <= timeout;
                }
            },
            $"Expected delegate to complete within {timeout.TotalMilliseconds}ms");
    }

    public static CustomAssertion<T> DoesNotCompleteWithin<T>(
        this DualAssertionBuilder<T> builder, TimeSpan timeout)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            _ =>
            {
                try
                {
                    var task = builder.ActualValueProvider();
                    return !task.Wait(timeout);
                }
                catch
                {
                    // If exception occurs quickly, it did complete
                    return false;
                }
            },
            $"Expected delegate to not complete within {timeout.TotalMilliseconds}ms");
    }
}