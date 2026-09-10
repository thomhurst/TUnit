using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class CallEventReceiverTests : IFirstTestInAssemblyEventReceiver
{
    private static int _beforeAssemblyInvoked;
    private static int _firstTestInAssemblyInvoked;

    [Test]
    public async Task Test1()
    {
        var result = true;
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Test2()
    {
        var result = true;
        await Assert.That(result).IsTrue();
    }

    [Before(Assembly)]
    public static async Task Before_assembly()
    {
        Interlocked.Increment(ref _beforeAssemblyInvoked);

        Console.WriteLine($@"Before Assembly = {_beforeAssemblyInvoked}");

        await Assert.That(_beforeAssemblyInvoked).IsEqualTo(1);
    }

    public async ValueTask OnFirstTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        Interlocked.Increment(ref _firstTestInAssemblyInvoked);

        Console.WriteLine($@"OnFirstTestInAssembly = {_firstTestInAssemblyInvoked}");

        await Assert.That(_firstTestInAssemblyInvoked).IsEqualTo(1);
    }

    public int Order => 0;
}
