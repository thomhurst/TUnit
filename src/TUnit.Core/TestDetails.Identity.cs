using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test identification - implements <see cref="ITestIdentity"/> interface
/// </summary>
public partial class TestDetails
{
    // Explicit interface implementation for ITestIdentity
    string ITestIdentity.TestId => TestId;
    string ITestIdentity.TestName => TestName;
    string ITestIdentity.MethodName => MethodName;
}
