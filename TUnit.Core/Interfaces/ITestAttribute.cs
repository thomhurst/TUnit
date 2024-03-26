namespace TUnit.Core.Interfaces;

public interface ITestAttribute
{
    Task Apply(TestContext testContext);
}