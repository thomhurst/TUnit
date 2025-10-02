using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders.Groups;

[UnconditionalSuppressMessage("Usage", "TUnitAssertions0002:Assert statements must be awaited",
    Justification = "This is a factory class for creating assertion groups. The factory methods return builders that create awaitable assertion groups; the factory methods themselves are not awaitable.")]
[UnconditionalSuppressMessage("Usage", "TUnitAssertions0008:ValueTasks should be awaited when used within Assert.That(...)",
    Justification = "The Assert.That() calls within this factory class return assertion builders that will be composed into groups and awaited by the consumer. The factory methods themselves do not need to await these builders.")]
public static class AssertionGroup
{
    public static OrAssertionGroupInvoker<TActual, TAssertionBuilder> Or<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group1, AssertionGroup<TActual, TAssertionBuilder> group2)
        where TAssertionBuilder : AssertionCore
    {
        return new OrAssertionGroupInvoker<TActual, TAssertionBuilder>(group1, group2);
    }

    public static AndAssertionGroupInvoker<TActual, TAssertionBuilder> And<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group1, AssertionGroup<TActual, TAssertionBuilder> group2)
        where TAssertionBuilder : AssertionCore
    {
        return new AndAssertionGroupInvoker<TActual, TAssertionBuilder>(group1, group2);
    }

    public static UnknownAssertionGroupInvoker<TActual, TAssertionBuilder> Assert<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group)
        where TAssertionBuilder : AssertionCore
    {
        return new UnknownAssertionGroupInvoker<TActual, TAssertionBuilder>(group);
    }

    public static AssertionGroupBuilder<TActual, ValueAssertion<TActual>> For<TActual>(TActual value)
    {
        return new AssertionGroupBuilder<TActual, ValueAssertion<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<TActual, ValueAssertion<TActual>> ForSameValueAs<TActual>(AssertionGroup<TActual, ValueAssertion<TActual>> otherGroup)
    {
        return new AssertionGroupBuilder<TActual, ValueAssertion<TActual>>(otherGroup.AssertionCore);
    }

    public static AssertionGroupBuilder<TActual, ValueDelegateAssertion<TActual>> For<TActual>(Func<TActual> value)
    {
        return new AssertionGroupBuilder<TActual, ValueDelegateAssertion<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<TActual, ValueDelegateAssertion<TActual>> ForSameValueAs<TActual>(AssertionGroup<TActual, ValueDelegateAssertion<TActual>> otherGroup)
    {
        return new AssertionGroupBuilder<TActual, ValueDelegateAssertion<TActual>>(otherGroup.AssertionCore);
    }

    public static AssertionGroupBuilder<TActual, AsyncValueDelegateAssertion<TActual>> For<TActual>(Task<TActual> value)
    {
        return new AssertionGroupBuilder<TActual, AsyncValueDelegateAssertion<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<TActual, AsyncValueDelegateAssertion<TActual>> ForSameValueAs<TActual>(AssertionGroup<TActual, AsyncValueDelegateAssertion<TActual>> otherGroup)
    {
        return new AssertionGroupBuilder<TActual, AsyncValueDelegateAssertion<TActual>>(otherGroup.AssertionCore);
    }

    public static AssertionGroupBuilder<TActual, AsyncValueDelegateAssertion<TActual>> For<TActual>(ValueTask<TActual> value)
    {
        return new AssertionGroupBuilder<TActual, AsyncValueDelegateAssertion<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<TActual, AsyncValueDelegateAssertion<TActual>> For<TActual>(Func<Task<TActual>> value)
    {
        return new AssertionGroupBuilder<TActual, AsyncValueDelegateAssertion<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<object?, DelegateAssertion> For(Action value)
    {
        return new AssertionGroupBuilder<object?, DelegateAssertion>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<object?, DelegateAssertion> ForSameValueAs(AssertionGroup<object?, DelegateAssertion> otherGroup)
    {
        return new AssertionGroupBuilder<object?, DelegateAssertion>(otherGroup.AssertionCore);
    }

    public static AssertionGroupBuilder<object?, AsyncDelegateAssertion> For(Task value)
    {
        return new AssertionGroupBuilder<object?, AsyncDelegateAssertion>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<object?, AsyncDelegateAssertion> ForSameValueAs(AssertionGroup<object?, AsyncDelegateAssertion> otherGroup)
    {
        return new AssertionGroupBuilder<object?, AsyncDelegateAssertion>(otherGroup.AssertionCore);
    }

    public static AssertionGroupBuilder<object?, AsyncDelegateAssertion> For(ValueTask value)
    {
        return new AssertionGroupBuilder<object?, AsyncDelegateAssertion>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<object?, AsyncDelegateAssertion> For(Func<Task> value)
    {
        return new AssertionGroupBuilder<object?, AsyncDelegateAssertion>(TUnit.Assertions.Assert.That(value));
    }
}

public abstract class AssertionGroup<TActual, TAssertionBuilder> where TAssertionBuilder : AssertionCore
{
    internal readonly TAssertionBuilder AssertionCore;

    internal AssertionGroup(TAssertionBuilder assertionBuilder)
    {
        AssertionCore = assertionBuilder;
    }

    public abstract TaskAwaiter<TActual?> GetAwaiter();
}

public class OrAssertionException : AggregateException
{
    public OrAssertionException(IEnumerable<Exception> exceptions) : base(exceptions)
    {
    }

    public override string Message =>
        $"{string.Join($"{Environment.NewLine}or ", InnerExceptions.Select(x => x.Message).Distinct())}";
}
