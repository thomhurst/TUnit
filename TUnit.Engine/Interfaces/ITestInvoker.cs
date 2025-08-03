using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Interface for invoking test methods without reflection
/// </summary>
public interface ITestInvoker
{
    Task InvokeTestAsync(object instance, Func<object, object?[], Task> testInvoker, object?[] arguments);
    
    /// <summary>
    /// Invokes a test with typed arguments to avoid boxing
    /// </summary>
    Task InvokeTypedTestAsync(object instance, Func<object, object?[], Task> testInvoker, TypedTestArguments typedArguments);
}
