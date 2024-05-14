namespace TUnit.Core.Interfaces;

public interface IApplicableTestAttribute
{
    Task Apply(TestContext testContext);
}