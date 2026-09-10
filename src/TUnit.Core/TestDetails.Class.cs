using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test class information - implements <see cref="ITestClass"/> interface
/// </summary>
public partial class TestDetails
{
    // Explicit interface implementation for ITestClass
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    Type ITestClass.ClassType => ClassType;
    object ITestClass.ClassInstance => ClassInstance;
    object?[] ITestClass.TestClassArguments => TestClassArguments;
    IDictionary<string, object?> ITestClass.TestClassInjectedPropertyArguments => TestClassInjectedPropertyArguments;
    Type[]? ITestClass.TestClassParameterTypes => TestClassParameterTypes;
    Type[] ITestClass.ClassGenericArguments => ClassGenericArguments;
}
