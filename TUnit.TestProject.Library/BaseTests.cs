namespace TUnit.TestProject.Library;

public abstract class BaseTests
{
    [Test]
    [Category("BaseCategory")]
    public void BaseTest()
    {
    }
    
    [Test]
    [Category("AnotherBaseCategory")]
    [Category("MultipleCategories")]
    public void BaseTestWithMultipleCategories()
    {
    }
}
