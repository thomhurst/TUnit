using System.Diagnostics;
using TUnit.Assertions;

namespace TUnit.TestProject;

public class FailFastTest
{
    [Test]
    public async Task Test1_ShouldFail()
    {
        await Assert.That(1).IsEqualTo(2);
    }
    
    [Test]
    public void Test2_ShouldNotRunIfFailFastEnabled()
    {
        Console.WriteLine("Test2 executed - fail-fast not working!");
    }
    
    [Test]
    public void Test3_ShouldNotRunIfFailFastEnabled()
    {
        Console.WriteLine("Test3 executed - fail-fast not working!");
    }
    
    [Test]
    public void TestWithDebugAssert()
    {
        Trace.Assert(false, "Testing Trace.Assert with our custom listener");
    }
}