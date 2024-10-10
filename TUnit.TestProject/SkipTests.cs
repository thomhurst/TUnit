namespace TUnit.TestProject;

public class SkipTests : SkipDummyHooks
{
    [Test]
    [Skip("Just because.")]
    public void SkippedTest()
    {
    }
}

public class SkipDummyHooks
{
    public string? _dummy;
    
    [Before(Test)]
    public void SetUp()
    {
    }
    
    [After(Test)]
    public void TearDown()
    {
    }
}