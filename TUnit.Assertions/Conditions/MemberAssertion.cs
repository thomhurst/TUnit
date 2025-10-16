using System.Linq.Expressions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Result of a member assertion that allows returning to the parent object context.
/// Enables chaining multiple member assertions on the same parent object.
/// </summary>
public class MemberAssertionResult<TObject>
{
    private readonly AssertionContext<TObject> _parentContext;
    private readonly Assertion<object?> _memberAssertion;

    internal MemberAssertionResult(AssertionContext<TObject> parentContext, Assertion<object?> memberAssertion)
    {
        _parentContext = parentContext ?? throw new ArgumentNullException(nameof(parentContext));
        _memberAssertion = memberAssertion ?? throw new ArgumentNullException(nameof(memberAssertion));
    }

    /// <summary>
    /// Returns an And continuation that operates on the parent object's context,
    /// allowing chaining of multiple member assertions on the same parent object.
    /// </summary>
    public AndContinuation<TObject> And
    {
        get
        {
            // Create a wrapper that executes the member assertion then returns to parent context
            var wrapper = new MemberExecutionWrapper<TObject>(_parentContext, _memberAssertion);
            return new AndContinuation<TObject>(_parentContext, wrapper);
        }
    }

    /// <summary>
    /// Returns an Or continuation that operates on the parent object's context.
    /// </summary>
    public OrContinuation<TObject> Or
    {
        get
        {
            var wrapper = new MemberExecutionWrapper<TObject>(_parentContext, _memberAssertion);
            return new OrContinuation<TObject>(_parentContext, wrapper);
        }
    }

    /// <summary>
    /// Enables await syntax by executing the member assertion and returning the parent object.
    /// </summary>
    public System.Runtime.CompilerServices.TaskAwaiter<TObject?> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    private async Task<TObject?> ExecuteAsync()
    {
        // Execute the member assertion
        await _memberAssertion.AssertAsync();

        // Return the parent object value
        var (parentValue, _) = await _parentContext.GetAsync();
        return parentValue;
    }
}

/// <summary>
/// Internal wrapper that executes a member assertion and returns the parent object value.
/// This enables chaining multiple member assertions on the same parent object.
/// </summary>
internal class MemberExecutionWrapper<TObject> : Assertion<TObject>
{
    private readonly Assertion<object?> _memberAssertion;

    public MemberExecutionWrapper(AssertionContext<TObject> parentContext, Assertion<object?> memberAssertion)
        : base(parentContext)
    {
        _memberAssertion = memberAssertion ?? throw new ArgumentNullException(nameof(memberAssertion));
    }

    public override async Task<TObject?> AssertAsync()
    {
        // Execute the member assertion
        await _memberAssertion.AssertAsync();

        // Return the parent object value for further chaining
        var (parentValue, _) = await Context.GetAsync();
        return parentValue;
    }

    protected override string GetExpectation() => _memberAssertion.InternalGetExpectation();
}

/// <summary>
/// Type-erased wrapper for assertions to allow storing different types in MemberAssertionResult.
/// </summary>
internal class TypeErasedAssertion<T> : Assertion<object?>
{
    private readonly Assertion<T> _innerAssertion;

    public TypeErasedAssertion(Assertion<T> innerAssertion)
        : base(innerAssertion.InternalContext.Map<object?>(val => val))
    {
        _innerAssertion = innerAssertion ?? throw new ArgumentNullException(nameof(innerAssertion));
    }

    public override async Task<object?> AssertAsync()
    {
        await _innerAssertion.AssertAsync();
        return null;
    }

    protected override string GetExpectation() => _innerAssertion.InternalGetExpectation();
}

/// <summary>
/// Simple adapter to wrap an AssertionContext as an IAssertionSource.
/// Used to pass member context to the assertion lambda.
/// </summary>
internal class AssertionSourceAdapter<T> : IAssertionSource<T>
{
    public AssertionContext<T> Context { get; }

    public AssertionSourceAdapter(AssertionContext<T> context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
}

/// <summary>
/// Combines a pending assertion with a member assertion using AND logic.
/// Both assertions must pass for the overall assertion to succeed.
/// </summary>
internal class CombinedAndAssertion<TObject> : Assertion<object?>
{
    private readonly Assertion<TObject> _pendingAssertion;
    private readonly Assertion<object?> _memberAssertion;

    public CombinedAndAssertion(AssertionContext<TObject> parentContext, Assertion<TObject> pendingAssertion, Assertion<object?> memberAssertion)
        : base(parentContext.Map<object?>(val => val))
    {
        _pendingAssertion = pendingAssertion ?? throw new ArgumentNullException(nameof(pendingAssertion));
        _memberAssertion = memberAssertion ?? throw new ArgumentNullException(nameof(memberAssertion));
    }

    public override async Task<object?> AssertAsync()
    {
        // Execute the pending assertion first (which should throw if it fails)
        await _pendingAssertion.AssertAsync();

        // Then execute the member assertion
        await _memberAssertion.AssertAsync();

        return null;
    }

    protected override string GetExpectation()
    {
        return $"{_pendingAssertion.InternalGetExpectation()} AND {_memberAssertion.InternalGetExpectation()}";
    }
}

/// <summary>
/// Combines a pending assertion with a member assertion using OR logic.
/// At least one assertion must pass for the overall assertion to succeed.
/// </summary>
internal class CombinedOrAssertion<TObject> : Assertion<object?>
{
    private readonly Assertion<TObject> _pendingAssertion;
    private readonly Assertion<object?> _memberAssertion;

    public CombinedOrAssertion(AssertionContext<TObject> parentContext, Assertion<TObject> pendingAssertion, Assertion<object?> memberAssertion)
        : base(parentContext.Map<object?>(val => val))
    {
        _pendingAssertion = pendingAssertion ?? throw new ArgumentNullException(nameof(pendingAssertion));
        _memberAssertion = memberAssertion ?? throw new ArgumentNullException(nameof(memberAssertion));
    }

    public override async Task<object?> AssertAsync()
    {
        Exception? firstException = null;

        try
        {
            await _pendingAssertion.AssertAsync();
            // First assertion passed, no need to check the second
            return null;
        }
        catch (Exception ex)
        {
            firstException = ex;
        }

        try
        {
            await _memberAssertion.AssertAsync();
            // Second assertion passed, overall succeeds
            return null;
        }
        catch (Exception secondException)
        {
            // Both failed, throw combined error
            throw new AssertionException($"Both conditions failed:\n1) {firstException.Message}\n2) {secondException.Message}");
        }
    }

    protected override string GetExpectation()
    {
        return $"{_pendingAssertion.InternalGetExpectation()} OR {_memberAssertion.InternalGetExpectation()}";
    }
}
