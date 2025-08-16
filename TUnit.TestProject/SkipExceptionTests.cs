using TUnit.Core.Exceptions;

namespace TUnit.TestProject;

public class SkipExceptionTests
{
    [Test]
    public void TestA()
    {
        throw new SkipTestException("i should be skipped");
    }
}