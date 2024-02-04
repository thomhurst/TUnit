using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;
using TUnit.Core;

namespace TUnit.Assertions;

public class AssertMultipleHandler
{
    private readonly Action _action;

    public AssertMultipleHandler(Action action)
    {
        _action = action;
    }

    public TaskAwaiter GetAwaiter() => AssertAsync().GetAwaiter();

    public async Task AssertAsync()
    {
        TestContext.Current.ClearObjects<BaseAssertCondition>();
        
        _action();
        
        var assertions = TestContext.Current.GetObjects<BaseAssertCondition>();

        var failed = new List<BaseAssertCondition>();
        foreach (var baseAssertCondition in assertions)
        {
            if (!await baseAssertCondition.AssertAsync())
            {
                failed.Add(baseAssertCondition);
            }
        }
        
        if (failed.Any())
        {
            throw new AssertionException(string.Join($"{Environment.NewLine}   ", failed.Select(x => x.Message)));
        }
    }
}