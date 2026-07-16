using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._2738;

public class DatabaseFactory : IAsyncInitializer
{
    public Task InitializeAsync()
    {
        // setup database
        return Task.Delay(10);
    }

    public static int DoStuff() => 12;
}

public class Tests
{
    [ClassDataSource<DatabaseFactory>]
    public required DatabaseFactory __ { get; init; }

    [Test]
    public async Task Test()
    {
        await Assert.That(__).IsNotNull();
    }
}
