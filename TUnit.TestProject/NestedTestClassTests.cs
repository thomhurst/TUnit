using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class NestedTestClassTests
{
    [Test]
    public void Outer()
    {
    }

    public class NestedClass
    {
        [Test]
        public void Inner()
        {
        }
    }
}
