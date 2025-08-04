using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;
using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[RunOn(OS.Windows)]
[UnconditionalSuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class HookExecutorVariousHooksTests
{
    private static ApartmentState _beforeAssemblyApartmentState;
    private static ApartmentState _beforeClassApartmentState;
    private static ApartmentState _beforeTestApartmentState;
    private static ApartmentState _testApartmentState;
    
    [Before(HookType.Assembly)]
    [HookExecutor<STAThreadExecutor>]
    public static void BeforeAssembly()
    {
        _beforeAssemblyApartmentState = Thread.CurrentThread.GetApartmentState();
    }
    
    [Before(HookType.Class)]
    [HookExecutor<STAThreadExecutor>]
    public static void BeforeClass()
    {
        _beforeClassApartmentState = Thread.CurrentThread.GetApartmentState();
    }
    
    [Before(HookType.Test)]
    [HookExecutor<STAThreadExecutor>]
    public static void BeforeTest()
    {
        _beforeTestApartmentState = Thread.CurrentThread.GetApartmentState();
    }
    
    [Test]
    public async Task All_Hooks_Should_Respect_STAThreadExecutor()
    {
        _testApartmentState = Thread.CurrentThread.GetApartmentState();
        
        // All hooks should have run in STA thread due to HookExecutor<STAThreadExecutor>
        await Assert.That(_beforeAssemblyApartmentState).IsEqualTo(ApartmentState.STA);
        await Assert.That(_beforeClassApartmentState).IsEqualTo(ApartmentState.STA);
        await Assert.That(_beforeTestApartmentState).IsEqualTo(ApartmentState.STA);
        
        // The test itself runs in MTA by default
        await Assert.That(_testApartmentState).IsEqualTo(ApartmentState.MTA);
    }
}