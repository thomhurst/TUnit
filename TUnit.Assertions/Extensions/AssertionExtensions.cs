using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.Chaining;
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
    /// <summary>
    /// Asserts that the value is not null (for nullable reference types).
    /// Returns a non-nullable assertion allowing proper nullability flow analysis.
    /// </summary>
    public static NotNullAssertion<TValue> IsNotNull<TValue>(
        this IAssertionSource<TValue> source)
        where TValue : class
    {
        source.Context.ExpressionBuilder.Append(".IsNotNull()");
        // Map from TValue? to TValue (nullable to non-nullable)
        var mappedContext = source.Context.Map((TValue? v) => v!);
        return new NotNullAssertion<TValue>(mappedContext);
    }

    /// <summary>
    /// Asserts that the value is not null (for nullable value types).
    /// Returns a non-nullable assertion allowing proper nullability flow analysis.
    /// </summary>
    public static NotNullAssertion<TValue> IsNotNull<TValue>(
        this IAssertionSource<TValue?> source)
        where TValue : struct
    {
        source.Context.ExpressionBuilder.Append(".IsNotNull()");
        // Map from TValue? to TValue (Nullable<TValue> to TValue)
        var mappedContext = source.Context.Map<TValue>((TValue? v) => v!.Value);
        return new NotNullAssertion<TValue>(mappedContext);
    }

    /// <summary>
    /// Asserts that a collection is not null, preserving collection type information.
    /// Returns a collection-aware assertion that maintains TItem type for proper chaining.
    /// This overload enables: Assert.That(collection).IsNotNull().And.Contains(x => predicate).
    /// </summary>
    public static CollectionNotNullAssertion<TCollection, TItem> IsNotNull<TCollection, TItem>(
        this CollectionAssertionBase<TCollection, TItem> source)
        where TCollection : class, IEnumerable<TItem>
    {
        var assertionSource = (IAssertionSource<TCollection>)source;
        assertionSource.Context.ExpressionBuilder.Append(".IsNotNull()");
        // Map from TCollection? to TCollection (nullable to non-nullable)
        var mappedContext = assertionSource.Context.Map((TCollection? v) => v!);
        return new CollectionNotNullAssertion<TCollection, TItem>(mappedContext);
    }

    /// <summary>
    /// Alias for IsEqualTo - asserts that the value is equal to the expected value.
    /// Works with assertions, And, and Or continuations!
    /// </summary>
    public static EqualsAssertion<TValue> EqualTo<TValue>(
        this IAssertionSource<TValue> source,
        TValue? expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".EqualTo({expression})");
        return new EqualsAssertion<TValue>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the numeric value is greater than zero (positive).
    /// </summary>
    public static GreaterThanAssertion<TValue> IsPositive<TValue>(
        this IAssertionSource<TValue> source)
        where TValue : IComparable<TValue>
    {
        source.Context.ExpressionBuilder.Append(".IsPositive()");
        return new GreaterThanAssertion<TValue>(source.Context, default(TValue)!);
    }

    /// <summary>
    /// Asserts that the nullable numeric value is greater than zero (positive).
    /// </summary>
    public static GreaterThanAssertion<TValue> IsPositive<TValue>(
        this IAssertionSource<TValue?> source)
        where TValue : struct, IComparable<TValue>
    {
        source.Context.ExpressionBuilder.Append(".IsPositive()");
        var mappedContext = source.Context.Map<TValue>(nullableValue =>
        {
            if (!nullableValue.HasValue)
            {
                throw new ArgumentNullException(nameof(nullableValue), "value was null");
            }

            return nullableValue.Value;
        });
        return new GreaterThanAssertion<TValue>(mappedContext, default(TValue)!);
    }

    /// <summary>
    /// Asserts that the numeric value is less than zero (negative).
    /// </summary>
    public static LessThanAssertion<TValue> IsNegative<TValue>(
        this IAssertionSource<TValue> source)
        where TValue : IComparable<TValue>
    {
        source.Context.ExpressionBuilder.Append(".IsNegative()");
        return new LessThanAssertion<TValue>(source.Context, default(TValue)!);
    }

    /// <summary>
    /// Asserts that the nullable numeric value is less than zero (negative).
    /// </summary>
    public static LessThanAssertion<TValue> IsNegative<TValue>(
        this IAssertionSource<TValue?> source)
        where TValue : struct, IComparable<TValue>
    {
        source.Context.ExpressionBuilder.Append(".IsNegative()");
        var mappedContext = source.Context.Map<TValue>(nullableValue =>
        {
            if (!nullableValue.HasValue)
            {
                throw new ArgumentNullException(nameof(nullableValue), "value was null");
            }

            return nullableValue.Value;
        });
        return new LessThanAssertion<TValue>(mappedContext, default(TValue)!);
    }

    /// <summary>
    /// Asserts that the value is of the specified type (runtime Type parameter).
    /// Example: await Assert.That(obj).IsOfType(typeof(string));
    /// </summary>
    public static IsTypeOfRuntimeAssertion<TValue> IsOfType<TValue>(
        this IAssertionSource<TValue> source,
        Type expectedType,
        [CallerArgumentExpression(nameof(expectedType))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsOfType({expression})");
        return new IsTypeOfRuntimeAssertion<TValue>(source.Context, expectedType);
    }

    /// <summary>
    /// Asserts on a dictionary member of an object using a lambda selector and assertion lambda.
    /// The assertion lambda receives dictionary assertion methods (ContainsKey, ContainsValue, IsEmpty, etc.).
    /// Supports type transformations like IsTypeOf within the assertion lambda.
    /// After the member assertion completes, returns to the parent object context for further chaining.
    /// Example: await Assert.That(myObject).Member(x => x.Attributes, attrs => attrs.ContainsKey("status").And.IsNotEmpty());
    /// </summary>
    [OverloadResolutionPriority(3)]
    public static MemberAssertionResult<TObject> Member<TObject, TKey, TValue, TTransformed>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, IReadOnlyDictionary<TKey, TValue>>> memberSelector,
        Func<DictionaryMemberAssertionAdapter<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>, Assertion<TTransformed>> assertions)
        where TKey : notnull
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(memberSelector);

        parentContext.ExpressionBuilder.Append($".Member(x => x.{memberPath}, ...)");

        // Check if there's a pending link (from .And or .Or) that needs to be consumed
        var (pendingAssertion, combinerType) = parentContext.ConsumePendingLink();

        // Map to member context
        var memberContext = parentContext.Map<IReadOnlyDictionary<TKey, TValue>>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = memberSelector.Compile();
            return compiled(obj);
        });

        // Create a DictionaryMemberAssertionAdapter for the member
        var dictionaryAdapter = new DictionaryMemberAssertionAdapter<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(memberContext);
        var memberAssertion = assertions(dictionaryAdapter);

        // Type-erase to object? for storage - using TTransformed instead of dictionary type
        var erasedAssertion = new TypeErasedAssertion<TTransformed>(memberAssertion);

        // If there was a pending link, wrap both assertions together
        if (pendingAssertion != null && combinerType != null)
        {
            Assertion<object?> combinedAssertion = combinerType == CombinerType.And
                ? new CombinedAndAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion)
                : new CombinedOrAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion);

            return new MemberAssertionResult<TObject>(parentContext, combinedAssertion);
        }

        return new MemberAssertionResult<TObject>(parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts on a dictionary member of an object using a lambda selector and assertion lambda.
    /// The assertion lambda receives dictionary assertion methods (ContainsKey, ContainsValue, IsEmpty, etc.).
    /// After the member assertion completes, returns to the parent object context for further chaining.
    /// Example: await Assert.That(myObject).Member(x => x.Attributes, attrs => attrs.ContainsKey("status").And.IsNotEmpty());
    /// </summary>
    [OverloadResolutionPriority(2)]
    public static MemberAssertionResult<TObject> Member<TObject, TKey, TValue>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, IReadOnlyDictionary<TKey, TValue>>> memberSelector,
        Func<DictionaryMemberAssertionAdapter<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>, Assertion<IReadOnlyDictionary<TKey, TValue>>> assertions)
        where TKey : notnull
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(memberSelector);

        parentContext.ExpressionBuilder.Append($".Member(x => x.{memberPath}, ...)");

        // Check if there's a pending link (from .And or .Or) that needs to be consumed
        var (pendingAssertion, combinerType) = parentContext.ConsumePendingLink();

        // Map to member context
        var memberContext = parentContext.Map<IReadOnlyDictionary<TKey, TValue>>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = memberSelector.Compile();
            return compiled(obj);
        });

        // Create a DictionaryMemberAssertionAdapter for the member
        var dictionaryAdapter = new DictionaryMemberAssertionAdapter<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(memberContext);
        var memberAssertion = assertions(dictionaryAdapter);

        // Type-erase to object? for storage
        var erasedAssertion = new TypeErasedAssertion<IReadOnlyDictionary<TKey, TValue>>(memberAssertion);

        // If there was a pending link, wrap both assertions together
        if (pendingAssertion != null && combinerType != null)
        {
            Assertion<object?> combinedAssertion = combinerType == CombinerType.And
                ? new CombinedAndAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion)
                : new CombinedOrAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion);

            return new MemberAssertionResult<TObject>(parentContext, combinedAssertion);
        }

        return new MemberAssertionResult<TObject>(parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts on a dictionary member of an object using a lambda selector and assertion lambda.
    /// The assertion lambda receives dictionary assertion methods (ContainsKey, ContainsValue, IsEmpty, etc.).
    /// After the member assertion completes, returns to the parent object context for further chaining.
    /// Example: await Assert.That(myObject).Member(x => x.Attributes, attrs => attrs.ContainsKey("status").And.IsNotEmpty());
    /// Note: This overload exists for backward compatibility. For AOT compatibility, use the TTransformed overload instead.
    /// </summary>
    [OverloadResolutionPriority(2)]
    [RequiresDynamicCode("Uses reflection for legacy compatibility. For AOT compatibility, use the Member<TObject, TKey, TValue, TTransformed> overload with strongly-typed assertions.")]
    public static MemberAssertionResult<TObject> Member<TObject, TKey, TValue>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, IReadOnlyDictionary<TKey, TValue>>> memberSelector,
        Func<DictionaryMemberAssertionAdapter<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>, object> assertions)
        where TKey : notnull
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(memberSelector);

        parentContext.ExpressionBuilder.Append($".Member(x => x.{memberPath}, ...)");

        // Check if there's a pending link (from .And or .Or) that needs to be consumed
        var (pendingAssertion, combinerType) = parentContext.ConsumePendingLink();

        // Map to member context
        var memberContext = parentContext.Map<IReadOnlyDictionary<TKey, TValue>>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = memberSelector.Compile();
            return compiled(obj);
        });

        // Create a DictionaryMemberAssertionAdapter for the member
        var dictionaryAdapter = new DictionaryMemberAssertionAdapter<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(memberContext);
        var memberAssertionObj = assertions(dictionaryAdapter);

        // Type-erase to object? for storage
        var erasedAssertion = WrapMemberAssertion(memberAssertionObj);

        // If there was a pending link, wrap both assertions together
        if (pendingAssertion != null && combinerType != null)
        {
            Assertion<object?> combinedAssertion = combinerType == CombinerType.And
                ? new CombinedAndAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion)
                : new CombinedOrAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion);

            return new MemberAssertionResult<TObject>(parentContext, combinedAssertion);
        }

        return new MemberAssertionResult<TObject>(parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts on a collection member of an object using a lambda selector and assertion lambda.
    /// The assertion lambda receives collection assertion methods (HasCount, Contains, IsEmpty, etc.).
    /// Supports type transformations like IsTypeOf within the assertion lambda.
    /// After the member assertion completes, returns to the parent object context for further chaining.
    /// Example: await Assert.That(myObject).Member(x => x.Tags, tags => tags.HasCount(1).And.Contains("value"));
    /// </summary>
    [OverloadResolutionPriority(2)]
    public static MemberAssertionResult<TObject> Member<TObject, TItem, TTransformed>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, IEnumerable<TItem>>> memberSelector,
        Func<CollectionMemberAssertionAdapter<IEnumerable<TItem>, TItem>, Assertion<TTransformed>> assertions)
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(memberSelector);

        parentContext.ExpressionBuilder.Append($".Member(x => x.{memberPath}, ...)");

        // Check if there's a pending link (from .And or .Or) that needs to be consumed
        var (pendingAssertion, combinerType) = parentContext.ConsumePendingLink();

        // Map to member context
        var memberContext = parentContext.Map<IEnumerable<TItem>>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = memberSelector.Compile();
            return compiled(obj);
        });

        // Create a CollectionMemberAssertionAdapter for the member
        var collectionAdapter = new CollectionMemberAssertionAdapter<IEnumerable<TItem>, TItem>(memberContext);
        var memberAssertion = assertions(collectionAdapter);

        // Type-erase to object? for storage - using TTransformed instead of collection type
        var erasedAssertion = new TypeErasedAssertion<TTransformed>(memberAssertion);

        // If there was a pending link, wrap both assertions together
        if (pendingAssertion != null && combinerType != null)
        {
            Assertion<object?> combinedAssertion = combinerType == CombinerType.And
                ? new CombinedAndAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion)
                : new CombinedOrAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion);

            return new MemberAssertionResult<TObject>(parentContext, combinedAssertion);
        }

        return new MemberAssertionResult<TObject>(parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts on a collection member of an object using a lambda selector and assertion lambda.
    /// The assertion lambda receives collection assertion methods (HasCount, Contains, IsEmpty, etc.).
    /// After the member assertion completes, returns to the parent object context for further chaining.
    /// Example: await Assert.That(myObject).Member(x => x.Tags, tags => tags.HasCount(1).And.Contains("value"));
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static MemberAssertionResult<TObject> Member<TObject, TItem>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, IEnumerable<TItem>>> memberSelector,
        Func<CollectionMemberAssertionAdapter<IEnumerable<TItem>, TItem>, Assertion<IEnumerable<TItem>>> assertions)
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(memberSelector);

        parentContext.ExpressionBuilder.Append($".Member(x => x.{memberPath}, ...)");

        // Check if there's a pending link (from .And or .Or) that needs to be consumed
        var (pendingAssertion, combinerType) = parentContext.ConsumePendingLink();

        // Map to member context
        var memberContext = parentContext.Map<IEnumerable<TItem>>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = memberSelector.Compile();
            return compiled(obj);
        });

        // Create a CollectionMemberAssertionAdapter for the member
        var collectionAdapter = new CollectionMemberAssertionAdapter<IEnumerable<TItem>, TItem>(memberContext);
        var memberAssertion = assertions(collectionAdapter);

        // Type-erase to object? for storage
        var erasedAssertion = new TypeErasedAssertion<IEnumerable<TItem>>(memberAssertion);

        // If there was a pending link, wrap both assertions together
        if (pendingAssertion != null && combinerType != null)
        {
            Assertion<object?> combinedAssertion = combinerType == CombinerType.And
                ? new CombinedAndAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion)
                : new CombinedOrAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion);

            return new MemberAssertionResult<TObject>(parentContext, combinedAssertion);
        }

        return new MemberAssertionResult<TObject>(parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts on a collection member of an object using a lambda selector and assertion lambda.
    /// The assertion lambda receives collection assertion methods (HasCount, Contains, IsEmpty, etc.).
    /// After the member assertion completes, returns to the parent object context for further chaining.
    /// Example: await Assert.That(myObject).Member(x => x.Tags, tags => tags.HasCount(1).And.Contains("value"));
    /// Note: This overload exists for backward compatibility. For AOT compatibility, use the TTransformed overload instead.
    /// </summary>
    [OverloadResolutionPriority(1)]
    [RequiresDynamicCode("Uses reflection for legacy compatibility. For AOT compatibility, use the Member<TObject, TItem, TTransformed> overload with strongly-typed assertions.")]
    public static MemberAssertionResult<TObject> Member<TObject, TItem>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, IEnumerable<TItem>>> memberSelector,
        Func<CollectionMemberAssertionAdapter<IEnumerable<TItem>, TItem>, object> assertions)
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(memberSelector);

        parentContext.ExpressionBuilder.Append($".Member(x => x.{memberPath}, ...)");

        // Check if there's a pending link (from .And or .Or) that needs to be consumed
        var (pendingAssertion, combinerType) = parentContext.ConsumePendingLink();

        // Map to member context
        var memberContext = parentContext.Map<IEnumerable<TItem>>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = memberSelector.Compile();
            return compiled(obj);
        });

        // Create a CollectionMemberAssertionAdapter for the member
        var collectionAdapter = new CollectionMemberAssertionAdapter<IEnumerable<TItem>, TItem>(memberContext);
        var memberAssertionObj = assertions(collectionAdapter);

        // Type-erase to object? for storage
        var erasedAssertion = WrapMemberAssertion(memberAssertionObj);

        // If there was a pending link, wrap both assertions together
        if (pendingAssertion != null && combinerType != null)
        {
            Assertion<object?> combinedAssertion = combinerType == CombinerType.And
                ? new CombinedAndAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion)
                : new CombinedOrAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion);

            return new MemberAssertionResult<TObject>(parentContext, combinedAssertion);
        }

        return new MemberAssertionResult<TObject>(parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts on a member of an object using a lambda selector and assertion lambda.
    /// The assertion lambda receives the member value and can perform any assertions on it.
    /// Supports type transformations like IsTypeOf within the assertion lambda.
    /// After the member assertion completes, returns to the parent object context for further chaining.
    /// Example: await Assert.That(myObject).Member(x => x.PropertyName, value => value.IsTypeOf<string>().And.IsEqualTo(expectedValue));
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static MemberAssertionResult<TObject> Member<TObject, TMember, TTransformed>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, TMember>> memberSelector,
        Func<IAssertionSource<TMember>, Assertion<TTransformed>> assertions)
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(memberSelector);

        parentContext.ExpressionBuilder.Append($".Member(x => x.{memberPath}, ...)");

        // Check if there's a pending link (from .And or .Or) that needs to be consumed
        var (pendingAssertion, combinerType) = parentContext.ConsumePendingLink();

        // Map to member context
        var memberContext = parentContext.Map<TMember>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = memberSelector.Compile();
            return compiled(obj);
        });

        // Let user build assertion via lambda
        var memberSource = new AssertionSourceAdapter<TMember>(memberContext);
        var memberAssertion = assertions(memberSource);

        // Type-erase to object? for storage - using TTransformed instead of member type
        var erasedAssertion = new TypeErasedAssertion<TTransformed>(memberAssertion);

        // If there was a pending link, wrap both assertions together
        if (pendingAssertion != null && combinerType != null)
        {
            Assertion<object?> combinedAssertion = combinerType == CombinerType.And
                ? new CombinedAndAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion)
                : new CombinedOrAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion);

            return new MemberAssertionResult<TObject>(parentContext, combinedAssertion);
        }

        return new MemberAssertionResult<TObject>(parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts on a member of an object using a lambda selector and assertion lambda.
    /// The assertion lambda receives the member value and can perform any assertions on it.
    /// After the member assertion completes, returns to the parent object context for further chaining.
    /// Example: await Assert.That(myObject).Member(x => x.PropertyName, value => value.IsEqualTo(expectedValue));
    /// </summary>
    public static MemberAssertionResult<TObject> Member<TObject, TMember>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, TMember>> memberSelector,
        Func<IAssertionSource<TMember>, Assertion<TMember>> assertions)
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(memberSelector);

        parentContext.ExpressionBuilder.Append($".Member(x => x.{memberPath}, ...)");

        // Check if there's a pending link (from .And or .Or) that needs to be consumed
        var (pendingAssertion, combinerType) = parentContext.ConsumePendingLink();

        // Map to member context
        var memberContext = parentContext.Map<TMember>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = memberSelector.Compile();
            return compiled(obj);
        });

        // Let user build assertion via lambda
        var memberSource = new AssertionSourceAdapter<TMember>(memberContext);
        var memberAssertion = assertions(memberSource);

        // Type-erase to object? for storage
        var erasedAssertion = new TypeErasedAssertion<TMember>(memberAssertion);

        // If there was a pending link, wrap both assertions together
        if (pendingAssertion != null && combinerType != null)
        {
            Assertion<object?> combinedAssertion = combinerType == CombinerType.And
                ? new CombinedAndAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion)
                : new CombinedOrAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion);

            return new MemberAssertionResult<TObject>(parentContext, combinedAssertion);
        }

        return new MemberAssertionResult<TObject>(parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts on a member of an object using a lambda selector and assertion lambda.
    /// The assertion lambda receives the member value and can perform any assertions on it.
    /// After the member assertion completes, returns to the parent object context for further chaining.
    /// Example: await Assert.That(myObject).Member(x => x.PropertyName, value => value.IsEqualTo(expectedValue));
    /// Note: This overload exists for backward compatibility. For AOT compatibility, use the TTransformed overload instead.
    /// </summary>
    [RequiresDynamicCode("Uses reflection for legacy compatibility. For AOT compatibility, use the Member<TObject, TMember, TTransformed> overload with strongly-typed assertions.")]
    public static MemberAssertionResult<TObject> Member<TObject, TMember>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, TMember>> memberSelector,
        Func<IAssertionSource<TMember>, object> assertions)
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(memberSelector);

        parentContext.ExpressionBuilder.Append($".Member(x => x.{memberPath}, ...)");

        // Check if there's a pending link (from .And or .Or) that needs to be consumed
        var (pendingAssertion, combinerType) = parentContext.ConsumePendingLink();

        // Map to member context
        var memberContext = parentContext.Map<TMember>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = memberSelector.Compile();
            return compiled(obj);
        });

        // Let user build assertion via lambda
        var memberSource = new AssertionSourceAdapter<TMember>(memberContext);
        var memberAssertionObj = assertions(memberSource);

        // Type-erase to object? for storage
        var erasedAssertion = WrapMemberAssertion(memberAssertionObj);

        // If there was a pending link, wrap both assertions together
        if (pendingAssertion != null && combinerType != null)
        {
            Assertion<object?> combinedAssertion = combinerType == CombinerType.And
                ? new CombinedAndAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion)
                : new CombinedOrAssertion<TObject>(parentContext, pendingAssertion, erasedAssertion);

            return new MemberAssertionResult<TObject>(parentContext, combinedAssertion);
        }

        return new MemberAssertionResult<TObject>(parentContext, erasedAssertion);
    }

    /// <summary>
    /// Helper method to wrap member assertions for type erasure.
    /// Uses reflection to handle assertions of any type, including type-transformed assertions.
    /// Note: This fallback path uses reflection for legacy object-based overloads.
    /// New code should use the TTransformed overloads which are AOT-compatible.
    /// </summary>
    [RequiresDynamicCode("Uses reflection to dynamically construct TypeErasedAssertion<T>. For AOT compatibility, use the strongly-typed TTransformed overloads instead of object-returning lambdas.")]
    private static Assertion<object?> WrapMemberAssertion(object memberAssertion)
    {
        if (memberAssertion is null)
        {
            throw new InvalidOperationException("Member assertion cannot be null.");
        }

        var type = memberAssertion.GetType();

        // Walk up the inheritance chain to find the Assertion<T> base class
        Type? assertionBaseType = null;
        var currentType = type;
        while (currentType != null && currentType != typeof(object))
        {
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Assertion<>))
            {
                assertionBaseType = currentType;
                break;
            }
            currentType = currentType.BaseType;
        }

        if (assertionBaseType != null)
        {
            // Extract the generic type parameter from Assertion<T>
            var memberType = assertionBaseType.GetGenericArguments()[0];

            // Create TypeErasedAssertion<T> dynamically using the discovered type
            var typeErasedAssertionType = typeof(TypeErasedAssertion<>).MakeGenericType(memberType);
            return (Assertion<object?>)Activator.CreateInstance(typeErasedAssertionType, memberAssertion)!;
        }

        throw new InvalidOperationException(
            $"Member assertion returned unexpected type: {type.Name}. " +
            "Expected a type inheriting from Assertion<T>.");
    }

    private static string GetMemberPath<TObject, TMember>(Expression<Func<TObject, TMember>> expression)
    {
        var body = expression.Body;
        var parts = new List<string>();

        while (body is MemberExpression memberExpr)
        {
            parts.Insert(0, memberExpr.Member.Name);
            body = memberExpr.Expression;
        }

        return parts.Count > 0 ? string.Join(".", parts) : "Unknown";
    }

    /// <summary>
    /// Gets the length of the string as an integer for numeric assertions.
    /// Example: await Assert.That(str).Length().IsGreaterThan(5);
    /// </summary>
    public static StringLengthValueAssertion Length(
        this IAssertionSource<string> source)
    {
        source.Context.ExpressionBuilder.Append(".Length()");
        return new StringLengthValueAssertion(source.Context);
    }

    /// <summary>
    /// Returns a wrapper for string length assertions.
    /// Example: await Assert.That(str).HasLength().EqualTo(5);
    /// </summary>
    [Obsolete("Use Length() instead, which provides all numeric assertion methods. Example: Assert.That(str).Length().IsGreaterThan(5)")]
    public static LengthWrapper HasLength(
        this IAssertionSource<string> source)
    {
        source.Context.ExpressionBuilder.Append(".HasLength()");
        return new LengthWrapper(source.Context);
    }

    /// <summary>
    /// Asserts that the string has the expected length.
    /// Example: await Assert.That(str).HasLength(5);
    /// </summary>
    [Obsolete("Use Length().IsEqualTo(expectedLength) instead.")]
    public static StringLengthAssertion HasLength(
        this IAssertionSource<string> source,
        int expectedLength,
        [CallerArgumentExpression(nameof(expectedLength))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".HasLength({expression})");
        return new StringLengthAssertion(source.Context, expectedLength);
    }

    /// <summary>
    /// Asserts that the value is structurally equivalent to the expected value.
    /// Performs deep comparison of properties and fields.
    /// Supports .WithPartialEquivalency() and .IgnoringMember() for advanced scenarios.
    /// </summary>
    [RequiresUnreferencedCode("Uses reflection to compare members")]
    public static StructuralEquivalencyAssertion<TValue> IsEquivalentTo<TValue>(
        this IAssertionSource<TValue> source,
        object? expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsEquivalentTo({expression})");
        return new StructuralEquivalencyAssertion<TValue>(source.Context, expected, expression);
    }

    /// <summary>
    /// Asserts that the value is NOT structurally equivalent to the expected value.
    /// Performs deep comparison of properties and fields.
    /// Supports .WithPartialEquivalency() and .IgnoringMember() for advanced scenarios.
    /// </summary>
    [RequiresUnreferencedCode("Uses reflection to compare members")]
    public static NotStructuralEquivalencyAssertion<TValue> IsNotEquivalentTo<TValue>(
        this IAssertionSource<TValue> source,
        object? expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsNotEquivalentTo({expression})");
        return new NotStructuralEquivalencyAssertion<TValue>(source.Context, expected, expression);
    }

    /// <summary>
    /// Asserts that a mapped Task value satisfies custom assertions on the unwrapped result.
    /// Maps the source value using a selector that returns a Task, then runs assertions on the awaited result.
    /// Supports both same-type and type-changing assertions.
    /// Example: await Assert.That(model).SatisfiesAsync(m => m.AsyncValue, assert => assert.IsEqualTo("Hello"));
    /// </summary>
    public static AsyncMappedSatisfiesAssertion<TValue, TMapped> SatisfiesAsync<TValue, TMapped>(
        this IAssertionSource<TValue> source,
        Func<TValue?, Task<TMapped?>> selector,
        Func<ValueAssertion<TMapped>, IAssertion?> assertions,
        [CallerArgumentExpression(nameof(selector))] string? selectorExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".SatisfiesAsync({selectorExpression}, ...)");
        return new AsyncMappedSatisfiesAssertion<TValue, TMapped>(
            source.Context,
            selector!,
            assertions,
            selectorExpression ?? "selector");
    }

    /// <summary>
    /// Asserts that a mapped value satisfies custom assertions.
    /// Maps the source value using a selector, then runs assertions on the mapped value.
    /// Supports both same-type and type-changing assertions.
    /// Example: await Assert.That(model).Satisfies(m => m.Name, assert => assert.IsEqualTo("John"));
    /// </summary>
    public static MappedSatisfiesAssertion<TValue, TMapped> Satisfies<TValue, TMapped>(
        this IAssertionSource<TValue> source,
        Func<TValue?, TMapped> selector,
        Func<ValueAssertion<TMapped>, IAssertion?> assertions,
        [CallerArgumentExpression(nameof(selector))] string? selectorExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".Satisfies({selectorExpression}, ...)");
        return new MappedSatisfiesAssertion<TValue, TMapped>(
            source.Context,
            selector,
            assertions,
            selectorExpression ?? "selector");
    }

    /// <summary>
    /// Asserts that the value satisfies the specified predicate.
    /// Example: await Assert.That(x).Satisfies(v => v > 0 && v < 100);
    /// </summary>
    public static SatisfiesAssertion<TValue> Satisfies<TValue>(
        this IAssertionSource<TValue> source,
        Func<TValue?, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".Satisfies({expression})");
        return new SatisfiesAssertion<TValue>(source.Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the delegate throws the specified exception type (or subclass).
    /// Only available on delegate-based assertions for type safety.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public static ThrowsAssertion<TException> Throws<TException, TValue>(
        this IDelegateAssertionSource<TValue> source)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        // Use MapException to move exception from exception field to value field
        var mappedContext = source.Context.MapException<TException>();
        return new ThrowsAssertion<TException>(mappedContext);
    }

    /// <summary>
    /// Alias for Throws - asserts that the delegate throws the specified exception type.
    /// Only available on delegate-based assertions for type safety.
    /// Example: await Assert.That(() => ThrowingMethod()).ThrowsException&lt;InvalidOperationException&gt;();
    /// </summary>
    public static ThrowsAssertion<TException> ThrowsException<TException, TValue>(
        this IDelegateAssertionSource<TValue> source)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".ThrowsException<{typeof(TException).Name}>()");
        var mappedContext = source.Context.MapException<TException>();
        return new ThrowsAssertion<TException>(mappedContext);
    }

    /// <summary>
    /// Asserts that the delegate throws any exception.
    /// Only available on delegate-based assertions for type safety.
    /// Example: await Assert.That(() => ThrowingMethod()).ThrowsException();
    /// </summary>
    public static ThrowsAssertion<Exception> ThrowsException<TValue>(
        this IDelegateAssertionSource<TValue> source)
    {
        source.Context.ExpressionBuilder.Append(".ThrowsException()");
        var mappedContext = source.Context.MapException<Exception>();
        return new ThrowsAssertion<Exception>(mappedContext);
    }

    /// <summary>
    /// Asserts that the async delegate throws the specified exception type (or subclass).
    /// Only available on delegate-based assertions for type safety.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).ThrowsAsync&lt;InvalidOperationException&gt;();
    /// </summary>
    public static ThrowsAssertion<TException> ThrowsAsync<TValue, TException>(
        this IDelegateAssertionSource<TValue> source)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".ThrowsAsync<{typeof(TException).Name}>()");
        var mappedContext = source.Context.MapException<TException>();
        return new ThrowsAssertion<TException>(mappedContext);
    }

    /// <summary>
    /// Asserts that the delegate throws exactly the specified exception type (not subclasses).
    /// Only available on delegate-based assertions for type safety.
    /// Example: await Assert.That(() => ThrowingMethod()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public static ThrowsExactlyAssertion<TException> ThrowsExactly<TException, TValue>(
        this IDelegateAssertionSource<TValue> source)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = source.Context.MapException<TException>();
        return new ThrowsExactlyAssertion<TException>(mappedContext);
    }

    /// <summary>
    /// Asserts that the delegate does not throw any exception and returns the actual value.
    /// Only available on delegate-based assertions for type safety.
    /// Example: await Assert.That(() => SafeMethod()).ThrowsNothing();
    /// </summary>
    public static ThrowsNothingAssertion<TValue> ThrowsNothing<TValue>(
        this IDelegateAssertionSource<TValue> source)
    {
        source.Context.ExpressionBuilder.Append(".ThrowsNothing()");
        // Preserve the value so it can be returned after the assertion
        return new ThrowsNothingAssertion<TValue>(source.Context);
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().WithMessageContaining("error");
    /// </summary>
    public static ExceptionMessageContainsAssertion<TException> WithMessageContaining<TException>(
        this IAssertionSource<TException> source,
        string expectedSubstring,
        [CallerArgumentExpression(nameof(expectedSubstring))] string? expression = null)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".WithMessageContaining({expression})");
        return new ExceptionMessageContainsAssertion<TException>(source.Context, expectedSubstring);
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring using the specified comparison.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().WithMessageContaining("error", StringComparison.OrdinalIgnoreCase);
    /// </summary>
    public static ExceptionMessageContainsAssertion<TException> WithMessageContaining<TException>(
        this IAssertionSource<TException> source,
        string expectedSubstring,
        StringComparison comparison,
        [CallerArgumentExpression(nameof(expectedSubstring))] string? expression = null)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".WithMessageContaining({expression}, StringComparison.{comparison})");
        return new ExceptionMessageContainsAssertion<TException>(source.Context, expectedSubstring, comparison);
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().HasMessageContaining("error");
    /// </summary>
    public static ExceptionMessageContainsAssertion<TException> HasMessageContaining<TException>(
        this IAssertionSource<TException> source,
        string expectedSubstring,
        [CallerArgumentExpression(nameof(expectedSubstring))] string? expression = null)
        where TException : Exception
    {
        return source.WithMessageContaining(expectedSubstring, expression);
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring using the specified comparison.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().HasMessageContaining("error", StringComparison.OrdinalIgnoreCase);
    /// </summary>
    public static ExceptionMessageContainsAssertion<TException> HasMessageContaining<TException>(
        this IAssertionSource<TException> source,
        string expectedSubstring,
        StringComparison comparison,
        [CallerArgumentExpression(nameof(expectedSubstring))] string? expression = null)
        where TException : Exception
    {
        return source.WithMessageContaining(expectedSubstring, comparison, expression);
    }

    /// <summary>
    /// Asserts that the exception message exactly equals the specified string.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().WithMessage("exact message");
    /// </summary>
    public static ExceptionMessageEqualsAssertion<TException> WithMessage<TException>(
        this IAssertionSource<TException> source,
        string expectedMessage,
        [CallerArgumentExpression(nameof(expectedMessage))] string? expression = null)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".WithMessage({expression})");
        return new ExceptionMessageEqualsAssertion<TException>(source.Context, expectedMessage);
    }

    /// <summary>
    /// Asserts that the exception message exactly equals the specified string using the specified comparison.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().WithMessage("exact message", StringComparison.OrdinalIgnoreCase);
    /// </summary>
    public static ExceptionMessageEqualsAssertion<TException> WithMessage<TException>(
        this IAssertionSource<TException> source,
        string expectedMessage,
        StringComparison comparison,
        [CallerArgumentExpression(nameof(expectedMessage))] string? expression = null)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".WithMessage({expression}, StringComparison.{comparison})");
        return new ExceptionMessageEqualsAssertion<TException>(source.Context, expectedMessage, comparison);
    }

    /// <summary>
    /// Asserts that the exception message does NOT contain the specified substring.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().WithMessageNotContaining("should not appear");
    /// </summary>
    public static ExceptionMessageNotContainsAssertion<TException> WithMessageNotContaining<TException>(
        this IAssertionSource<TException> source,
        string notExpectedSubstring,
        [CallerArgumentExpression(nameof(notExpectedSubstring))] string? expression = null)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".WithMessageNotContaining({expression})");
        return new ExceptionMessageNotContainsAssertion<TException>(source.Context, notExpectedSubstring);
    }

    /// <summary>
    /// Asserts that the exception message does NOT contain the specified substring using the specified comparison.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().WithMessageNotContaining("should not appear", StringComparison.OrdinalIgnoreCase);
    /// </summary>
    public static ExceptionMessageNotContainsAssertion<TException> WithMessageNotContaining<TException>(
        this IAssertionSource<TException> source,
        string notExpectedSubstring,
        StringComparison comparison,
        [CallerArgumentExpression(nameof(notExpectedSubstring))] string? expression = null)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".WithMessageNotContaining({expression}, StringComparison.{comparison})");
        return new ExceptionMessageNotContainsAssertion<TException>(source.Context, notExpectedSubstring, comparison);
    }

    /// <summary>
    /// Asserts that the exception message matches a wildcard pattern.
    /// * matches any number of characters, ? matches a single character.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().WithMessageMatching("Error: *");
    /// </summary>
    public static ExceptionMessageMatchesPatternAssertion<TException> WithMessageMatching<TException>(
        this IAssertionSource<TException> source,
        string pattern,
        [CallerArgumentExpression(nameof(pattern))] string? expression = null)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".WithMessageMatching({expression})");
        return new ExceptionMessageMatchesPatternAssertion<TException>(source.Context, pattern);
    }

    /// <summary>
    /// Asserts that the exception message matches a StringMatcher pattern.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;Exception&gt;().WithMessageMatching(StringMatcher.Regex("Error.*"));
    /// </summary>
    public static ExceptionMessageMatchesAssertion<TException> WithMessageMatching<TException>(
        this IAssertionSource<TException> source,
        StringMatcher matcher,
        [CallerArgumentExpression(nameof(matcher))] string? expression = null)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".WithMessageMatching({expression})");
        return new ExceptionMessageMatchesAssertion<TException>(source.Context, matcher);
    }

    /// <summary>
    /// Asserts that an ArgumentException has the specified parameter name.
    /// Works after Throws assertions.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;ArgumentException&gt;().WithParameterName("paramName");
    /// </summary>
    public static ExceptionParameterNameAssertion<TException> WithParameterName<TException>(
        this IAssertionSource<TException> source,
        string expectedParameterName,
        [CallerArgumentExpression(nameof(expectedParameterName))] string? expression = null)
        where TException : Exception
    {
        source.Context.ExpressionBuilder.Append($".WithParameterName({expression})");
        return new ExceptionParameterNameAssertion<TException>(source.Context, expectedParameterName);
    }

    public static ThrowsAssertion<TException> Throws<TException>(this DelegateAssertion source) where TException : Exception
    {
        var iface = (IAssertionSource<object?>)source;
        iface.Context.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.MapException<TException>();
        return new ThrowsAssertion<TException>(mappedContext);
    }

    public static ThrowsExactlyAssertion<TException> ThrowsExactly<TException>(this DelegateAssertion source) where TException : Exception
    {
        var iface = (IAssertionSource<object?>)source;
        iface.Context.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.MapException<TException>();
        return new ThrowsExactlyAssertion<TException>(mappedContext);
    }

    public static ThrowsAssertion<TException> Throws<TException>(this AsyncDelegateAssertion source) where TException : Exception
    {
        var iface = (IAssertionSource<object?>)source;
        iface.Context.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.MapException<TException>();
        return new ThrowsAssertion<TException>(mappedContext);
    }

    public static ThrowsExactlyAssertion<TException> ThrowsExactly<TException>(this AsyncDelegateAssertion source) where TException : Exception
    {
        var iface = (IAssertionSource<object?>)source;
        iface.Context.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = iface.Context.MapException<TException>();
        return new ThrowsExactlyAssertion<TException>(mappedContext);
    }

    /// <summary>
    /// Asserts that an exception's Message property exactly equals the expected string.
    /// Works with both direct exception assertions and chained exception assertions (via .And).
    /// </summary>
    public static HasMessageEqualToAssertion<TValue> HasMessageEqualTo<TValue>(
        this IAssertionSource<TValue> source,
        string expectedMessage)
    {
        source.Context.ExpressionBuilder.Append($".HasMessageEqualTo(\"{expectedMessage}\")");
        return new HasMessageEqualToAssertion<TValue>(source.Context, expectedMessage);
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
        source.Context.ExpressionBuilder.Append($".HasMessageEqualTo(\"{expectedMessage}\", StringComparison.{comparison})");
        return new HasMessageEqualToAssertion<TValue>(source.Context, expectedMessage,  comparison);
    }


    /// <summary>
    /// Asserts that an exception's Message property starts with the expected string.
    /// Works with both direct exception assertions and chained exception assertions (via .And).
    /// </summary>
    public static HasMessageStartingWithAssertion<TValue> HasMessageStartingWith<TValue>(
        this IAssertionSource<TValue> source,
        string expectedPrefix)
    {
        source.Context.ExpressionBuilder.Append($".HasMessageStartingWith(\"{expectedPrefix}\")");
        return new HasMessageStartingWithAssertion<TValue>(source.Context, expectedPrefix);
    }

    /// <summary>
    /// Asserts that an exception's Message property starts with the expected string using the specified string comparison.
    /// Works with both direct exception assertions and chained exception assertions (via .And).
    /// </summary>
    public static HasMessageStartingWithAssertion<TValue> HasMessageStartingWith<TValue>(
        this IAssertionSource<TValue> source,
        string expectedPrefix,
        StringComparison comparison)
    {
        source.Context.ExpressionBuilder.Append($".HasMessageStartingWith(\"{expectedPrefix}\", StringComparison.{comparison})");
        return new HasMessageStartingWithAssertion<TValue>(source.Context, expectedPrefix,  comparison);
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
        source.Context.ExpressionBuilder.Append($".IsAfterOrEqualTo({expression})");
        return new GreaterThanOrEqualAssertion<DateTime>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateTime is after the expected DateTime.
    /// Alias for IsGreaterThan for better readability with dates.
    /// </summary>
    public static GreaterThanAssertion<DateTime> IsAfter(
        this IAssertionSource<DateTime> source,
        DateTime expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsAfter({expression})");
        return new GreaterThanAssertion<DateTime>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateTime is before the expected DateTime.
    /// Alias for IsLessThan for better readability with dates.
    /// </summary>
    public static LessThanAssertion<DateTime> IsBefore(
        this IAssertionSource<DateTime> source,
        DateTime expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsBefore({expression})");
        return new LessThanAssertion<DateTime>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateTime is before or equal to the expected DateTime.
    /// Alias for IsLessThanOrEqualTo for better readability with dates.
    /// </summary>
    public static LessThanOrEqualAssertion<DateTime> IsBeforeOrEqualTo(
        this IAssertionSource<DateTime> source,
        DateTime expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsBeforeOrEqualTo({expression})");
        return new LessThanOrEqualAssertion<DateTime>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateTimeOffset is after the expected DateTimeOffset.
    /// Alias for IsGreaterThan for better readability with dates.
    /// </summary>
    public static GreaterThanAssertion<DateTimeOffset> IsAfter(
        this IAssertionSource<DateTimeOffset> source,
        DateTimeOffset expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsAfter({expression})");
        return new GreaterThanAssertion<DateTimeOffset>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateTimeOffset is before the expected DateTimeOffset.
    /// Alias for IsLessThan for better readability with dates.
    /// </summary>
    public static LessThanAssertion<DateTimeOffset> IsBefore(
        this IAssertionSource<DateTimeOffset> source,
        DateTimeOffset expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsBefore({expression})");
        return new LessThanAssertion<DateTimeOffset>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateTimeOffset is after or equal to the expected DateTimeOffset.
    /// Alias for IsGreaterThanOrEqualTo for better readability with dates.
    /// </summary>
    public static GreaterThanOrEqualAssertion<DateTimeOffset> IsAfterOrEqualTo(
        this IAssertionSource<DateTimeOffset> source,
        DateTimeOffset expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsAfterOrEqualTo({expression})");
        return new GreaterThanOrEqualAssertion<DateTimeOffset>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateTimeOffset is before or equal to the expected DateTimeOffset.
    /// Alias for IsLessThanOrEqualTo for better readability with dates.
    /// </summary>
    public static LessThanOrEqualAssertion<DateTimeOffset> IsBeforeOrEqualTo(
        this IAssertionSource<DateTimeOffset> source,
        DateTimeOffset expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsBeforeOrEqualTo({expression})");
        return new LessThanOrEqualAssertion<DateTimeOffset>(source.Context, expected);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Asserts that the DateOnly is after the expected DateOnly.
    /// Alias for IsGreaterThan for better readability with dates.
    /// </summary>
    public static GreaterThanAssertion<DateOnly> IsAfter(
        this IAssertionSource<DateOnly> source,
        DateOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsAfter({expression})");
        return new GreaterThanAssertion<DateOnly>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateOnly is before the expected DateOnly.
    /// Alias for IsLessThan for better readability with dates.
    /// </summary>
    public static LessThanAssertion<DateOnly> IsBefore(
        this IAssertionSource<DateOnly> source,
        DateOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsBefore({expression})");
        return new LessThanAssertion<DateOnly>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateOnly is after or equal to the expected DateOnly.
    /// Alias for IsGreaterThanOrEqualTo for better readability with dates.
    /// </summary>
    public static GreaterThanOrEqualAssertion<DateOnly> IsAfterOrEqualTo(
        this IAssertionSource<DateOnly> source,
        DateOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsAfterOrEqualTo({expression})");
        return new GreaterThanOrEqualAssertion<DateOnly>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the DateOnly is before or equal to the expected DateOnly.
    /// Alias for IsLessThanOrEqualTo for better readability with dates.
    /// </summary>
    public static LessThanOrEqualAssertion<DateOnly> IsBeforeOrEqualTo(
        this IAssertionSource<DateOnly> source,
        DateOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsBeforeOrEqualTo({expression})");
        return new LessThanOrEqualAssertion<DateOnly>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the TimeOnly is after the expected TimeOnly.
    /// Alias for IsGreaterThan for better readability with times.
    /// </summary>
    public static GreaterThanAssertion<TimeOnly> IsAfter(
        this IAssertionSource<TimeOnly> source,
        TimeOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsAfter({expression})");
        return new GreaterThanAssertion<TimeOnly>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the TimeOnly is before the expected TimeOnly.
    /// Alias for IsLessThan for better readability with times.
    /// </summary>
    public static LessThanAssertion<TimeOnly> IsBefore(
        this IAssertionSource<TimeOnly> source,
        TimeOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsBefore({expression})");
        return new LessThanAssertion<TimeOnly>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the TimeOnly is after or equal to the expected TimeOnly.
    /// Alias for IsGreaterThanOrEqualTo for better readability with times.
    /// </summary>
    public static GreaterThanOrEqualAssertion<TimeOnly> IsAfterOrEqualTo(
        this IAssertionSource<TimeOnly> source,
        TimeOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsAfterOrEqualTo({expression})");
        return new GreaterThanOrEqualAssertion<TimeOnly>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the TimeOnly is before or equal to the expected TimeOnly.
    /// Alias for IsLessThanOrEqualTo for better readability with times.
    /// </summary>
    public static LessThanOrEqualAssertion<TimeOnly> IsBeforeOrEqualTo(
        this IAssertionSource<TimeOnly> source,
        TimeOnly expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".IsBeforeOrEqualTo({expression})");
        return new LessThanOrEqualAssertion<TimeOnly>(source.Context, expected);
    }
#endif

    /// <summary>
    /// Asserts that a synchronous delegate completes execution within the specified timeout.
    /// If the delegate takes longer than the timeout, the assertion fails.
    /// </summary>
    public static CompletesWithinActionAssertion CompletesWithin(
        this DelegateAssertion source,
        TimeSpan timeout,
        [CallerArgumentExpression(nameof(timeout))] string? expression = null)
    {
        var action = GetActionFromDelegate(source);
        var assertionSource = (IAssertionSource<object?>)source;
        assertionSource.Context.ExpressionBuilder.Append($".CompletesWithin({expression})");
        return new CompletesWithinActionAssertion(action, timeout);
    }

    /// <summary>
    /// Asserts that an asynchronous delegate completes execution within the specified timeout.
    /// If the delegate takes longer than the timeout, the assertion fails.
    /// </summary>
    public static CompletesWithinAsyncAssertion CompletesWithin(
        this AsyncDelegateAssertion source,
        TimeSpan timeout,
        [CallerArgumentExpression(nameof(timeout))] string? expression = null)
    {
        var asyncAction = GetFuncFromAsyncDelegate(source);
        var assertionSource = (IAssertionSource<object?>)source;
        assertionSource.Context.ExpressionBuilder.Append($".CompletesWithin({expression})");
        return new CompletesWithinAsyncAssertion(asyncAction, timeout);
    }

    /// <summary>
    /// Asserts that an assertion passes within the specified timeout by polling repeatedly.
    /// The assertion builder is invoked on each polling attempt until it passes or the timeout expires.
    /// Useful for testing asynchronous or event-driven code where state changes take time to propagate.
    /// Example: await Assert.That(value).WaitsFor(assert => assert.IsEqualTo(2), timeout: TimeSpan.FromSeconds(5));
    /// </summary>
    /// <typeparam name="TValue">The type of value being asserted</typeparam>
    /// <param name="source">The assertion source</param>
    /// <param name="assertionBuilder">A function that builds the assertion to be evaluated on each poll</param>
    /// <param name="timeout">The maximum time to wait for the assertion to pass</param>
    /// <param name="pollingInterval">The interval between polling attempts (defaults to 10ms if not specified)</param>
    /// <param name="timeoutExpression">Captured expression for the timeout parameter</param>
    /// <param name="pollingIntervalExpression">Captured expression for the polling interval parameter</param>
    /// <returns>An assertion that can be awaited or chained with And/Or</returns>
    public static WaitsForAssertion<TValue> WaitsFor<TValue>(
        this IAssertionSource<TValue> source,
        Func<IAssertionSource<TValue>, Assertion<TValue>> assertionBuilder,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null,
        [CallerArgumentExpression(nameof(timeout))] string? timeoutExpression = null,
        [CallerArgumentExpression(nameof(pollingInterval))] string? pollingIntervalExpression = null)
    {
        var intervalExpr = pollingInterval.HasValue ? $", pollingInterval: {pollingIntervalExpression}" : "";
        source.Context.ExpressionBuilder.Append($".WaitsFor(..., timeout: {timeoutExpression}{intervalExpr})");
        return new WaitsForAssertion<TValue>(source.Context, assertionBuilder, timeout, pollingInterval);
    }

    private static Action GetActionFromDelegate(DelegateAssertion source)
    {
        return source.Action;
    }

    private static Func<Task> GetFuncFromAsyncDelegate(AsyncDelegateAssertion source)
    {
        return source.AsyncAction;
    }

    /// <summary>
    /// Asserts that a string can be parsed into the specified type.
    /// </summary>
    public static Assertions.Strings.IsParsableIntoAssertion<T> IsParsableInto<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] T>(
        this IAssertionSource<string> source)
    {
        source.Context.ExpressionBuilder.Append($".IsParsableInto<{typeof(T).Name}>()");
        return new Assertions.Strings.IsParsableIntoAssertion<T>(source.Context);
    }

    /// <summary>
    /// Asserts that a string cannot be parsed into the specified type.
    /// </summary>
    public static Assertions.Strings.IsNotParsableIntoAssertion<T> IsNotParsableInto<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] T>(
        this IAssertionSource<string> source)
    {
        source.Context.ExpressionBuilder.Append($".IsNotParsableInto<{typeof(T).Name}>()");
        return new Assertions.Strings.IsNotParsableIntoAssertion<T>(source.Context);
    }

    /// <summary>
    /// Parses a string into the specified type and returns an assertion on the parsed value.
    /// This allows chaining assertions on the parsed result.
    /// </summary>
    public static Assertions.Strings.WhenParsedIntoAssertion<T> WhenParsedInto<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] T>(
        this IAssertionSource<string> source)
    {
        source.Context.ExpressionBuilder.Append($".WhenParsedInto<{typeof(T).Name}>()");
        return new Assertions.Strings.WhenParsedIntoAssertion<T>(source.Context);
    }

    /// <summary>
    /// Asserts that a flags enum has the specified flag set.
    /// </summary>
    public static Assertions.Enums.HasFlagAssertion<TEnum> HasFlag<TEnum>(
        this IAssertionSource<TEnum> source,
        TEnum expectedFlag,
        [CallerArgumentExpression(nameof(expectedFlag))] string? expression = null)
        where TEnum : struct, Enum
    {
        source.Context.ExpressionBuilder.Append($".HasFlag({expression})");
        return new Assertions.Enums.HasFlagAssertion<TEnum>(source.Context, expectedFlag);
    }

    /// <summary>
    /// Asserts that a flags enum does NOT have the specified flag set.
    /// </summary>
    public static Assertions.Enums.DoesNotHaveFlagAssertion<TEnum> DoesNotHaveFlag<TEnum>(
        this IAssertionSource<TEnum> source,
        TEnum unexpectedFlag,
        [CallerArgumentExpression(nameof(unexpectedFlag))] string? expression = null)
        where TEnum : struct, Enum
    {
        source.Context.ExpressionBuilder.Append($".DoesNotHaveFlag({expression})");
        return new Assertions.Enums.DoesNotHaveFlagAssertion<TEnum>(source.Context, unexpectedFlag);
    }

    /// <summary>
    /// Asserts that an enum value is defined in its enum type.
    /// </summary>
    public static Assertions.Enums.IsDefinedAssertion<TEnum> IsDefined<TEnum>(
        this IAssertionSource<TEnum> source)
        where TEnum : struct, Enum
    {
        source.Context.ExpressionBuilder.Append(".IsDefined()");
        return new Assertions.Enums.IsDefinedAssertion<TEnum>(source.Context);
    }

    /// <summary>
    /// Asserts that an enum value is NOT defined in its enum type.
    /// </summary>
    public static Assertions.Enums.IsNotDefinedAssertion<TEnum> IsNotDefined<TEnum>(
        this IAssertionSource<TEnum> source)
        where TEnum : struct, Enum
    {
        source.Context.ExpressionBuilder.Append(".IsNotDefined()");
        return new Assertions.Enums.IsNotDefinedAssertion<TEnum>(source.Context);
    }

    /// <summary>
    /// Asserts that two enum values have the same name.
    /// </summary>
    public static Assertions.Enums.HasSameNameAsAssertion<TEnum> HasSameNameAs<TEnum>(
        this IAssertionSource<TEnum> source,
        Enum otherEnumValue,
        [CallerArgumentExpression(nameof(otherEnumValue))] string? expression = null)
        where TEnum : struct, Enum
    {
        source.Context.ExpressionBuilder.Append($".HasSameNameAs({expression})");
        return new Assertions.Enums.HasSameNameAsAssertion<TEnum>(source.Context, otherEnumValue);
    }

    /// <summary>
    /// Asserts that two enum values have the same underlying value.
    /// </summary>
    public static Assertions.Enums.HasSameValueAsAssertion<TEnum> HasSameValueAs<TEnum>(
        this IAssertionSource<TEnum> source,
        Enum otherEnumValue,
        [CallerArgumentExpression(nameof(otherEnumValue))] string? expression = null)
        where TEnum : struct, Enum
    {
        source.Context.ExpressionBuilder.Append($".HasSameValueAs({expression})");
        return new Assertions.Enums.HasSameValueAsAssertion<TEnum>(source.Context, otherEnumValue);
    }

    /// <summary>
    /// Asserts that two enum values do NOT have the same name.
    /// </summary>
    public static Assertions.Enums.DoesNotHaveSameNameAsAssertion<TEnum> DoesNotHaveSameNameAs<TEnum>(
        this IAssertionSource<TEnum> source,
        Enum otherEnumValue,
        [CallerArgumentExpression(nameof(otherEnumValue))] string? expression = null)
        where TEnum : struct, Enum
    {
        source.Context.ExpressionBuilder.Append($".DoesNotHaveSameNameAs({expression})");
        return new Assertions.Enums.DoesNotHaveSameNameAsAssertion<TEnum>(source.Context, otherEnumValue);
    }

    /// <summary>
    /// Asserts that two enum values do NOT have the same underlying value.
    /// </summary>
    public static Assertions.Enums.DoesNotHaveSameValueAsAssertion<TEnum> DoesNotHaveSameValueAs<TEnum>(
        this IAssertionSource<TEnum> source,
        Enum otherEnumValue,
        [CallerArgumentExpression(nameof(otherEnumValue))] string? expression = null)
        where TEnum : struct, Enum
    {
        source.Context.ExpressionBuilder.Append($".DoesNotHaveSameValueAs({expression})");
        return new Assertions.Enums.DoesNotHaveSameValueAsAssertion<TEnum>(source.Context, otherEnumValue);
    }

}
