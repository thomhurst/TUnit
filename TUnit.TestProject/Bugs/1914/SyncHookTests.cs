using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.Bugs._1914;

public class SyncHookTests
{
    private static readonly AsyncLocal<string> _0BeforeTestDiscoveryLocal = new();
    private static readonly AsyncLocal<string> _0BeforeTestDiscoveryLocal2 = new();
    
    private static readonly AsyncLocal<string> _1BeforeTestSessionLocal = new();
    private static readonly AsyncLocal<string> _1BeforeTestSessionLocal2 = new();
    
    private static readonly AsyncLocal<string> _2BeforeAssemblyLocal = new();
    private static readonly AsyncLocal<string> _2BeforeAssemblyLocal2 = new();
    
    private static readonly AsyncLocal<string> _3BeforeClassLocal = new();
    private static readonly AsyncLocal<string> _3BeforeClassLocal2 = new();
    
    private static readonly AsyncLocal<string> _4BeforeTestLocal = new();
    private static readonly AsyncLocal<string> _4BeforeTestLocal2 = new();

    [BeforeEvery(TestDiscovery)]
    public static void BeforeTestDiscovery(BeforeTestDiscoveryContext context)
    {
        _0BeforeTestDiscoveryLocal.Value = "BeforeTestDiscovery";
        context.FlowAsyncLocalValues();
    }
    
    [BeforeEvery(TestDiscovery)]
    public static void BeforeTestDiscovery2(BeforeTestDiscoveryContext context)
    {
        _0BeforeTestDiscoveryLocal2.Value = "BeforeTestDiscovery2";
        context.FlowAsyncLocalValues();
    }

    [BeforeEvery(TestSession)]
    public static void BeforeTestSession(TestSessionContext context)
    {
        _1BeforeTestSessionLocal.Value = "BeforeTestSession";
        context.FlowAsyncLocalValues();
    }
    
    [BeforeEvery(TestSession)]
    public static void BeforeTestSession2(TestSessionContext context)
    {
        _1BeforeTestSessionLocal2.Value = "BeforeTestSession2";
        context.FlowAsyncLocalValues();
    }

    [BeforeEvery(Assembly)]
    public static void BeforeAssembly(AssemblyHookContext context)
    {
        _2BeforeAssemblyLocal.Value = "BeforeAssembly";
        context.FlowAsyncLocalValues();
    }
    
    [BeforeEvery(Assembly)]
    public static void BeforeAssembly2(AssemblyHookContext context)
    {
        _2BeforeAssemblyLocal2.Value = "BeforeAssembly2";
        context.FlowAsyncLocalValues();
    }

    [BeforeEvery(Class)]
    public static void BeforeClass(ClassHookContext context)
    {
        _3BeforeClassLocal.Value = "BeforeClass";
        context.FlowAsyncLocalValues();
    }
    
    [BeforeEvery(Class)]
    public static void BeforeClass2(ClassHookContext context)
    {
        _3BeforeClassLocal2.Value = "BeforeClass2";
        context.FlowAsyncLocalValues();
    }

    [BeforeEvery(Test)]
    public static void BeforeTest(TestContext context)
    {
        _4BeforeTestLocal.Value = "BeforeTest";
        context.FlowAsyncLocalValues();
    }
    
    [BeforeEvery(Test)]
    public static void BeforeTest2(TestContext context)
    {
        _4BeforeTestLocal2.Value = "BeforeTest2";
        context.FlowAsyncLocalValues();
    }


    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(8)]
    public async Task TestAsyncLocal(int i)
    {
        using var _ = Assert.Multiple();

        await Assert.That(_0BeforeTestDiscoveryLocal.Value).IsEqualTo("BeforeTestDiscovery");
        await Assert.That(_1BeforeTestSessionLocal.Value).IsEqualTo("BeforeTestSession");
        await Assert.That(_2BeforeAssemblyLocal.Value).IsEqualTo("BeforeAssembly");
        await Assert.That(_3BeforeClassLocal.Value).IsEqualTo("BeforeClass");
        await Assert.That(_4BeforeTestLocal.Value).IsEqualTo("BeforeTest");
        
        await Assert.That(_0BeforeTestDiscoveryLocal2.Value).IsEqualTo("BeforeTestDiscovery2");
        await Assert.That(_1BeforeTestSessionLocal2.Value).IsEqualTo("BeforeTestSession2");
        await Assert.That(_2BeforeAssemblyLocal2.Value).IsEqualTo("BeforeAssembly2");
        await Assert.That(_3BeforeClassLocal2.Value).IsEqualTo("BeforeClass2");
        await Assert.That(_4BeforeTestLocal2.Value).IsEqualTo("BeforeTest2");
    }
}