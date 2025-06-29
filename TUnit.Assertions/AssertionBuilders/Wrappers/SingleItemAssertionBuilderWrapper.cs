using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class SingleItemAssertionBuilderWrapper<TActual, TInner> : InvokableValueAssertionBuilder<TActual> where TActual : IEnumerable<TInner>
{
    internal SingleItemAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public new TaskAwaiter<TInner?> GetAwaiter()
    {
        var task = ProcessAssertionsAsync(d =>
        {
            if (d.Result is IEnumerable<TInner> enumerable)
            {
                return Task.FromResult(enumerable.SingleOrDefault());
            }

            return Task.FromResult<TInner?>(default)!;
        });

        return task.GetAwaiter()!;
    }
}
