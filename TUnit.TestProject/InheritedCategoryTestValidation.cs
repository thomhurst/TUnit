using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class InheritedCategoryTestValidation : Library.BaseTests
{
    [Test]
    [Category("TestCategory")]
    public async Task TestInheritedMultipleCategoriesMethod()
    {
        // This test verifies that the class inherits the BaseCategoriesOnClass category from the base class
        // and has its own TestCategory
        await Assert.That(TestContext.Current!.TestDetails.Categories).Contains("BaseCategoriesOnClass");
        await Assert.That(TestContext.Current!.TestDetails.Categories).Contains("TestCategory");
    }
}
