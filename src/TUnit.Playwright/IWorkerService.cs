namespace TUnit.Playwright;

public interface IWorkerService
{
    public Task ResetAsync();
    public Task DisposeAsync();
}
