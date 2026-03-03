using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

namespace TUnit;

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

}
