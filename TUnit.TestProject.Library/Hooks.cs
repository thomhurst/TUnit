namespace TUnit.TestProject.Library;

public class Hooks
{
    [Before(Test)]
    public void BeforeTests(TestContext testContext)
    {
        testContext.StateBag.Items["BeforeHit"] = true;
    }

    [After(Test)]
    public void AfterTests(TestContext testContext)
    {
        testContext.StateBag.Items["AfterHit"] = true;
    }
}
