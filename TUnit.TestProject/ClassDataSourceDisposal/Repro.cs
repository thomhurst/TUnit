namespace TUnit.TestProject.ClassDataSourceDisposal;

public class ReproFixtureBase : IDisposable
{
    public ReproFixtureBase()
    {
        Console.WriteLine($@"Constructing {GetType().Name}");
    }

    public void Dispose()
    {
        Console.WriteLine($@"Disposing {GetType().Name}");
    }
}

public class SharedTypeNoneFixture : ReproFixtureBase;
[ClassDataSource<SharedTypeNoneFixture>(Shared = SharedType.None)]
public class SharedTypeNoneTests(SharedTypeNoneFixture fixture)
{
    [Test]
    public void Test()
    {
        Console.WriteLine($@"Test using {fixture.GetType().Name}");
    }
}

public class SharedTypePerClassFixture : ReproFixtureBase;
[ClassDataSource<SharedTypePerClassFixture>(Shared = SharedType.PerClass)]
public class SharedTypePerClassTests(SharedTypePerClassFixture fixture)
{
    [Test]
    public void Test()
    {
        Console.WriteLine($@"Test using {fixture.GetType().Name}");
    }
}

public class SharedTypeKeyedFixture : ReproFixtureBase;
[ClassDataSource<SharedTypeKeyedFixture>(Shared = SharedType.Keyed, Key = "Key")]
public class SharedTypeKeyedTests(SharedTypeKeyedFixture fixture)
{
    [Test]
    public void Test()
    {
        Console.WriteLine($@"Test using {fixture.GetType().Name}");
    }
}

public class SharedTypePerAssemblyFixture : ReproFixtureBase;
[ClassDataSource<SharedTypePerAssemblyFixture>(Shared = SharedType.PerAssembly)]
public class SharedTypePerAssemblyTests(SharedTypePerAssemblyFixture fixture)
{
    [Test]
    public void Test()
    {
        Console.WriteLine($@"Test using {fixture.GetType().Name}");
    }
}

public class SharedTypePerPerTestSessionFixture : ReproFixtureBase;
[ClassDataSource<SharedTypePerPerTestSessionFixture>(Shared = SharedType.PerTestSession)]
public class SharedTypePerTestSessionTests(SharedTypePerPerTestSessionFixture fixture)
{
    [Test]
    public void Test()
    {
        Console.WriteLine($@"Test using {fixture.GetType().Name}");
    }
}