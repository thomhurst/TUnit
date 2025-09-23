using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NotNullAssertionBuilderWrapper<TActual> : AssertionBuilder<TActual> where TActual : class
{
    internal NotNullAssertionBuilderWrapper(AssertionBuilder<TActual?> assertionBuilder) : base(default(TActual)!, assertionBuilder.ActualExpression)
    {
        // Copy the assertion chain from the original builder
        foreach (var assertion in assertionBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
        
        // Store the actual value task
        _actualValueTask = assertionBuilder.GetActualValueTask();
    }
    
    private readonly ValueTask<TActual?> _actualValueTask;

    private new async ValueTask<TActual?> GetActualValueTask()
    {
        return await _actualValueTask;
    }

    public new TaskAwaiter<TActual> GetAwaiter()
    {
        return Process().GetAwaiter();
    }

    private async Task<TActual> Process()
    {
        var assertionData = await GetAssertionData();
        await ProcessAssertionsAsync(assertionData);

        var tActual = assertionData.Result as TActual;
        return tActual!;
    }
}

public class NotNullStructAssertionBuilderWrapper<TActual> : AssertionBuilder<TActual> where TActual : struct
{
    internal NotNullStructAssertionBuilderWrapper(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.Actual, assertionBuilder.ActualExpression)
    {
        // Copy the assertion chain from the original builder
        foreach (var assertion in assertionBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }

    public new TaskAwaiter<TActual> GetAwaiter()
    {
        return Process().GetAwaiter();
    }

    private async Task<TActual> Process()
    {
        var assertionData = await GetAssertionData();
        await ProcessAssertionsAsync(assertionData);

        var tActual = assertionData.Result is TActual actual ? actual : default(TActual);
        return tActual;
    }
}