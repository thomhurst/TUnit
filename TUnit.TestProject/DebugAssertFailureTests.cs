using System.Diagnostics;

namespace TUnit.TestProject;

public class DebugAssertFailureTests
{
    [Test]
    public void Test()
    {
        var @true = "true";
        Trace.Assert(@true is "false", "Some message");
    }
}
