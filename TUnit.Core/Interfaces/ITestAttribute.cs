namespace TUnit.Core.Interfaces;

public interface ITestAttribute
{
    Task ApplyToTest(TestContext testContext);
}