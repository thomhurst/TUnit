using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Conditions.Wrappers;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for IAssertionSource&lt;T&gt; - the primary assertion API surface.
/// These methods work on Assertion&lt;T&gt;, AndContinuation&lt;T&gt;, and OrContinuation&lt;T&gt;!
/// No duplication needed - one set of extensions for everything!
/// </summary>
public static class AssertionExtensions
{
    // ============ NULL CHECKS ============

    /// <summary>
    /// Asserts that the value is null.
    /// </summary>
    public static NullAssertion<TValue> IsNull<TValue>(
        this IAssertionSource<TValue> source)
    {
        source.ExpressionBuilder.Append(".IsNull()");
        return new NullAssertion<TValue>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is not null.
    /// </summary>
    public static NotNullAssertion<TValue> IsNotNull<TValue>(
        this IAssertionSource<TValue> source)
    {
        source.ExpressionBuilder.Append(".IsNotNull()");
        return new NotNullAssertion<TValue>(source.Context, source.ExpressionBuilder);
    }

    // ============ EQUALITY ============

    /// <summary>
    /// Asserts that the value is equal to the expected value (generic version).
    /// Works with assertions, And, and Or continuations!
    /// </summary>
    public static EqualsAssertion<TValue> IsEqualTo<TValue>(
        this IAssertionSource<TValue> source,
        TValue expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new EqualsAssertion<TValue>(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is NOT equal to the expected value.
    /// </summary>
    public static NotEqualsAssertion<TValue> IsNotEqualTo<TValue>(
        this IAssertionSource<TValue> source,
        TValue notExpected,
        [CallerArgumentExpression(nameof(notExpected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsNotEqualTo({expression})");
        return new NotEqualsAssertion<TValue>(source.Context, notExpected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the DateTime is equal to the expected value.
    /// Returns DateTimeEqualsAssertion which has .Within() method!
    /// </summary>
    public static DateTimeEqualsAssertion IsEqualTo(
        this IAssertionSource<DateTime> source,
        DateTime expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new DateTimeEqualsAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string is equal to the expected value.
    /// Returns StringEqualsAssertion which has .IgnoringCase() and .WithComparison() methods!
    /// </summary>
    public static StringEqualsAssertion IsEqualTo(
        this IAssertionSource<string> source,
        string expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new StringEqualsAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string is equal to the expected value using the specified comparison.
    /// </summary>
    public static StringEqualsAssertion IsEqualTo(
        this IAssertionSource<string> source,
        string expected,
        StringComparison comparison,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEqualTo({expression}, {comparison})");
        var assertion = new StringEqualsAssertion(source.Context, expected, source.ExpressionBuilder);
        return assertion.WithComparison(comparison);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Asserts that the DateOnly is equal to the expected value.
    /// Returns DateOnlyEqualsAssertion which has .WithinDays() method!
    /// </summary>
    public static DateOnlyEqualsAssertion IsEqualTo(
        this IAssertionSource<DateOnly> source,
        DateOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new DateOnlyEqualsAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the TimeOnly is equal to the expected value.
    /// Returns TimeOnlyEqualsAssertion which has .Within() method!
    /// </summary>
    public static TimeOnlyEqualsAssertion IsEqualTo(
        this IAssertionSource<TimeOnly> source,
        TimeOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new TimeOnlyEqualsAssertion(source.Context, expected, source.ExpressionBuilder);
    }
#endif

    /// <summary>
    /// Asserts that the double is equal to the expected value.
    /// Returns DoubleEqualsAssertion which has .Within() method!
    /// </summary>
    public static DoubleEqualsAssertion IsEqualTo(
        this IAssertionSource<double> source,
        double expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new DoubleEqualsAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the long is equal to the expected value.
    /// Returns LongEqualsAssertion which has .Within() method!
    /// </summary>
    public static LongEqualsAssertion IsEqualTo(
        this IAssertionSource<long> source,
        long expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new LongEqualsAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the DateTimeOffset is equal to the expected value.
    /// Returns DateTimeOffsetEqualsAssertion which has .Within() method!
    /// </summary>
    public static DateTimeOffsetEqualsAssertion IsEqualTo(
        this IAssertionSource<DateTimeOffset> source,
        DateTimeOffset expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new DateTimeOffsetEqualsAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    // ============ COMPARISONS ============

    /// <summary>
    /// Asserts that the value is greater than the minimum.
    /// </summary>
    public static GreaterThanAssertion<TValue> IsGreaterThan<TValue>(
        this IAssertionSource<TValue> source,
        TValue minimum,
        [CallerArgumentExpression(nameof(minimum))] string? expression = null)
        where TValue : IComparable<TValue>
    {
        source.ExpressionBuilder.Append($".IsGreaterThan({expression})");
        return new GreaterThanAssertion<TValue>(source.Context, minimum, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is greater than or equal to the minimum.
    /// </summary>
    public static GreaterThanOrEqualAssertion<TValue> IsGreaterThanOrEqualTo<TValue>(
        this IAssertionSource<TValue> source,
        TValue minimum,
        [CallerArgumentExpression(nameof(minimum))] string? expression = null)
        where TValue : IComparable<TValue>
    {
        source.ExpressionBuilder.Append($".IsGreaterThanOrEqualTo({expression})");
        return new GreaterThanOrEqualAssertion<TValue>(source.Context, minimum, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is less than the maximum.
    /// </summary>
    public static LessThanAssertion<TValue> IsLessThan<TValue>(
        this IAssertionSource<TValue> source,
        TValue maximum,
        [CallerArgumentExpression(nameof(maximum))] string? expression = null)
        where TValue : IComparable<TValue>
    {
        source.ExpressionBuilder.Append($".IsLessThan({expression})");
        return new LessThanAssertion<TValue>(source.Context, maximum, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is less than or equal to the maximum.
    /// </summary>
    public static LessThanOrEqualAssertion<TValue> IsLessThanOrEqualTo<TValue>(
        this IAssertionSource<TValue> source,
        TValue maximum,
        [CallerArgumentExpression(nameof(maximum))] string? expression = null)
        where TValue : IComparable<TValue>
    {
        source.ExpressionBuilder.Append($".IsLessThanOrEqualTo({expression})");
        return new LessThanOrEqualAssertion<TValue>(source.Context, maximum, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is between minimum and maximum.
    /// Returns BetweenAssertion with .Inclusive(), .Exclusive() methods!
    /// </summary>
    public static BetweenAssertion<TValue> IsBetween<TValue>(
        this IAssertionSource<TValue> source,
        TValue minimum,
        TValue maximum,
        [CallerArgumentExpression(nameof(minimum))] string? minExpr = null,
        [CallerArgumentExpression(nameof(maximum))] string? maxExpr = null)
        where TValue : IComparable<TValue>
    {
        source.ExpressionBuilder.Append($".IsBetween({minExpr}, {maxExpr})");
        return new BetweenAssertion<TValue>(source.Context, minimum, maximum, source.ExpressionBuilder);
    }

    // ============ BOOLEAN ============

    /// <summary>
    /// Asserts that the boolean value is true.
    /// </summary>
    public static TrueAssertion IsTrue(
        this IAssertionSource<bool> source)
    {
        source.ExpressionBuilder.Append(".IsTrue()");
        return new TrueAssertion(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the boolean value is false.
    /// </summary>
    public static FalseAssertion IsFalse(
        this IAssertionSource<bool> source)
    {
        source.ExpressionBuilder.Append(".IsFalse()");
        return new FalseAssertion(source.Context, source.ExpressionBuilder);
    }

    // ============ TYPE CHECKS ============

    /// <summary>
    /// Asserts that the value is of the specified type.
    /// Returns an assertion typed to TExpected, enabling type-safe chaining!
    /// Example: await Assert.That(obj).IsTypeOf&lt;string&gt;().And.IsEqualTo("hello");
    /// Example: await Assert.That(exception).IsTypeOf&lt;InvalidOperationException&gt;();
    /// </summary>
    public static TypeOfAssertion<object, TExpected> IsTypeOf<TExpected, TValue>(
        this IAssertionSource<TValue> source)
    {
        source.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        // Map to object context since TypeOfAssertion expects object
        var objectContext = source.Context.Map<object>(v => (object)v!);
        return new TypeOfAssertion<object, TExpected>(objectContext, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value's type is assignable to the specified type (is the type or a derived type).
    /// Example: await Assert.That(myDog).IsAssignableTo&lt;Animal&gt;();
    /// </summary>
    public static IsAssignableToAssertion<TValue, TTarget> IsAssignableTo<TTarget, TValue>(
        this IAssertionSource<TValue> source)
    {
        source.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TValue, TTarget>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value's type is NOT assignable to the specified type.
    /// Example: await Assert.That(myDog).IsNotAssignableTo&lt;Cat&gt;();
    /// </summary>
    public static IsNotAssignableToAssertion<TValue, TTarget> IsNotAssignableTo<TTarget, TValue>(
        this IAssertionSource<TValue> source)
    {
        source.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TValue, TTarget>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts on a member of an object using a lambda selector.
    /// Returns an assertion on the member value for further chaining.
    /// Example: await Assert.That(myObject).HasMember(x => x.PropertyName).IsEqualTo(expectedValue);
    /// </summary>
    public static MemberAssertion<TObject, TMember> HasMember<TObject, TMember>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, TMember>> memberSelector)
    {
        return new MemberAssertion<TObject, TMember>(source.Context, memberSelector, source.ExpressionBuilder);
    }

    // ============ REFERENCE EQUALITY ============

    /// <summary>
    /// Asserts that the value is the same reference as the expected object.
    /// Example: await Assert.That(obj1).IsSameReferenceAs(obj2);
    /// </summary>
    public static SameReferenceAssertion<TValue> IsSameReferenceAs<TValue>(
        this IAssertionSource<TValue> source,
        object? expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsSameReferenceAs({expression})");
        return new SameReferenceAssertion<TValue>(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is NOT the same reference as the expected object.
    /// Example: await Assert.That(obj1).IsNotSameReferenceAs(obj2);
    /// </summary>
    public static NotSameReferenceAssertion<TValue> IsNotSameReferenceAs<TValue>(
        this IAssertionSource<TValue> source,
        object? expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsNotSameReferenceAs({expression})");
        return new NotSameReferenceAssertion<TValue>(source.Context, expected, source.ExpressionBuilder);
    }

    // ============ STRING ASSERTIONS ============

    /// <summary>
    /// Asserts that the string contains the expected substring.
    /// </summary>
    public static StringContainsAssertion Contains(
        this IAssertionSource<string> source,
        string expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".Contains({expression})");
        return new StringContainsAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string does NOT contain the expected substring.
    /// </summary>
    public static StringDoesNotContainAssertion DoesNotContain(
        this IAssertionSource<string> source,
        string expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new StringDoesNotContainAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string starts with the expected substring.
    /// </summary>
    public static StringStartsWithAssertion StartsWith(
        this IAssertionSource<string> source,
        string expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".StartsWith({expression})");
        return new StringStartsWithAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string ends with the expected substring.
    /// </summary>
    public static StringEndsWithAssertion EndsWith(
        this IAssertionSource<string> source,
        string expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".EndsWith({expression})");
        return new StringEndsWithAssertion(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string is not empty or whitespace.
    /// </summary>
    public static StringIsNotEmptyAssertion IsNotEmpty(
        this IAssertionSource<string> source)
    {
        source.ExpressionBuilder.Append(".IsNotEmpty()");
        return new StringIsNotEmptyAssertion(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string is empty or whitespace.
    /// </summary>
    public static StringIsEmptyAssertion IsEmpty(
        this IAssertionSource<string> source)
    {
        source.ExpressionBuilder.Append(".IsEmpty()");
        return new StringIsEmptyAssertion(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string is null or empty.
    /// </summary>
    public static StringIsNullOrEmptyAssertion IsNullOrEmpty(
        this IAssertionSource<string> source)
    {
        source.ExpressionBuilder.Append(".IsNullOrEmpty()");
        return new StringIsNullOrEmptyAssertion(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string is NOT null or empty.
    /// </summary>
    public static StringIsNotNullOrEmptyAssertion IsNotNullOrEmpty(
        this IAssertionSource<string> source)
    {
        source.ExpressionBuilder.Append(".IsNotNullOrEmpty()");
        return new StringIsNotNullOrEmptyAssertion(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Returns a wrapper for string length assertions.
    /// Example: await Assert.That(str).HasLength().EqualTo(5);
    /// </summary>
    public static LengthWrapper HasLength(
        this IAssertionSource<string> source)
    {
        source.ExpressionBuilder.Append(".HasLength()");
        return new LengthWrapper(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string has a specific length.
    /// Example: await Assert.That(str).HasLength(5);
    /// </summary>
    public static StringLengthAssertion HasLength(
        this IAssertionSource<string> source,
        int expectedLength,
        [CallerArgumentExpression(nameof(expectedLength))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".HasLength({expression})");
        return new StringLengthAssertion(source.Context, expectedLength, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string is null, empty, or whitespace.
    /// </summary>
    public static StringIsNullOrWhitespaceAssertion IsNullOrWhitespace(
        this IAssertionSource<string> source)
    {
        source.ExpressionBuilder.Append(".IsNullOrWhitespace()");
        return new StringIsNullOrWhitespaceAssertion(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string matches a regular expression pattern.
    /// </summary>
    public static StringMatchesAssertion Matches(
        this IAssertionSource<string> source,
        string pattern,
        [CallerArgumentExpression(nameof(pattern))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".Matches({expression})");
        return new StringMatchesAssertion(source.Context, pattern, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string matches a regular expression.
    /// </summary>
    public static StringMatchesAssertion Matches(
        this IAssertionSource<string> source,
        Regex regex,
        [CallerArgumentExpression(nameof(regex))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".Matches({expression})");
        return new StringMatchesAssertion(source.Context, regex.ToString(), source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string does NOT match a regular expression pattern.
    /// </summary>
    public static StringDoesNotMatchAssertion DoesNotMatch(
        this IAssertionSource<string> source,
        string pattern,
        [CallerArgumentExpression(nameof(pattern))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".DoesNotMatch({expression})");
        return new StringDoesNotMatchAssertion(source.Context, pattern, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the string does NOT match a regular expression.
    /// </summary>
    public static StringDoesNotMatchAssertion DoesNotMatch(
        this IAssertionSource<string> source,
        Regex regex,
        [CallerArgumentExpression(nameof(regex))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".DoesNotMatch({expression})");
        return new StringDoesNotMatchAssertion(source.Context, regex.ToString(), source.ExpressionBuilder);
    }

    // ============ DICTIONARY ASSERTIONS ============

    /// <summary>
    /// Asserts that the dictionary contains the specified key.
    /// Example: await Assert.That(dict).ContainsKey("key");
    /// </summary>
    public static DictionaryContainsKeyAssertion<TKey, TValue> ContainsKey<TKey, TValue>(
        this IAssertionSource<IReadOnlyDictionary<TKey, TValue>> source,
        TKey key,
        [CallerArgumentExpression(nameof(key))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".ContainsKey({expression})");
        return new DictionaryContainsKeyAssertion<TKey, TValue>(source.Context, key, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the dictionary contains the specified key.
    /// This overload works with custom dictionary types.
    /// Example: await Assert.That(customDict).ContainsKey("key");
    /// </summary>
    public static DictionaryContainsKeyAssertion<TKey, TValue> ContainsKey<TDictionary, TKey, TValue>(
        this IAssertionSource<TDictionary> source,
        TKey key,
        [CallerArgumentExpression(nameof(key))] string? expression = null)
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
    {
        source.ExpressionBuilder.Append($".ContainsKey({expression})");
        var mappedContext = source.Context.Map<IReadOnlyDictionary<TKey, TValue>>(dict => dict);
        return new DictionaryContainsKeyAssertion<TKey, TValue>(mappedContext, key, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the dictionary does NOT contain the specified key.
    /// Example: await Assert.That(dict).DoesNotContainKey("key");
    /// </summary>
    public static DictionaryDoesNotContainKeyAssertion<TKey, TValue> DoesNotContainKey<TKey, TValue>(
        this IAssertionSource<IReadOnlyDictionary<TKey, TValue>> source,
        TKey key,
        [CallerArgumentExpression(nameof(key))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".DoesNotContainKey({expression})");
        return new DictionaryDoesNotContainKeyAssertion<TKey, TValue>(source.Context, key, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the dictionary does NOT contain the specified key.
    /// This overload works with custom dictionary types.
    /// Example: await Assert.That(customDict).DoesNotContainKey("key");
    /// </summary>
    public static DictionaryDoesNotContainKeyAssertion<TKey, TValue> DoesNotContainKey<TDictionary, TKey, TValue>(
        this IAssertionSource<TDictionary> source,
        TKey key,
        [CallerArgumentExpression(nameof(key))] string? expression = null)
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
    {
        source.ExpressionBuilder.Append($".DoesNotContainKey({expression})");
        var mappedContext = source.Context.Map<IReadOnlyDictionary<TKey, TValue>>(dict => dict);
        return new DictionaryDoesNotContainKeyAssertion<TKey, TValue>(mappedContext, key, source.ExpressionBuilder);
    }

    // ============ COLLECTION ASSERTIONS ============

    /// <summary>
    /// Asserts that the collection is empty.
    /// </summary>
    public static CollectionIsEmptyAssertion<TValue> IsEmpty<TValue>(
        this IAssertionSource<TValue> source)
        where TValue : IEnumerable
    {
        source.ExpressionBuilder.Append(".IsEmpty()");
        return new CollectionIsEmptyAssertion<TValue>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection is NOT empty.
    /// </summary>
    public static CollectionIsNotEmptyAssertion<TValue> IsNotEmpty<TValue>(
        this IAssertionSource<TValue> source)
        where TValue : IEnumerable
    {
        source.ExpressionBuilder.Append(".IsNotEmpty()");
        return new CollectionIsNotEmptyAssertion<TValue>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection contains the expected item.
    /// </summary>
    public static CollectionContainsAssertion<TCollection, TItem> Contains<TCollection, TItem>(
        this IAssertionSource<TCollection> source,
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsAssertion<TCollection, TItem>(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection contains an item matching the predicate.
    /// </summary>
    public static CollectionContainsPredicateAssertion<TCollection, TItem> Contains<TCollection, TItem>(
        this IAssertionSource<TCollection> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsPredicateAssertion<TCollection, TItem>(source.Context, predicate, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection does NOT contain the expected item.
    /// </summary>
    public static CollectionDoesNotContainAssertion<TCollection, TItem> DoesNotContain<TCollection, TItem>(
        this IAssertionSource<TCollection> source,
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new CollectionDoesNotContainAssertion<TCollection, TItem>(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection does NOT contain any item matching the predicate.
    /// </summary>
    public static CollectionDoesNotContainPredicateAssertion<TCollection, TItem> DoesNotContain<TCollection, TItem>(
        this IAssertionSource<TCollection> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new CollectionDoesNotContainPredicateAssertion<TCollection, TItem>(source.Context, predicate, expression ?? "predicate", source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection contains ONLY items matching the predicate (all items satisfy the predicate).
    /// </summary>
    public static CollectionAllAssertion<TCollection, TItem> ContainsOnly<TCollection, TItem>(
        this IAssertionSource<TCollection> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.ExpressionBuilder.Append($".ContainsOnly({expression})");
        return new CollectionAllAssertion<TCollection, TItem>(source.Context, predicate, expression ?? "predicate", source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection is in ascending order.
    /// </summary>
    public static CollectionIsInOrderAssertion<TCollection, TItem> IsInOrder<TCollection, TItem>(
        this IAssertionSource<TCollection> source)
        where TCollection : IEnumerable<TItem>
        where TItem : IComparable<TItem>
    {
        source.ExpressionBuilder.Append(".IsInOrder()");
        return new CollectionIsInOrderAssertion<TCollection, TItem>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection is in descending order.
    /// </summary>
    public static CollectionIsInDescendingOrderAssertion<TCollection, TItem> IsInDescendingOrder<TCollection, TItem>(
        this IAssertionSource<TCollection> source)
        where TCollection : IEnumerable<TItem>
        where TItem : IComparable<TItem>
    {
        source.ExpressionBuilder.Append(".IsInDescendingOrder()");
        return new CollectionIsInDescendingOrderAssertion<TCollection, TItem>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Returns a wrapper for collection count assertions.
    /// Example: await Assert.That(list).HasCount().EqualTo(5);
    /// </summary>
    public static CountWrapper<TValue> HasCount<TValue>(
        this IAssertionSource<TValue> source)
        where TValue : IEnumerable
    {
        source.ExpressionBuilder.Append(".HasCount()");
        return new CountWrapper<TValue>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection has the expected count.
    /// Example: await Assert.That(list).HasCount(5);
    /// </summary>
    public static CollectionCountAssertion<TValue> HasCount<TValue>(
        this IAssertionSource<TValue> source,
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
        where TValue : IEnumerable
    {
        source.ExpressionBuilder.Append($".HasCount({expression})");
        return new CollectionCountAssertion<TValue>(source.Context, expectedCount, source.ExpressionBuilder);
    }

    /// <summary>
    /// Creates a helper for asserting that all items in the collection satisfy custom assertions.
    /// Example: await Assert.That(list).All().Satisfy(item => item.IsNotNull());
    /// </summary>
    public static CollectionAllSatisfyHelper<TCollection, TItem> All<TCollection, TItem>(
        this IAssertionSource<TCollection> source)
        where TCollection : IEnumerable<TItem>
    {
        source.ExpressionBuilder.Append(".All()");
        return new CollectionAllSatisfyHelper<TCollection, TItem>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that all items in the collection satisfy the predicate.
    /// </summary>
    public static CollectionAllAssertion<TCollection, TItem> All<TCollection, TItem>(
        this IAssertionSource<TCollection> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.ExpressionBuilder.Append($".All({expression})");
        return new CollectionAllAssertion<TCollection, TItem>(source.Context, predicate, expression ?? "predicate", source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that at least one item in the collection satisfies the predicate.
    /// </summary>
    public static CollectionAnyAssertion<TCollection, TItem> Any<TCollection, TItem>(
        this IAssertionSource<TCollection> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.ExpressionBuilder.Append($".Any({expression})");
        return new CollectionAnyAssertion<TCollection, TItem>(source.Context, predicate, expression ?? "predicate", source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection contains exactly one item.
    /// </summary>
    public static HasSingleItemAssertion<TValue> HasSingleItem<TValue>(
        this IAssertionSource<TValue> source)
        where TValue : IEnumerable
    {
        source.ExpressionBuilder.Append(".HasSingleItem()");
        return new HasSingleItemAssertion<TValue>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection contains only distinct (unique) items.
    /// </summary>
    public static HasDistinctItemsAssertion<TValue> HasDistinctItems<TValue>(
        this IAssertionSource<TValue> source)
        where TValue : IEnumerable
    {
        source.ExpressionBuilder.Append(".HasDistinctItems()");
        return new HasDistinctItemsAssertion<TValue>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection is equivalent to the expected collection.
    /// Two collections are equivalent if they contain the same elements, regardless of order.
    /// </summary>
    public static IsEquivalentToAssertion<TCollection, TItem> IsEquivalentTo<TCollection, TItem>(
        this IAssertionSource<TCollection> source,
        IEnumerable<TItem> expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.ExpressionBuilder.Append($".IsEquivalentTo({expression})");
        return new IsEquivalentToAssertion<TCollection, TItem>(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is equivalent (equal) to the expected value.
    /// For non-collection types, this is the same as IsEqualTo.
    /// </summary>
    public static EqualsAssertion<TValue> IsEquivalentTo<TValue>(
        this IAssertionSource<TValue> source,
        TValue expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEquivalentTo({expression})");
        return new EqualsAssertion<TValue>(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is NOT equivalent (not equal) to the expected value.
    /// For non-collection types, this is the same as IsNotEqualTo.
    /// </summary>
    public static NotEqualsAssertion<TValue> IsNotEquivalentTo<TValue>(
        this IAssertionSource<TValue> source,
        TValue expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsNotEquivalentTo({expression})");
        return new NotEqualsAssertion<TValue>(source.Context, expected, source.ExpressionBuilder);
    }

    // ============ PREDICATE CHECKS ============

    /// <summary>
    /// Asserts that the value satisfies the specified predicate.
    /// Example: await Assert.That(x).Satisfies(v => v > 0 && v < 100);
    /// </summary>
    public static SatisfiesAssertion<TValue> Satisfies<TValue>(
        this IAssertionSource<TValue> source,
        Func<TValue?, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".Satisfies({expression})");
        return new SatisfiesAssertion<TValue>(source.Context, predicate, expression ?? "predicate", source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is equal to the expected value using IEquatable or default equality.
    /// Optimized for types that implement IEquatable.
    /// Example: await Assert.That(obj).IsEquatableOrEqualTo(expected);
    /// </summary>
    public static IsEquatableOrEqualToAssertion<TValue> IsEquatableOrEqualTo<TValue>(
        this IAssertionSource<TValue> source,
        TValue expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsEquatableOrEqualTo({expression})");
        return new IsEquatableOrEqualToAssertion<TValue>(source.Context, expected, source.ExpressionBuilder);
    }

    // ============ MEMBERSHIP CHECKS ============

    /// <summary>
    /// Asserts that the value is in the specified collection.
    /// Example: await Assert.That(5).IsIn(new[] { 1, 3, 5, 7, 9 });
    /// </summary>
    public static IsInAssertion<TValue> IsIn<TValue>(
        this IAssertionSource<TValue> source,
        IEnumerable<TValue> collection,
        [CallerArgumentExpression(nameof(collection))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsIn({expression})");
        return new IsInAssertion<TValue>(source.Context, collection, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is in the specified collection (params array).
    /// Example: await Assert.That(5).IsIn(1, 3, 5, 7, 9);
    /// </summary>
    public static IsInAssertion<TValue> IsIn<TValue>(
        this IAssertionSource<TValue> source,
        params TValue[] collection)
    {
        source.ExpressionBuilder.Append($".IsIn({string.Join(", ", collection)})");
        return new IsInAssertion<TValue>(source.Context, collection, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is NOT in the specified collection.
    /// Example: await Assert.That(4).IsNotIn(new[] { 1, 3, 5, 7, 9 });
    /// </summary>
    public static IsNotInAssertion<TValue> IsNotIn<TValue>(
        this IAssertionSource<TValue> source,
        IEnumerable<TValue> collection,
        [CallerArgumentExpression(nameof(collection))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsNotIn({expression})");
        return new IsNotInAssertion<TValue>(source.Context, collection, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is NOT in the specified collection (params array).
    /// Example: await Assert.That(4).IsNotIn(1, 3, 5, 7, 9);
    /// </summary>
    public static IsNotInAssertion<TValue> IsNotIn<TValue>(
        this IAssertionSource<TValue> source,
        params TValue[] collection)
    {
        source.ExpressionBuilder.Append($".IsNotIn({string.Join(", ", collection)})");
        return new IsNotInAssertion<TValue>(source.Context, collection, source.ExpressionBuilder);
    }

    // ============ EXCEPTION CHECKS ============

    /// <summary>
    /// Asserts that the delegate throws the specified exception type (or subclass).
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public static ThrowsAssertion<TException> Throws<TException, TValue>(
        this IAssertionSource<TValue> source)
        where TException : Exception
    {
        source.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        // Map the context to object? since we only care about the exception
        var mappedContext = source.Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext, source.ExpressionBuilder);
    }

    /// <summary>
    /// Alias for Throws - asserts that the delegate throws the specified exception type.
    /// Example: await Assert.That(() => ThrowingMethod()).ThrowsException&lt;InvalidOperationException&gt;();
    /// </summary>
    public static ThrowsAssertion<TException> ThrowsException<TException, TValue>(
        this IAssertionSource<TValue> source)
        where TException : Exception
    {
        source.ExpressionBuilder.Append($".ThrowsException<{typeof(TException).Name}>()");
        var mappedContext = source.Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the delegate throws any exception.
    /// Example: await Assert.That(() => ThrowingMethod()).ThrowsException();
    /// </summary>
    public static ThrowsAssertion<Exception> ThrowsException<TValue>(
        this IAssertionSource<TValue> source)
    {
        source.ExpressionBuilder.Append(".ThrowsException()");
        var mappedContext = source.Context.Map<object?>(_ => null);
        return new ThrowsAssertion<Exception>(mappedContext, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the async delegate throws the specified exception type (or subclass).
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).ThrowsAsync&lt;InvalidOperationException&gt;();
    /// </summary>
    public static ThrowsAssertion<TException> ThrowsAsync<TValue, TException>(
        this IAssertionSource<TValue> source)
        where TException : Exception
    {
        source.ExpressionBuilder.Append($".ThrowsAsync<{typeof(TException).Name}>()");
        // Map the context to object? since we only care about the exception
        var mappedContext = source.Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the delegate throws exactly the specified exception type (not subclasses).
    /// Example: await Assert.That(() => ThrowingMethod()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public static ThrowsExactlyAssertion<TException> ThrowsExactly<TException, TValue>(
        this IAssertionSource<TValue> source)
        where TException : Exception
    {
        source.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        // Map the context to object? since we only care about the exception
        var mappedContext = source.Context.Map<object?>(_ => null);
        return new ThrowsExactlyAssertion<TException>(mappedContext, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the delegate does not throw any exception.
    /// Example: await Assert.That(() => SafeMethod()).ThrowsNothing();
    /// </summary>
    public static ThrowsNothingAssertion ThrowsNothing<TValue>(
        this IAssertionSource<TValue> source)
    {
        source.ExpressionBuilder.Append(".ThrowsNothing()");
        // Map the context to object? since we only care about the exception
        var mappedContext = source.Context.Map<object?>(_ => null);
        return new ThrowsNothingAssertion(mappedContext, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring.
    /// Works on AndContinuation after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().And.HasMessageContaining("error");
    /// </summary>
    public static ExceptionMessageAssertion HasMessageContaining(
        this IAssertionSource<object?> source,
        string expectedSubstring)
    {
        source.ExpressionBuilder.Append($".HasMessageContaining(\"{expectedSubstring}\")");
        return new ExceptionMessageAssertion(source.Context, expectedSubstring, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring using the specified comparison.
    /// Works on AndContinuation after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().And.HasMessageContaining("error", StringComparison.OrdinalIgnoreCase);
    /// </summary>
    public static ExceptionMessageAssertion HasMessageContaining(
        this IAssertionSource<object?> source,
        string expectedSubstring,
        StringComparison comparison)
    {
        source.ExpressionBuilder.Append($".HasMessageContaining(\"{expectedSubstring}\", StringComparison.{comparison})");
        return new ExceptionMessageAssertion(source.Context, expectedSubstring, source.ExpressionBuilder, comparison);
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring.
    /// Alias for HasMessageContaining.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().And.WithMessageContaining("error");
    /// </summary>
    public static ExceptionMessageAssertion WithMessageContaining(
        this IAssertionSource<object?> source,
        string expectedSubstring)
    {
        source.ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\")");
        return new ExceptionMessageAssertion(source.Context, expectedSubstring, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring using the specified comparison.
    /// Alias for HasMessageContaining.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().And.WithMessageContaining("error", StringComparison.OrdinalIgnoreCase);
    /// </summary>
    public static ExceptionMessageAssertion WithMessageContaining(
        this IAssertionSource<object?> source,
        string expectedSubstring,
        StringComparison comparison)
    {
        source.ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\", StringComparison.{comparison})");
        return new ExceptionMessageAssertion(source.Context, expectedSubstring, source.ExpressionBuilder, comparison);
    }

    // Specific overloads for delegate types where TValue is always object?
    public static ThrowsAssertion<TException> Throws<TException>(this DelegateAssertion source) where TException : Exception
    {
        var iface = (IAssertionSource<object?>)source;
        iface.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext, iface.ExpressionBuilder);
    }

    public static ThrowsExactlyAssertion<TException> ThrowsExactly<TException>(this DelegateAssertion source) where TException : Exception
    {
        var iface = (IAssertionSource<object?>)source;
        iface.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.Map<object?>(_ => null);
        return new ThrowsExactlyAssertion<TException>(mappedContext, iface.ExpressionBuilder);
    }

    public static ThrowsAssertion<TException> Throws<TException>(this AsyncDelegateAssertion source) where TException : Exception
    {
        var iface = (IAssertionSource<object?>)source;
        iface.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext, iface.ExpressionBuilder);
    }

    public static ThrowsExactlyAssertion<TException> ThrowsExactly<TException>(this AsyncDelegateAssertion source) where TException : Exception
    {
        var iface = (IAssertionSource<object?>)source;
        iface.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.Map<object?>(_ => null);
        return new ThrowsExactlyAssertion<TException>(mappedContext, iface.ExpressionBuilder);
    }

    // Specific overloads for Func/AsyncFunc assertions - TValue is inferred from the receiver type
    public static ThrowsAssertion<TException> Throws<TException, TValue>(this FuncAssertion<TValue> source) where TException : Exception
    {
        var iface = (IAssertionSource<TValue>)source;
        iface.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext, iface.ExpressionBuilder);
    }

    public static ThrowsAssertion<TException> Throws<TException, TValue>(this AsyncFuncAssertion<TValue> source) where TException : Exception
    {
        var iface = (IAssertionSource<TValue>)source;
        iface.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext, iface.ExpressionBuilder);
    }

    public static ThrowsExactlyAssertion<TException> ThrowsExactly<TException, TValue>(this FuncAssertion<TValue> source) where TException : Exception
    {
        var iface = (IAssertionSource<TValue>)source;
        iface.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.Map<object?>(_ => null);
        return new ThrowsExactlyAssertion<TException>(mappedContext, iface.ExpressionBuilder);
    }

    public static ThrowsExactlyAssertion<TException> ThrowsExactly<TException, TValue>(this AsyncFuncAssertion<TValue> source) where TException : Exception
    {
        var iface = (IAssertionSource<TValue>)source;
        iface.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.Map<object?>(_ => null);
        return new ThrowsExactlyAssertion<TException>(mappedContext, iface.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that an exception's Message property exactly equals the expected string.
    /// Works with both direct exception assertions and chained exception assertions (via .And).
    /// </summary>
    public static HasMessageEqualToAssertion<TValue> HasMessageEqualTo<TValue>(
        this IAssertionSource<TValue> source,
        string expectedMessage)
    {
        source.ExpressionBuilder.Append($".HasMessageEqualTo(\"{expectedMessage}\")");
        return new HasMessageEqualToAssertion<TValue>(source.Context, expectedMessage, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that an exception's Message property exactly equals the expected string using the specified string comparison.
    /// Works with both direct exception assertions and chained exception assertions (via .And).
    /// </summary>
    public static HasMessageEqualToAssertion<TValue> HasMessageEqualTo<TValue>(
        this IAssertionSource<TValue> source,
        string expectedMessage,
        StringComparison comparison)
    {
        source.ExpressionBuilder.Append($".HasMessageEqualTo(\"{expectedMessage}\", StringComparison.{comparison})");
        return new HasMessageEqualToAssertion<TValue>(source.Context, expectedMessage, source.ExpressionBuilder, comparison);
    }

    /// <summary>
    /// Asserts that the DateTime is after or equal to the expected DateTime.
    /// Alias for IsGreaterThanOrEqualTo for better readability with dates.
    /// </summary>
    public static GreaterThanOrEqualAssertion<DateTime> IsAfterOrEqualTo(
        this IAssertionSource<DateTime> source,
        DateTime expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".IsAfterOrEqualTo({expression})");
        return new GreaterThanOrEqualAssertion<DateTime>(source.Context, expected, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is equal to its default value.
    /// For reference types, this is null. For value types, this is the zero-initialized value.
    /// </summary>
    public static IsDefaultAssertion<TValue> IsDefault<TValue>(
        this IAssertionSource<TValue> source)
    {
        source.ExpressionBuilder.Append(".IsDefault()");
        return new IsDefaultAssertion<TValue>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is not the default value for its type.
    /// </summary>
    public static IsNotDefaultAssertion<TValue> IsNotDefault<TValue>(
        this IAssertionSource<TValue> source)
    {
        source.ExpressionBuilder.Append(".IsNotDefault()");
        return new IsNotDefaultAssertion<TValue>(source.Context, source.ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the TimeSpan is equal to the expected value within the specified tolerance.
    /// </summary>
    public static EqualsAssertion<TimeSpan> Within(
        this EqualsAssertion<TimeSpan> assertion,
        TimeSpan tolerance)
    {
        assertion.ExpressionBuilder.Append($".Within({tolerance})");
        return assertion.WithTolerance(tolerance);
    }

    /// <summary>
    /// Asserts that the DateTime is equal to the expected value within the specified tolerance.
    /// </summary>
    public static EqualsAssertion<DateTime> Within(
        this EqualsAssertion<DateTime> assertion,
        TimeSpan tolerance)
    {
        assertion.ExpressionBuilder.Append($".Within({tolerance})");
        return assertion.WithTolerance(tolerance);
    }

    /// <summary>
    /// Asserts that the DateTimeOffset is equal to the expected value within the specified tolerance.
    /// </summary>
    public static EqualsAssertion<DateTimeOffset> Within(
        this EqualsAssertion<DateTimeOffset> assertion,
        TimeSpan tolerance)
    {
        assertion.ExpressionBuilder.Append($".Within({tolerance})");
        return assertion.WithTolerance(tolerance);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Asserts that the TimeOnly is equal to the expected value within the specified tolerance.
    /// </summary>
    public static EqualsAssertion<TimeOnly> Within(
        this EqualsAssertion<TimeOnly> assertion,
        TimeSpan tolerance)
    {
        assertion.ExpressionBuilder.Append($".Within({tolerance})");
        return assertion.WithTolerance(tolerance);
    }
#endif

    /// <summary>
    /// Asserts that the int is equal to the expected value within the specified tolerance.
    /// </summary>
    public static EqualsAssertion<int> Within(
        this EqualsAssertion<int> assertion,
        int tolerance)
    {
        assertion.ExpressionBuilder.Append($".Within({tolerance})");
        return assertion.WithTolerance(tolerance);
    }

    /// <summary>
    /// Asserts that the long is equal to the expected value within the specified tolerance.
    /// </summary>
    public static EqualsAssertion<long> Within(
        this EqualsAssertion<long> assertion,
        long tolerance)
    {
        assertion.ExpressionBuilder.Append($".Within({tolerance})");
        return assertion.WithTolerance(tolerance);
    }

    /// <summary>
    /// Asserts that the double is equal to the expected value within the specified tolerance.
    /// </summary>
    public static EqualsAssertion<double> Within(
        this EqualsAssertion<double> assertion,
        double tolerance)
    {
        assertion.ExpressionBuilder.Append($".Within({tolerance})");
        return assertion.WithTolerance(tolerance);
    }

    /// <summary>
    /// Asserts that the decimal is equal to the expected value within the specified tolerance.
    /// </summary>
    public static EqualsAssertion<decimal> Within(
        this EqualsAssertion<decimal> assertion,
        decimal tolerance)
    {
        assertion.ExpressionBuilder.Append($".Within({tolerance})");
        return assertion.WithTolerance(tolerance);
    }
}
