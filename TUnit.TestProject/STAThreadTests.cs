using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[WindowsOnly]
[UnconditionalSuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class STAThreadTests
{
    [Before(Test)]
    [HookExecutor<STAThreadExecutor>]
    public void BeforeTest()
    {
    }

    [After(Test)]
    [HookExecutor<STAThreadExecutor>]
    public void AfterTest()
    {
    }

    [Test, STAThreadExecutor]
    public async Task With_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test]
    public async Task Without_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.MTA);
    }
}