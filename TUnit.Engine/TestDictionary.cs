using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine;

public class TestDictionary
{
    private static readonly Dictionary<string, Func<Task>> Tests = new();

    public static void AddTest(string testId, Func<Task> action)
    {
        Tests[testId] = action;
    }
    
    public static void AddTest(string testId, Action action)
    {
        Tests[testId] = () =>
        {
            action();
            return Task.CompletedTask;
        };
    }
}