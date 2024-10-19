using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders.Groups;

[SuppressMessage("Usage", "TUnitAssertions0002:Assert statements must be awaited")]
public static class AssertionGroup
{
    public static OrAssertionGroupInvoker<TActual, TAssertionBuilder> Or<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group1, AssertionGroup<TActual, TAssertionBuilder> group2) 
        where TAssertionBuilder : AssertionBuilder<TActual>
    {
        return new OrAssertionGroupInvoker<TActual, TAssertionBuilder>(group1, group2);
    }
    
    public static AndAssertionGroupInvoker<TActual, TAssertionBuilder> And<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group1, AssertionGroup<TActual, TAssertionBuilder> group2) 
        where TAssertionBuilder : AssertionBuilder<TActual>
    {
        return new AndAssertionGroupInvoker<TActual, TAssertionBuilder>(group1, group2);
    }
    
    public static UnknownAssertionGroupInvoker<TActual, TAssertionBuilder> Assert<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group)
        where TAssertionBuilder : AssertionBuilder<TActual>
    {
        return new UnknownAssertionGroupInvoker<TActual, TAssertionBuilder>(group);
    }
    
    public static AssertionGroupBuilder<TActual, ValueAssertionBuilder<TActual>> For<TActual>(TActual value)
    {
        return new AssertionGroupBuilder<TActual, ValueAssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }
    
    public static AssertionGroupBuilder<TActual, ValueAssertionBuilder<TActual>> ForSameValueAs<TActual>(AssertionGroup<TActual, ValueAssertionBuilder<TActual>> otherGroup)
    {
        return new AssertionGroupBuilder<TActual, ValueAssertionBuilder<TActual>>(otherGroup.AssertionBuilder);
    }
    
    public static AssertionGroupBuilder<TActual, ValueDelegateAssertionBuilder<TActual>> For<TActual>(Func<TActual> value)
    {
        return new AssertionGroupBuilder<TActual, ValueDelegateAssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }
    
    public static AssertionGroupBuilder<TActual, ValueDelegateAssertionBuilder<TActual>> ForSameValueAs<TActual>(AssertionGroup<TActual, ValueDelegateAssertionBuilder<TActual>> otherGroup)
    {
        return new AssertionGroupBuilder<TActual, ValueDelegateAssertionBuilder<TActual>>(otherGroup.AssertionBuilder);
    }
    
    public static AssertionGroupBuilder<TActual, AsyncValueDelegateAssertionBuilder<TActual>> For<TActual>(Task<TActual> value)
    {
        return new AssertionGroupBuilder<TActual, AsyncValueDelegateAssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }
    
    public static AssertionGroupBuilder<TActual, AsyncValueDelegateAssertionBuilder<TActual>> ForSameValueAs<TActual>(AssertionGroup<TActual, AsyncValueDelegateAssertionBuilder<TActual>> otherGroup)
    {
        return new AssertionGroupBuilder<TActual, AsyncValueDelegateAssertionBuilder<TActual>>(otherGroup.AssertionBuilder);
    }
    
    public static AssertionGroupBuilder<TActual, AsyncValueDelegateAssertionBuilder<TActual>> For<TActual>(ValueTask<TActual> value)
    {
        return new AssertionGroupBuilder<TActual, AsyncValueDelegateAssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }
    
    public static AssertionGroupBuilder<TActual, AsyncValueDelegateAssertionBuilder<TActual>> For<TActual>(Func<Task<TActual>> value)
    {
        return new AssertionGroupBuilder<TActual, AsyncValueDelegateAssertionBuilder<TActual>>(TUnit.Assertions.Assert.That(value));
    }
    
    public static AssertionGroupBuilder<object?, DelegateAssertionBuilder> For(Action value)
    {
        return new AssertionGroupBuilder<object?, DelegateAssertionBuilder>(TUnit.Assertions.Assert.That(value));
    }
    
    public static AssertionGroupBuilder<object?, DelegateAssertionBuilder> ForSameValueAs(AssertionGroup<object?, DelegateAssertionBuilder> otherGroup)
    {
        return new AssertionGroupBuilder<object?, DelegateAssertionBuilder>(otherGroup.AssertionBuilder);
    }
    
    public static AssertionGroupBuilder<object?, AsyncDelegateAssertionBuilder> For(Task value)
    {
        return new AssertionGroupBuilder<object?, AsyncDelegateAssertionBuilder>(TUnit.Assertions.Assert.That(value));
    }
    
    public static AssertionGroupBuilder<object?, AsyncDelegateAssertionBuilder> ForSameValueAs(AssertionGroup<object?, AsyncDelegateAssertionBuilder> otherGroup)
    {
        return new AssertionGroupBuilder<object?, AsyncDelegateAssertionBuilder>(otherGroup.AssertionBuilder);
    }
    
    public static AssertionGroupBuilder<object?, AsyncDelegateAssertionBuilder> For(ValueTask value)
    {
        return new AssertionGroupBuilder<object?, AsyncDelegateAssertionBuilder>(TUnit.Assertions.Assert.That(value));
    }
    
    public static AssertionGroupBuilder<object?, AsyncDelegateAssertionBuilder> For(Func<Task> value)
    {
        return new AssertionGroupBuilder<object?, AsyncDelegateAssertionBuilder>(TUnit.Assertions.Assert.That(value));
    }
}

public abstract class AssertionGroup<TActual, TAssertionBuilder> where TAssertionBuilder : AssertionBuilder<TActual>
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