namespace TUnit.TestProject.Library;

public class Hooks
{
    [Before(Test)]
    public void BeforeTests(TestContext testContext)
    {
        testContext.ObjectBag["BeforeHit"] = true;
    }

    [After(Test)]
    public void AfterTests(TestContext testContext)
    {
        testContext.ObjectBag["AfterHit"] = true;
    }
}
