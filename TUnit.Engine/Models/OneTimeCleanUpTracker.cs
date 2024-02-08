using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TUnit.Engine.Models;

public class OneTimeCleanUpTracker
{
    private readonly Action<TestCase, Task> _onLastTestForClassProcessed;
    private readonly Dictionary<string, List<TestCase>> _innerDictionary;

    public OneTimeCleanUpTracker(IEnumerable<TestCase> tests, Action<TestCase, Task> onLastTestForClassProcessed)
    {
        _onLastTestForClassProcessed = onLastTestForClassProcessed;
        
        _innerDictionary = tests
            .GroupBy(GetClassName)
            .ToDictionary(x => x.Key, x => x.ToList());
    }

    private readonly object _removeLock = new();
    
    public void Remove(TestCase testCase, Task executingTask)
    {
        lock (_removeLock)
        {
            var className = GetClassName(testCase);

            if (!_innerDictionary.TryGetValue(className, out var list))
            {
                throw new ArgumentNullException($"No tests for class {className} were found");
            }

            list.Remove(testCase);

            if (list.Count == 0)
            {
                _innerDictionary.Remove(className);
                
                executingTask.ContinueWith(_ =>
                    _onLastTestForClassProcessed(testCase, executingTask)
                );
            }
        }
    }

    private string GetClassName(TestCase testCase)
    {
        return testCase.GetPropertyValue(TUnitTestProperties.TestClass, string.Empty);
    }
}