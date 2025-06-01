namespace TUnit.TestProject.Attributes;

public class EngineTest(ExpectedResult expectedResult) : PropertyAttribute("EngineTest", expectedResult.ToString());

public enum ExpectedResult
{
    Pass,
    Failure,
}