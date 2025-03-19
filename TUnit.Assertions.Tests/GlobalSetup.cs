namespace TUnit.Assertions.Tests;

public class GlobalSetup
{
    [Before(TestSession)]
    public static void DisableTruncation()
    {
        Environment.SetEnvironmentVariable("TUNIT_ASSERTIONS_DISABLE_TRUNCATION", "true");
    }
}