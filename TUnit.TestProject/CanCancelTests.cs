namespace TUnit.TestProject;

[Category("Cancellation")]
public class CanCancelTests
{
    [Test, Explicit]
    public async Task CanCancel()
    {
        await Task.Delay(TimeSpan.FromMinutes(5));
    }
}
