using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Tests for nested class filtering support.
/// This validates that tests in nested classes can be filtered using the OuterClass+NestedClass syntax.
/// See: https://github.com/thomhurst/TUnit/issues/XXXX
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class NestedTestClassTests
{
    [Test]
    public void Outer()
    {
        // This test should pass when filtered by OuterClass name
    }

    public class NestedClass
    {
        [Test]
        public void Inner()
        {
            // This test should pass when filtered by OuterClass+NestedClass name
        }
    }
}
