using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._3992;

public class DummyContainer : IAsyncInitializer, IAsyncDisposable
{
    public Task InitializeAsync()
    {
        NumberOfInits++;
        Ints = [1, 2, 3, 4, 5, 6];
        return Task.CompletedTask;
    }

    public int[] Ints { get; private set; } = null!;

    public static int NumberOfInits { get; private set; }

    public ValueTask DisposeAsync()
    {
        return default;
    }
}
