namespace TUnit.TestProject;

[InheritsTests]
public class InheritedTestsFromDifferentProjectTests : Library.BaseTests
{
    [Test]
    public void Test()
    {
    }

    [Test]
    [MethodDataSource<TestData>(nameof(TestData.Foo))]
    public void ClickLinkTest(string value)
    {
    }
}