namespace TUnit.Engine;

/// <summary>
/// Interface for invoking test methods without reflection
/// </summary>
public interface ITestInvoker
{
    Task InvokeTestAsync(object instance, Func<object, object?[], Task> testInvoker, object?[] arguments);
    Task InvokeTestAsync(string testMethodKey, object instance, object?[] arguments);
}
