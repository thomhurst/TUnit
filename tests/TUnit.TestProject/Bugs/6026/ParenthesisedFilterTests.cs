using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6026;

/// <summary>
/// Regression fixture for GitHub issue #6026. The MetadataFilterMatcher pre-filter
/// must let TreeNodeFilter grouping expressions like (MyTest1) and (MyTest1|MyTest2)
/// through to MTP's authoritative path matcher instead of treating them as literal
/// method names.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ParenthesisedFilterTests
{
    [Test]
    public async Task MyTest1()
    {
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task MyTest2()
    {
        await Assert.That(true).IsTrue();
    }
}
