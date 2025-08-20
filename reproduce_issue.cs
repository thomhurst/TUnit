using TUnit.Core;

namespace TUnit.TestProject;

public class MyTests
{
    [Test]
    [Arguments(4)]
    [Arguments(0.1)]
    [Arguments(1.1)]
    [Arguments(1e-1)]
    public void ParameterTest(double number)
    {
        
    }
}