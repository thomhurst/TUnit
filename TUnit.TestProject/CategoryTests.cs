using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
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

    [Test]
    [Category("A")]
    public void A()
    {
    }

    [Test]
    [Category("B")]
    public void B()
    {
    }

    [Test]
    [Category("A"), Category("B")]
    public void C()
    {
    }

    private static IEnumerable<string> GetDictionary()
    {
        return TestContext.Current?.TestDetails.Categories ?? [];
    }

    public class ClassCategoryAttribute() : CategoryAttribute("ClassCategory2");

    public class MethodCategoryAttribute() : CategoryAttribute("MethodCategory2");
}
