namespace TUnit.Core.Interfaces;

public interface ITestEndEvents
{
    Task OnTestEnd(TestContext testContext);
    Task IfLastTestInClass(ClassHookContext context, TestContext testContext);
    Task IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext);
    Task IfLastTestInTestSession(TestSessionContext current, TestContext testContext);
}