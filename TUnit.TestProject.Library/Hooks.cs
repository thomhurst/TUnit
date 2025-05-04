namespace TUnit.TestProject.Library;

public class Hooks
{
    [Before(Test)]
    public void BeforeTests(TestContext testContext)
    {
        testContext.ObjectBag.Add("BeforeHit", true);
    }
    
    [After(Test)]
    public void AfterTests(TestContext testContext)
    {
        testContext.ObjectBag.Add("AfterHit", true);
    }
}