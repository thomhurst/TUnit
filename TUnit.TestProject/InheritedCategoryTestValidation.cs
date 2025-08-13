using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class InheritedCategoryTestValidation : Library.BaseTests
{
    [Test]
    public async Task TestInheritedBaseTestHasBaseCategory()
    {
        // This test verifies that when we call the inherited BaseTest method,
        // it retains its BaseCategory attribute
        await Assert.That(TestContext.Current!.TestDetails.Categories).Contains("BaseCategory");
    }

    [Test]
    public async Task TestInheritedMultipleCategoriesMethod()
    {
        // This test verifies that inherited methods with multiple categories retain all of them
        await Assert.That(TestContext.Current!.TestDetails.Categories).Contains("AnotherBaseCategory");
        await Assert.That(TestContext.Current!.TestDetails.Categories).Contains("MultipleCategories");
    }
}
