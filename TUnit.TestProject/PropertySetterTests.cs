using System.Text;
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

    [ClassDataSource<StaticInnerModel>(Shared = SharedType.Globally)]
    public static StaticInnerModel StaticProperty { get; set; } = null!;
    
    private static readonly StringBuilder _stringBuilder = new();
    private static TextWriter? _defaultOut;
    
    [Before(TestDiscovery)]
    public static void RedirectConsole()
    {
        _defaultOut = Console.Out;
        Console.SetOut(new StringWriter(_stringBuilder));
    }
    
    [After(TestSession)]
    public static async Task RevertConsole()
    {
        Console.SetOut(_defaultOut!);
        await File.WriteAllTextAsync("PropertySetterTests_CapturedOutput.txt", _stringBuilder.ToString());
    }
    
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

        Console.WriteLine(StaticProperty);
        await Assert.That(StaticProperty).IsNotNull();
        await Assert.That(StaticProperty.IsInitialized).IsTrue();
        await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
    }

    public class InnerModel : IAsyncInitializer, IAsyncDisposable
    {
        public Task InitializeAsync()
        {
            Console.WriteLine("Initializing Property");
            IsInitialized = true;
            Foo = "Bar";
            return Task.CompletedTask;
        }

        public bool IsInitialized { get; private set; }
        public string? Foo { get; private set; }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("Disposing Property");
            await File.WriteAllTextAsync("Property_IAsyncDisposable.txt", "true");
        }
    }
    
    public record StaticInnerModel : IAsyncInitializer, IAsyncDisposable
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