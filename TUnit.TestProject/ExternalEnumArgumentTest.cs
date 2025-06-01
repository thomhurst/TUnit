using Polly.CircuitBreaker;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ExternalEnumArgumentTest
{
    [Test]
    [Arguments(CircuitState.Closed)]
    public void MyTest(CircuitState value)
    {
    }
}