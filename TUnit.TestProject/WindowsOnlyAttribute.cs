namespace TUnit.TestProject;

public class WindowsOnlyAttribute : SkipAttribute
{
    public WindowsOnlyAttribute() : base("This test is only applicable on Windows platforms.")
    {
    }

    public override Task<bool> ShouldSkip(BeforeTestContext context)
    {
        return Task.FromResult(!OperatingSystem.IsWindows());
    }
}