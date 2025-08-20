using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class FloatingPointDebugTests
{
    [Test]
    [Arguments(4)]
    [Arguments(0.1)]
    [Arguments(1.1)]
    [Arguments(1e-1)]
    public void ParameterTest(double number)
    {
        // This should work without throwing "Expected exactly 1 argument, but got 2"
    }
}