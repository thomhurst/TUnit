using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;
using TUnit.Assertions.Should.Core;

namespace TUnit.Assertions.Should;

/// <summary>
/// Entry-point extensions for Should-style assertions. Mirrors
/// <c>TUnit.Assertions.Assert.That(...)</c> as a fluent extension on the value.
/// </summary>
public static partial class ShouldExtensions
{
    /// <summary>
    /// Begins a Should-flavored assertion chain on the supplied value.
    /// </summary>
    public static ShouldSource<T> Should<T>(
        this T? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        var sb = ShouldExpressionBuilder.Build(expression);
        return new ShouldSource<T>(new AssertionContext<T>(value, sb));
    }

    /// <summary>
    /// Strings — explicit overload so they don't bind to the <see cref="IEnumerable{T}"/> overload
    /// (which would lose string-specific assertions like <c>StartWith</c>/<c>Contain</c>).
    /// <see cref="OverloadResolutionPriorityAttribute"/> is honored by SDK 9+ compilers; older
    /// compilers ignore it and fall back to normal overload resolution.
    /// </summary>
    [OverloadResolutionPriority(2)]
    public static ShouldSource<string> Should(
        this string? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        var sb = ShouldExpressionBuilder.Build(expression);
        return new ShouldSource<string>(new AssertionContext<string>(value, sb));
    }

    /// <summary>
    /// Collections enter as <see cref="ShouldCollectionSource{T}"/> so element-typed
    /// instance methods (<c>BeInOrder</c>, <c>HaveSingleItem</c>, <c>All</c>, <c>Any</c>) infer
    /// the element type without an explicit type argument. The wrapper also implements
    /// <see cref="IShouldSource{T}"/> for <see cref="IEnumerable{T}"/> so generated extensions
    /// that DO have item-typed parameters (<c>Contain</c>, <c>NotContain</c>) resolve as well.
    /// <see cref="OverloadResolutionPriorityAttribute"/> is honored by SDK 9+ compilers; older
    /// compilers ignore it and fall back to normal overload resolution.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static ShouldCollectionSource<T> Should<T>(
        this IEnumerable<T>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => new(value, expression);

    [OverloadResolutionPriority(3)]
    public static ShouldCollectionSource<T> Should<T>(
        this ICollection<T>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => new(value, expression);

    [OverloadResolutionPriority(2)]
    public static ShouldCollectionSource<T> Should<T>(
        this IReadOnlyCollection<T>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => new(value, expression);

    [OverloadResolutionPriority(4)]
    public static ShouldCollectionSource<T> Should<T>(
        this IList<T>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => new(value, expression);

    [OverloadResolutionPriority(5)]
    public static ShouldCollectionSource<T> Should<T>(
        this T[]? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => new(value, expression);

    [OverloadResolutionPriority(3)]
    public static ShouldCollectionSource<T> Should<T>(
        this IReadOnlyList<T>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => new(value, expression);

    [OverloadResolutionPriority(2)]
    public static ShouldDictionarySource<TKey, TValue> Should<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where TKey : notnull
        => new(value, expression);

    [OverloadResolutionPriority(3)]
    public static ShouldMutableDictionarySource<TKey, TValue> Should<TKey, TValue>(
        this IDictionary<TKey, TValue>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where TKey : notnull
        => new(value, expression);

    [OverloadResolutionPriority(1)]
    public static ShouldSetSource<T> Should<T>(
        this ISet<T>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => new(value, expression);

#if NET5_0_OR_GREATER
    [OverloadResolutionPriority(2)]
    public static ShouldReadOnlySetSource<T> Should<T>(
        this IReadOnlySet<T>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => new(value, expression);
#endif

    [OverloadResolutionPriority(3)]
    public static ShouldHashSetSource<T> Should<T>(
        this HashSet<T>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => new(value, expression);

    /// <summary>
    /// Begins a Should-flavored assertion chain on a synchronous action.
    /// Used primarily for exception assertions: <c>(() => action()).Should().Throw&lt;E&gt;()</c>.
    /// </summary>
    public static ShouldDelegateSource<object?> Should(
        this Action action,
        [CallerArgumentExpression(nameof(action))] string? expression = null)
        => CreateDelegateSource<object?>(expression, () =>
        {
            try { action(); return Task.FromResult<(object?, Exception?)>((null, null)); }
            catch (Exception ex) { return Task.FromResult<(object?, Exception?)>((null, ex)); }
        });

    /// <summary>
    /// Begins a Should-flavored assertion chain on a synchronous function.
    /// </summary>
    public static ShouldDelegateSource<T> Should<T>(
        this Func<T?> func,
        [CallerArgumentExpression(nameof(func))] string? expression = null)
        => CreateDelegateSource<T>(expression, () =>
        {
            try { return Task.FromResult<(T?, Exception?)>((func(), null)); }
            catch (Exception ex) { return Task.FromResult<(T?, Exception?)>((default, ex)); }
        });

    [OverloadResolutionPriority(1)]
    public static ShouldDelegateCollectionSource<T> Should<T>(
        this Func<IEnumerable<T>?> func,
        [CallerArgumentExpression(nameof(func))] string? expression = null)
        => new(ShouldDelegateCollectionSource<T>.CreateContext(func, expression));

    /// <summary>
    /// Begins a Should-flavored assertion chain on an asynchronous action.
    /// </summary>
    public static ShouldDelegateSource<object?> Should(
        this Func<Task> action,
        [CallerArgumentExpression(nameof(action))] string? expression = null)
        => CreateDelegateSource<object?>(expression, async () =>
        {
            try { await action().ConfigureAwait(false); return (null, null); }
            catch (Exception ex) { return (null, ex); }
        });

    /// <summary>
    /// Begins a Should-flavored assertion chain on an asynchronous function.
    /// </summary>
    public static ShouldDelegateSource<T> Should<T>(
        this Func<Task<T?>> func,
        [CallerArgumentExpression(nameof(func))] string? expression = null)
        => CreateDelegateSource<T>(expression, async () =>
        {
            try { return (await func().ConfigureAwait(false), null); }
            catch (Exception ex) { return (default, ex); }
        });

    [OverloadResolutionPriority(1)]
    public static ShouldDelegateCollectionSource<T> Should<T>(
        this Func<Task<IEnumerable<T>?>> func,
        [CallerArgumentExpression(nameof(func))] string? expression = null)
        => new(ShouldDelegateCollectionSource<T>.CreateContext(func, expression));

    private static ShouldDelegateSource<T> CreateDelegateSource<T>(
        string? expression, Func<Task<(T?, Exception?)>> evaluator)
        => new(new AssertionContext<T>(new EvaluationContext<T>(evaluator), ShouldExpressionBuilder.Build(expression)));
}
