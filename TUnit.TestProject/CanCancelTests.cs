namespace TUnit.TestProject;

public class CanCancelTests
{
    [Test, Explicit]
    public async Task CanCancel()
    {
        await Task.Delay(TimeSpan.FromMinutes(5));
    }
}
