using System;
using System.Reflection;
using System.Threading.Tasks;

namespace TUnit.Engine;

/// <summary>
/// Interface for invoking test methods
/// </summary>
public interface ITestInvoker
{
    Task InvokeTestMethod(object instance, MethodInfo method, object?[] arguments);
    Task InvokeTestAsync(object instance, MethodInfo method, object?[] arguments);
    Task InvokeTestAsync(object instance, Func<object, object?[], Task> testInvoker, object?[] arguments);
}