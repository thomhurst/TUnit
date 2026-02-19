using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Collections;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a set is a subset of another collection.
/// </summary>
public class SetIsSubsetOfAssertion<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;
    private readonly IEnumerable<TItem> _other;

    public SetIsSubsetOfAssertion(
        AssertionContext<TSet> context,
        Func<TSet, ISetAdapter<TItem>> adapterFactory,
        IEnumerable<TItem> other)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _other = other;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("set was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsSubsetOf(adapter, _other));
    }

    protected override string GetExpectation() => "to be a subset of the specified collection";
}

/// <summary>
/// Asserts that a set is a superset of another collection.
/// </summary>
public class SetIsSupersetOfAssertion<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;
    private readonly IEnumerable<TItem> _other;

    public SetIsSupersetOfAssertion(
        AssertionContext<TSet> context,
        Func<TSet, ISetAdapter<TItem>> adapterFactory,
        IEnumerable<TItem> other)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _other = other;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("set was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsSupersetOf(adapter, _other));
    }

    protected override string GetExpectation() => "to be a superset of the specified collection";
}

/// <summary>
/// Asserts that a set is a proper subset of another collection.
/// </summary>
public class SetIsProperSubsetOfAssertion<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;
    private readonly IEnumerable<TItem> _other;

    public SetIsProperSubsetOfAssertion(
        AssertionContext<TSet> context,
        Func<TSet, ISetAdapter<TItem>> adapterFactory,
        IEnumerable<TItem> other)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _other = other;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("set was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsProperSubsetOf(adapter, _other));
    }

    protected override string GetExpectation() => "to be a proper subset of the specified collection";
}

/// <summary>
/// Asserts that a set is a proper superset of another collection.
/// </summary>
public class SetIsProperSupersetOfAssertion<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;
    private readonly IEnumerable<TItem> _other;

    public SetIsProperSupersetOfAssertion(
        AssertionContext<TSet> context,
        Func<TSet, ISetAdapter<TItem>> adapterFactory,
        IEnumerable<TItem> other)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _other = other;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("set was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsProperSupersetOf(adapter, _other));
    }

    protected override string GetExpectation() => "to be a proper superset of the specified collection";
}

/// <summary>
/// Asserts that a set overlaps with another collection.
/// </summary>
public class SetOverlapsAssertion<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;
    private readonly IEnumerable<TItem> _other;

    public SetOverlapsAssertion(
        AssertionContext<TSet> context,
        Func<TSet, ISetAdapter<TItem>> adapterFactory,
        IEnumerable<TItem> other)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _other = other;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("set was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckOverlaps(adapter, _other));
    }

    protected override string GetExpectation() => "to overlap with the specified collection";
}

/// <summary>
/// Asserts that a set does not overlap with another collection.
/// </summary>
public class SetDoesNotOverlapAssertion<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;
    private readonly IEnumerable<TItem> _other;

    public SetDoesNotOverlapAssertion(
        AssertionContext<TSet> context,
        Func<TSet, ISetAdapter<TItem>> adapterFactory,
        IEnumerable<TItem> other)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _other = other;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("set was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckDoesNotOverlap(adapter, _other));
    }

    protected override string GetExpectation() => "to not overlap with the specified collection";
}

/// <summary>
/// Asserts that a set equals another collection (same elements).
/// </summary>
public class SetEqualsAssertion<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;
    private readonly IEnumerable<TItem> _other;

    public SetEqualsAssertion(
        AssertionContext<TSet> context,
        Func<TSet, ISetAdapter<TItem>> adapterFactory,
        IEnumerable<TItem> other)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _other = other;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("set was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckSetEquals(adapter, _other));
    }

    protected override string GetExpectation() => "to be equal to the specified collection";
}
