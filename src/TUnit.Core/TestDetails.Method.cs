using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test method information - implements <see cref="ITestMethod"/> interface
/// </summary>
public partial class TestDetails
{
    // Explicit interface implementation for ITestMethod
    MethodMetadata ITestMethod.MethodMetadata => MethodMetadata;
    Type ITestMethod.ReturnType => ReturnType;
    object?[] ITestMethod.TestMethodArguments => TestMethodArguments;
    Type[] ITestMethod.MethodGenericArguments => MethodGenericArguments;
}
