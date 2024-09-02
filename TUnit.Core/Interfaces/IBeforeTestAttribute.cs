namespace TUnit.Core.Interfaces;

public interface IBeforeTestAttribute
{
    Task OnBeforeTest(BeforeTestContext testContext);
}