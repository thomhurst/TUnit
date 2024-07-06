namespace TUnit.Core.Interfaces;

public interface IAfterTestAttribute
{
    Task OnAfterTest(TestContext testContext);
}