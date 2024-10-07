using TUnit.Assertions;
using TUnit.Assertions.Extensions;
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
    
    [Before(TestSession)]
    public static async Task BeforeTestSession()
    {
        if (IsMatchingTestFilter())
        {
            await PrintMessage("Before Test Session");

            await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
        }
    }
    
    [Before(Assembly)]
    public static async Task BeforeAssembly()
    {
        if (IsMatchingTestFilter())
        {
            await PrintMessage("Before Assembly");

            await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
        }
    }

    [Before(Class)]
    public static async Task BeforeClass()
    {
        if (IsMatchingTestFilter())
        {
            await PrintMessage("Before Class");

            await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
        }
    }

    [Test]
    public async Task Test()
    {
        await PrintMessage("Running Test");

        await PrintMessage(StaticProperty.ToString());
        await Assert.That(StaticProperty).IsNotNull();
        await Assert.That(StaticProperty.IsInitialized).IsTrue();
        await Assert.That(StaticProperty.Foo).IsEqualTo("Bar");
    }

    public class InnerModel : IAsyncInitializer, IAsyncDisposable
    {
        public async Task InitializeAsync()
        {
            await PrintMessage("Initializing Property");
            IsInitialized = true;
            Foo = "Bar";
        }

        public bool IsInitialized { get; private set; }
        public string? Foo { get; private set; }

        public async ValueTask DisposeAsync()
        {
            await PrintMessage("Disposing Property");
            
            if (IsMatchingTestFilter())
            {
                await File.WriteAllTextAsync("Property_IAsyncDisposable.txt", "true");
            }
        }
    }
    
    public record StaticInnerModel : IAsyncInitializer, IAsyncDisposable
    {
        public async Task InitializeAsync()
        {
            await PrintMessage("Initializing Static Property");
            IsInitialized = true;
            Foo = "Bar";
        }

        public bool IsInitialized { get; private set; }
        public string? Foo { get; private set; }

        public async ValueTask DisposeAsync()
        {
            await PrintMessage("Disposing Static Property");
            
            if (IsMatchingTestFilter())
            {
                await File.WriteAllTextAsync("StaticProperty_IAsyncDisposable.txt", "true");
            }
        }
    }

    public static string MethodData() => "2";

    private static async Task PrintMessage(string message)
    {
        if (GlobalContext.Current.TestFilter is "/*/*/PropertySetterTests/*")
        {
            Console.WriteLine(message);
            await File.AppendAllLinesAsync("PropertySetterTests_CapturedOutput.txt", [message]);
        }
        
        if (GlobalContext.Current.TestFilter is "/*/*/InheritedPropertySetterTests/*")
        {
            Console.WriteLine(message);
            await File.AppendAllLinesAsync("InheritedPropertySetterTests_CapturedOutput.txt", [message]);
        }
    }

    private static bool IsMatchingTestFilter()
    {
        return GlobalContext.Current.TestFilter is "/*/*/PropertySetterTests/*" or "/*/*/InheritedPropertySetterTests/*";
    }
}