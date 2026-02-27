using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

namespace TestProject;

public class GlobalHooks
{
    [Before(TestSession)]
    public static Task BeforeTestSession(TestSessionContext context)
    {
        // Runs once before all tests - e.g. start a test container, seed a database
        return Task.CompletedTask;
    }

    [After(TestSession)]
    public static Task AfterTestSession(TestSessionContext context)
    {
        // Runs once after all tests - e.g. stop containers, clean up resources
        return Task.CompletedTask;
    }

    [After(Class)]
    public static Task AfterClass(ClassHookContext context)
    {
        // Runs after each test class
        return Task.CompletedTask;
    }

    [After(Test)]
    public Task AfterTest(TestContext context)
    {
        // Runs after each individual test
        return Task.CompletedTask;
    }
}
