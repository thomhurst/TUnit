using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class FloatingPointDebugTests
{
    [Test]
    [Arguments(1.1)]
    public void TestDoubleArgument(double value)
    {
        // This should receive 1.1, not be split into (1, 1)
    }
    
    [Test]
    [Arguments(1.1f)]
    public void TestFloatArgument(float value)
    {
        // This should receive 1.1f, not be split 
    }
    
    [Test]
    [Arguments(1e-1)]
    public void TestScientificNotation(double value)
    {
        // This should receive 0.1, not be split
    }
}