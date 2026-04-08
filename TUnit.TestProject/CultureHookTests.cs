using System.Globalization;
using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/5452 — CultureAttribute
/// applied at method, class, and assembly level must also affect lifecycle hooks, not just
/// the test body.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[Culture("de-AT")]
public class CultureHookTests_ClassLevel
{
    private static string? _beforeClassCulture;
    private static string? _afterClassCulture;

    [Before(Class)]
    public static Task BeforeClass()
    {
        _beforeClassCulture = CultureInfo.CurrentCulture.Name;
        return Task.CompletedTask;
    }

    [After(Class)]
    public static Task AfterClass()
    {
        _afterClassCulture = CultureInfo.CurrentCulture.Name;
        return Task.CompletedTask;
    }

    [Before(Test)]
    public async Task BeforeTest()
    {
        await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo("de-AT");
    }

    [After(Test)]
    public async Task AfterTest()
    {
        await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo("de-AT");
    }

    [Test]
    public async Task Test_Body_RunsInClassCulture()
    {
        await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo("de-AT");
    }

    [Test]
    public async Task Test_BeforeClassHook_RanInClassCulture()
    {
        // Before(Class) hook must have executed in the class-level [Culture] context.
        await Assert.That(_beforeClassCulture).IsEqualTo("de-AT");
    }
}

[EngineTest(ExpectedResult.Pass)]
[Culture("de-AT")]
public class CultureHookTests_MethodLevelOverride
{
    [Test, Culture("fr-FR")]
    public async Task MethodLevel_Overrides_ClassLevel()
    {
        await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo("fr-FR");
    }

    [Before(Test)]
    public async Task BeforeTest()
    {
        // Before(Test) shares the same CustomHookExecutor as the test body, so it runs
        // under the most-specific [Culture] resolved for each test — fr-FR when the
        // test method has its own override.
        await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo("fr-FR");
    }
}

[EngineTest(ExpectedResult.Pass)]
[Culture("de-AT")]
public class CultureHookTests_MethodLevelInheritsClass
{
    // No method-level [Culture] override — class-level de-AT applies to both the test
    // and its Before(Test) hook.
    [Test]
    public async Task Test_InheritsClassCulture()
    {
        await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo("de-AT");
    }

    [Before(Test)]
    public async Task BeforeTest()
    {
        await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo("de-AT");
    }
}
