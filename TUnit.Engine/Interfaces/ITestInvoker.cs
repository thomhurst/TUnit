namespace TUnit.Engine.Interfaces;

/// <summary>
/// Interface for invoking test methods without reflection
/// </summary>
public interface ITestInvoker
{
    Task InvokeTestAsync(object instance, Func<object, object?[], Task> testInvoker, object?[] arguments);
}
