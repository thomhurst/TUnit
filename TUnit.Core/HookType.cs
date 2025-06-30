namespace TUnit.Core;

public enum HookType
{
    /// <summary>
    /// Hook to run before/after every test in the same class
    /// </summary>
    Test,

    /// <summary>
    /// Hook to run once per class before/after all tests in the class
    /// </summary>
    Class,

    /// <summary>
    /// Hook to run once per assembly before/after all tests in the assembly
    /// </summary>
    Assembly,

    /// <summary>
    /// Hook to run once per test session on test session set up/tear down
    /// </summary>
    TestSession,

    /// <summary>
    /// Hook to run before/after test discovery
    /// </summary>
    TestDiscovery,
}
