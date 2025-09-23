using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders.Groups;

[UnconditionalSuppressMessage("Usage", "TUnitAssertions0002:Assert statements must be awaited")]
[UnconditionalSuppressMessage("Usage", "TUnitAssertions0008:ValueTasks should be awaited when used within Assert.That(...)")]
public static class AssertionGroup
{
    public static OrAssertionGroupInvoker<TActual, TAssertionBuilder> Or<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group1, AssertionGroup<TActual, TAssertionBuilder> group2)
        where TAssertionBuilder : AssertionBuilder
    {
        return new OrAssertionGroupInvoker<TActual, TAssertionBuilder>(group1, group2);
    }

    public static AndAssertionGroupInvoker<TActual, TAssertionBuilder> And<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group1, AssertionGroup<TActual, TAssertionBuilder> group2)
        where TAssertionBuilder : AssertionBuilder
    {
        return new AndAssertionGroupInvoker<TActual, TAssertionBuilder>(group1, group2);
    }

    public static UnknownAssertionGroupInvoker<TActual, TAssertionBuilder> Assert<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group)
        where TAssertionBuilder : AssertionBuilder
    {
        return new UnknownAssertionGroupInvoker<TActual, TAssertionBuilder>(group);
    }

    public static AssertionGroupBuilder<TActual, AssertionBuilder<TActual>> For<TActual>(TActual value)
    {
        return new AssertionGroupBuilder<TActual, AssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<TActual, AssertionBuilder<TActual>> ForSameValueAs<TActual>(AssertionGroup<TActual, AssertionBuilder<TActual>> otherGroup)
    {
        return new AssertionGroupBuilder<TActual, AssertionBuilder<TActual>>(otherGroup.AssertionBuilder);
    }

    public static AssertionGroupBuilder<TActual, AssertionBuilder<TActual>> For<TActual>(Func<TActual> value)
    {
        return new AssertionGroupBuilder<TActual, AssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<TActual, AssertionBuilder<TActual>> For<TActual>(Task<TActual> value)
    {
        return new AssertionGroupBuilder<TActual, AssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<TActual, AssertionBuilder<TActual>> For<TActual>(ValueTask<TActual> value)
    {
        return new AssertionGroupBuilder<TActual, AssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<TActual, AssertionBuilder<TActual>> For<TActual>(Func<Task<TActual>> value)
    {
        return new AssertionGroupBuilder<TActual, AssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<object?, DelegateAssertionBuilder> For(Action value)
    {
        return new AssertionGroupBuilder<object?, DelegateAssertionBuilder>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<object?, DelegateAssertionBuilder> ForSameValueAs(AssertionGroup<object?, DelegateAssertionBuilder> otherGroup)
    {
        return new AssertionGroupBuilder<object?, DelegateAssertionBuilder>(otherGroup.AssertionBuilder);
    }

    public static AssertionGroupBuilder<object?, DelegateAssertionBuilder> For(Task value)
    {
        return new AssertionGroupBuilder<object?, DelegateAssertionBuilder>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<object?, DelegateAssertionBuilder> For(ValueTask value)
    {
        return new AssertionGroupBuilder<object?, DelegateAssertionBuilder>(TUnit.Assertions.Assert.That(value));
    }

    public static AssertionGroupBuilder<object?, DelegateAssertionBuilder> For(Func<Task> value)
    {
        return new AssertionGroupBuilder<object?, DelegateAssertionBuilder>(TUnit.Assertions.Assert.That(value));
    }
}

public abstract class AssertionGroup<TActual, TAssertionBuilder> where TAssertionBuilder : AssertionBuilder
{
    internal readonly TAssertionBuilder AssertionBuilder;

    internal AssertionGroup(TAssertionBuilder assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
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
