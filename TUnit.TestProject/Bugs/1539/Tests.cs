using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1539;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [AttributeWithPositionalArgs]
    [AttributeWithPositionalArgs(11)]
    [AttributeWithPositionalArgs(two: "two")]
    [AttributeWithPositionalArgs(three: false)]
    public void Test()
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class AttributeWithPositionalArgs : Attribute
    {
        public AttributeWithPositionalArgs(int one = 1, string two = "2", bool three = true)
        {

        }
    }
}
