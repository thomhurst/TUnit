using TUnit.Core;

namespace TUnit.TestProject;

public class ApplicableAttributeTests
{
    [Test, CustomSkip, SomethingElse]
    public void Test()
    {
    }
}