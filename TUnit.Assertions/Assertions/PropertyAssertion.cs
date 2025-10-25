using System.Linq.Expressions;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Assertions;

/// <summary>
/// Result type for property assertions that allows fluent chaining.
/// After asserting on a property, returns to the parent object context.
/// </summary>
/// <typeparam name="TObject">The parent object type</typeparam>
/// <typeparam name="TProperty">The property type</typeparam>
public class PropertyAssertion<TObject, TProperty>
{
    private readonly AssertionContext<TObject> _parentContext;
    private readonly AssertionContext<TProperty> _propertyContext;
    private readonly string _propertyName;

    internal PropertyAssertion(
        AssertionContext<TObject> parentContext,
        AssertionContext<TProperty> propertyContext,
        string propertyName)
    {
        _parentContext = parentContext;
        _propertyContext = propertyContext;
        _propertyName = propertyName;
    }

    /// <summary>
    /// Asserts that the property value is equal to the expected value.
    /// Returns back to the parent object context for further assertions.
    /// Example: await Assert.That(obj).HasProperty(x => x.Name).IsEqualTo("expected").And...
    /// </summary>
    public PropertyAssertionResult<TObject> IsEqualTo(TProperty expected)
    {
        _parentContext.ExpressionBuilder.Append($".IsEqualTo({expected})");

        // Create assertion on the property and wrap it with type erasure
        var assertion = new Conditions.EqualsAssertion<TProperty>(_propertyContext, expected);
        var erasedAssertion = new Conditions.TypeErasedAssertion<TProperty>(assertion);

        return new PropertyAssertionResult<TObject>(_parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts that the property value is not equal to the expected value.
    /// Returns back to the parent object context for further assertions.
    /// </summary>
    public PropertyAssertionResult<TObject> IsNotEqualTo(TProperty expected)
    {
        _parentContext.ExpressionBuilder.Append($".IsNotEqualTo({expected})");

        var assertion = new Conditions.NotEqualsAssertion<TProperty>(_propertyContext, expected);
        var erasedAssertion = new Conditions.TypeErasedAssertion<TProperty>(assertion);

        return new PropertyAssertionResult<TObject>(_parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts that the property value is null.
    /// Returns back to the parent object context for further assertions.
    /// </summary>
    public PropertyAssertionResult<TObject> IsNull()
    {
        _parentContext.ExpressionBuilder.Append(".IsNull()");

        var assertion = new Conditions.NullAssertion<TProperty>(_propertyContext);
        var erasedAssertion = new Conditions.TypeErasedAssertion<TProperty>(assertion);

        return new PropertyAssertionResult<TObject>(_parentContext, erasedAssertion);
    }

    /// <summary>
    /// Asserts that the property value is not null.
    /// Returns back to the parent object context for further assertions.
    /// </summary>
    public PropertyAssertionResult<TObject> IsNotNull()
    {
        _parentContext.ExpressionBuilder.Append(".IsNotNull()");

        var assertion = new Conditions.NotNullAssertion<TProperty>(_propertyContext);
        var erasedAssertion = new Conditions.TypeErasedAssertion<TProperty>(assertion);

        return new PropertyAssertionResult<TObject>(_parentContext, erasedAssertion);
    }
}

/// <summary>
/// Result of a property assertion that can be chained back to the parent object.
/// Allows continuing assertions on the parent object after asserting on a property.
/// </summary>
/// <typeparam name="TObject">The parent object type</typeparam>
public class PropertyAssertionResult<TObject> : IAssertionSource<TObject>
{
    public AssertionContext<TObject> Context { get; }
    private readonly Assertion<object?> _propertyAssertion;

    internal PropertyAssertionResult(AssertionContext<TObject> parentContext, Assertion<object?> propertyAssertion)
    {
        Context = parentContext;
        _propertyAssertion = propertyAssertion;

        // Queue the property assertion to be executed
        var previousPreWork = Context.PendingPreWork;
        Context.PendingPreWork = async () =>
        {
            if (previousPreWork != null)
            {
                await previousPreWork();
            }
            await _propertyAssertion.AssertAsync();
        };
    }

    /// <summary>
    /// Asserts that the parent object is of the specified type and returns an assertion on the casted value.
    /// Example: await Assert.That(obj).HasProperty(x => x.Name).IsEqualTo("test").IsTypeOf<DerivedClass>();
    /// </summary>
    public TypeOfAssertion<TObject, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TObject, TExpected>(Context);
    }

    /// <summary>
    /// Enables await syntax by executing the property assertion and returning the parent object.
    /// </summary>
    public System.Runtime.CompilerServices.TaskAwaiter<TObject?> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    private async Task<TObject?> ExecuteAsync()
    {
        // Execute the property assertion
        await _propertyAssertion.AssertAsync();

        // Return the parent object value
        var (parentValue, _) = await Context.GetAsync();
        return parentValue;
    }
}
