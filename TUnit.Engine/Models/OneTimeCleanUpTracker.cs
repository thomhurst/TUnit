using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models;

public class OneTimeCleanUpTracker
{
    private readonly Action<TestNode, Task> _onLastTestForClassProcessed;
    private readonly Dictionary<string, List<TestNode>> _innerDictionary;

    public OneTimeCleanUpTracker(IEnumerable<TestNode> tests, Action<TestNode, Task> onLastTestForClassProcessed)
    {
        _onLastTestForClassProcessed = onLastTestForClassProcessed;
        
        _innerDictionary = tests
            .GroupBy(GetClassName)
            .ToDictionary(x => x.Key, x => x.ToList());
    }

    private readonly object _removeLock = new();

    public void Remove(TestNode testNode, Task executingTask)
    {
        var className = GetClassName(testNode);

        if (!_innerDictionary.TryGetValue(className, out var list))
        {
            return;
        }

        executingTask.ContinueWith(_ =>
        {
            lock (_removeLock)
            {
                list.Remove(testNode);

                if (list.Count == 0)
                {
                    _onLastTestForClassProcessed(testNode, executingTask);
                }
            }
        });
    }

    private string GetClassName(TestNode testNode)
    {
        return testNode.GetPropertyValue(TUnitTestProperties.TestClass, string.Empty);
    }
}