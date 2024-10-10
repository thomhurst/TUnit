using TUnit.Assertions;
using TUnit.Assertions.Extensions;

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
        await Assert.That(GetDictionary()).Contains("ClassCategory");
        
        await Assert.That(GetDictionary()).Contains("ClassCategory2");
        
        await Assert.That(GetDictionary()).Contains("MethodCategory");
        
        await Assert.That(GetDictionary()).Contains("MethodCategory2");
    }

    private static IEnumerable<string> GetDictionary()
    {
        return TestContext.Current?.TestDetails.Categories ?? [];
    }

    public class ClassCategoryAttribute() : CategoryAttribute("ClassCategory2");
    
    public class MethodCategoryAttribute() : CategoryAttribute("MethodCategory2");
}