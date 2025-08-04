using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;
using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[RunOn(OS.Windows)]
[UnconditionalSuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class HookExecutorTestSessionTests
{
    private static ApartmentState _beforeTestSessionApartmentState;
    private static ApartmentState _testApartmentState;
    
    [Before(HookType.TestSession)]
    [HookExecutor<STAThreadExecutor>]
    public static void BeforeTestSession()
    {
        _beforeTestSessionApartmentState = Thread.CurrentThread.GetApartmentState();
    }
    
    [Test]
    public async Task TestSession_Hook_Should_Respect_STAThreadExecutor()
    {
        _testApartmentState = Thread.CurrentThread.GetApartmentState();
        
        // The hook should have run in STA thread due to HookExecutor<STAThreadExecutor>
        await Assert.That(_beforeTestSessionApartmentState).IsEqualTo(ApartmentState.STA);
        
        // The test itself runs in MTA by default
        await Assert.That(_testApartmentState).IsEqualTo(ApartmentState.MTA);
    }
}