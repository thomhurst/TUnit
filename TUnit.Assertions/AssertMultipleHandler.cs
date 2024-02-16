using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;

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
        AssertionsTracker.Current.Clear();
        
        _action();
        
        var assertions = AssertionsTracker.Current;

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
            throw new AssertionException(string.Join($"{Environment.NewLine}{Environment.NewLine}", failed.Select(x => x.Message?.Trim())));
        }
    }
}