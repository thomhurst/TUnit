using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

[Category("ClassCategory")]
[ClassCategory]
public class CategoryTests
{
    [Test]
    [Category("MethodCategory")]
    [MethodCategory]
    public async Task Test()
    {
        await Assert.That(GetDictionary()).Does.Contain("ClassCategory");
        
        await Assert.That(GetDictionary()).Does.Contain("ClassCategory2");
        
        await Assert.That(GetDictionary()).Does.Contain("MethodCategory");
        
        await Assert.That(GetDictionary()).Does.Contain("MethodCategory2");
    }

    private static IEnumerable<string> GetDictionary()
    {
        return TestContext.Current?.TestDetails.Categories ?? [];
    }

    private class ClassCategoryAttribute : CategoryAttribute
    {
        public ClassCategoryAttribute() : base("ClassCategory2")
        {
        }
    }
    
    private class MethodCategoryAttribute : CategoryAttribute
    {
        public MethodCategoryAttribute() : base("MethodCategory2")
        {
        }
    }
}