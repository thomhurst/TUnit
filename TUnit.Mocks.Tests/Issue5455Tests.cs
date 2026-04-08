using Azure;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/5455
// Azure.Response.IsError is `public virtual bool IsError { get; internal set; }` — the internal
// setter is invisible to external assemblies, so the generated override must not emit it.
public class Issue5455Tests
{
    [Test]
    public void Mocking_Response_With_Internal_Setter_Compiles()
    {
        var mock = Mock.Of<Response>(MockBehavior.Strict);
        _ = mock.Object;
    }
}
