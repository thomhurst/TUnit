using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.Bugs._1914;

public class Tests
{
    private static readonly AsyncLocal<string> _0BeforeTestDiscoveryLocal = new();
    private static readonly AsyncLocal<string> _1BeforeTestSessionLocal = new();
    private static readonly AsyncLocal<string> _2BeforeAssemblyLocal = new();
    private static readonly AsyncLocal<string> _3BeforeClassLocal = new();
    private static readonly AsyncLocal<string> _4BeforeTestLocal = new();

    [BeforeEvery(TestDiscovery)]
    public static void BeforeTestDiscovery(BeforeTestDiscoveryContext context)
        => _0BeforeTestDiscoveryLocal.Value = "BeforeTestDiscovery";
    
    [BeforeEvery(TestSession)]
    public static void BeforeTestSession(TestSessionContext context)
        => _1BeforeTestSessionLocal.Value = "BeforeTestSession";

    [BeforeEvery(Assembly)]
    public static void BeforeAssembly(AssemblyHookContext context)
        => _2BeforeAssemblyLocal.Value = "BeforeAssembly";

    [BeforeEvery(Class)]
    public static void BeforeClass(ClassHookContext context)
        => _3BeforeClassLocal.Value = "BeforeClass";

    [BeforeEvery(Test)]
    public static void BeforeTest(TestContext context)
        => _4BeforeTestLocal.Value = "BeforeTest";


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
        // await Assert.That(_3BeforeClassLocal.Value).IsEqualTo("BeforeClass");
        await Assert.That(_4BeforeTestLocal.Value).IsEqualTo("BeforeTest");
    }
}