using Polly.CircuitBreaker;

namespace TUnit.TestProject;

public class ExternalEnumArgumentTest
{
    [Test]
    [Arguments(CircuitState.Closed)]
    public void MyTest(CircuitState value)
    {
    }
}