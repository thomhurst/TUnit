using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class PropertySetterTests
{
    [Arguments("1")]
    public required string Property1 { get; init; }
        
    [MethodDataSource(nameof(MethodData))]
    public required string Property2 { get; init; }
        
    [ClassDataSource<InnerModel>]
    public required InnerModel Property3 { get; init; }
    
    [ClassDataSource<InnerModel>(Shared = SharedType.Globally)]
    public required InnerModel Property4 { get; init; }
    
    [ClassDataSource<InnerModel>(Shared = SharedType.ForClass)]
    public required InnerModel Property5 { get; init; }
    
    [ClassDataSource<InnerModel>(Shared = SharedType.Keyed, Key = "Key")]
    public required InnerModel Property6 { get; init; }
        
    [DataSourceGeneratorTests.AutoFixtureGenerator<string>]
    public required string Property7 { get; init; }

    [ClassDataSource<InnerModel>(Shared = SharedType.Globally)]
    public static InnerModel StaticProperty { get; set; } = null!;

    [Before(TestSession)]
    public static async Task BeforeTestSession()
    {
        Console.WriteLine("Before Test Session");

        await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
    }
    
    [Before(Assembly)]
    public static async Task BeforeAssembly()
    {
        Console.WriteLine("Before Assembly");

        await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
    }

    [Before(Class)]
    public static async Task BeforeClass()
    {
        Console.WriteLine("Before Class");

        await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
    }

    [Test]
    public async Task Test()
    {
        Console.WriteLine("Running Test");

        Console.WriteLine(Property7);
        await Assert.That(StaticProperty).IsNotNull();
        await Assert.That(StaticProperty.IsInitialized).IsTrue();
        await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
    }

    public class InnerModel : IAsyncInitializer, IAsyncDisposable
    {
        public Task InitializeAsync()
        {
            Console.WriteLine("Initializing Static Property");
            IsInitialized = true;
            Foo = "Bar";
            return Task.CompletedTask;
        }

        public bool IsInitialized { get; private set; }
        public string? Foo { get; private set; }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("Disposing Static Property");
            await File.WriteAllTextAsync("StaticProperty_IAsyncDisposable.txt", "true");
        }
    }

    public static string MethodData() => "2";
}