#pragma warning disable TUnit0042

using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

[NotInParallel(nameof(PropertySetterTests))]
public class PropertySetterTests
{
    [Arguments("1")]
    public required string Property1 { get; init; }

    [MethodDataSource(nameof(MethodData))]
    public required string Property2 { get; init; }

    [ClassDataSource<InnerModel>]
    public required InnerModel Property3 { get; init; }

    [ClassDataSource<InnerModel>(Shared = SharedType.PerTestSession)]
    public required InnerModel Property4 { get; init; }

    [ClassDataSource<InnerModel>(Shared = SharedType.PerClass)]
    public required InnerModel Property5 { get; init; }

    [ClassDataSource<InnerModel>(Shared = SharedType.Keyed, Key = "Key")]
    public required InnerModel Property6 { get; init; }

    [DataSourceGeneratorTests.AutoFixtureGenerator<string>]
    public required string Property7 { get; init; }

    [ClassDataSource<StaticInnerModel>(Shared = SharedType.PerTestSession)]
    public static StaticInnerModel StaticProperty { get; set; } = null!;

    [Before(TestSession)]
    public static async Task BeforeTestSession()
    {
        if (IsMatchingTestFilter())
        {
            Console.WriteLine(@"Before Test Session");

            await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
        }
    }

    [Before(Assembly)]
    public static async Task BeforeAssembly()
    {
        if (IsMatchingTestFilter())
        {
            Console.WriteLine(@"Before Assembly");

            await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
        }
    }

    [Before(Class)]
    public static async Task BeforeClass()
    {
        if (IsMatchingTestFilter())
        {
            Console.WriteLine(@"Before Class");

            await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
        }
    }

    [Test]
    public async Task Test()
    {
        Console.WriteLine(@"Running Test");

        Console.WriteLine(StaticProperty.ToString());
        await Assert.That(StaticProperty).IsNotNull();
        await Assert.That(StaticProperty.IsInitialized).IsTrue();
        await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
        
        await Assert.That(Property3.IsInitialized).IsTrue();
        await Assert.That(Property4.IsInitialized).IsTrue();
        await Assert.That(Property5.IsInitialized).IsTrue();
        await Assert.That(Property6.IsInitialized).IsTrue();
    }

    public class InnerModel : IAsyncInitializer, IAsyncDisposable
    {
        public Task InitializeAsync()
        {
            Console.WriteLine(@"Initializing Property");
            IsInitialized = true;
            Foo = "Bar";
            return Task.CompletedTask;
        }

        public bool IsInitialized { get; private set; }
        public string? Foo { get; private set; }

        public ValueTask DisposeAsync()
        {
            Console.WriteLine(@"Disposing Property");

            return default;
        }
    }

    public record StaticInnerModel : IAsyncInitializer, IAsyncDisposable
    {
        public Task InitializeAsync()
        {
            Console.WriteLine(@"Initializing Static Property");
            IsInitialized = true;
            Foo = "Bar";
            return Task.CompletedTask;
        }

        public bool IsInitialized { get; private set; }
        public string? Foo { get; private set; }

        public ValueTask DisposeAsync()
        {
            Console.WriteLine(@"Disposing Static Property");
            return default;
        }
    }

    public static string MethodData() => "2";

    private static bool IsMatchingTestFilter()
    {
        return GlobalContext.Current.TestFilter is "/*/*/PropertySetterTests/*" or "/*/*/InheritedPropertySetterTests/*";
    }
}