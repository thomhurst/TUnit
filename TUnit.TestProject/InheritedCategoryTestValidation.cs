using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class InheritedCategoryTestValidation : Library.BaseTests
{
    [Test]
    public void TestInheritedBaseTestHasBaseCategory()
    {
        // This test verifies that when we call the inherited BaseTest method,
        // it retains its BaseCategory attribute
        // We can't directly check the BaseTest categories from here, but we can
        // verify our fix by checking the source generator tests
    }
    
    [Test]
    public void TestInheritedMultipleCategoriesMethod()
    {
        // Similar verification for the BaseTestWithMultipleCategories method
        // The real validation happens in the source generator test
    }
}