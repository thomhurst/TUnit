using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4432;

#region Fixtures

/// <summary>
/// Simple fixture that implements IAsyncInitializer.
/// </summary>
public class SimpleFixture : IAsyncInitializer
{
    public int? Value { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(10);
        Value = 123;
    }
}

/// <summary>
/// Fixture with a nested object that also implements IAsyncInitializer.
/// </summary>
public class OuterFixture : IAsyncInitializer
{
    public int? OuterValue { get; private set; }

    [ClassDataSource<InnerFixture>]
    public required InnerFixture Inner { get; init; }

    public async Task InitializeAsync()
    {
        await Task.Delay(10);
        OuterValue = 100;
    }
}

public class InnerFixture : IAsyncInitializer
{
    public int? InnerValue { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(10);
        InnerValue = 200;
    }
}

/// <summary>
/// Fixture with deeply nested IAsyncInitializer objects.
/// </summary>
public class Level1Fixture : IAsyncInitializer
{
    public int? Level1Value { get; private set; }

    [ClassDataSource<Level2Fixture>]
    public required Level2Fixture Level2 { get; init; }

    public async Task InitializeAsync()
    {
        await Task.Delay(5);
        Level1Value = 1;
    }
}

public class Level2Fixture : IAsyncInitializer
{
    public int? Level2Value { get; private set; }

    [ClassDataSource<Level3Fixture>]
    public required Level3Fixture Level3 { get; init; }

    public async Task InitializeAsync()
    {
        await Task.Delay(5);
        Level2Value = 2;
    }
}

public class Level3Fixture : IAsyncInitializer
{
    public int? Level3Value { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(5);
        Level3Value = 3;
    }
}

/// <summary>
/// First fixture for multiple parameter tests.
/// </summary>
public class FirstFixture : IAsyncInitializer
{
    public string? Name { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(10);
        Name = "First";
    }
}

/// <summary>
/// Second fixture for multiple parameter tests.
/// </summary>
public class SecondFixture : IAsyncInitializer
{
    public string? Name { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(10);
        Name = "Second";
    }
}

/// <summary>
/// Fixture for shared instance tests - tracks initialization count.
/// </summary>
public class SharedFixture : IAsyncInitializer
{
    private static int _initCount;
    public static int InitCount => _initCount;

    public int MyInitCount { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(5);
        MyInitCount = Interlocked.Increment(ref _initCount);
    }

    public static void ResetCount() => _initCount = 0;
}

#endregion

#region Test Classes

/// <summary>
/// Basic test: Single fixture injected via constructor.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<SimpleFixture>]
public class BasicConstructorInjectionTests
{
    private readonly int? _valueAtConstruction;

    public BasicConstructorInjectionTests(SimpleFixture fix)
    {
        _valueAtConstruction = fix.Value;
    }

    [Test]
    public async Task ValueShouldBeInitializedAtConstruction()
    {
        await Assert.That(_valueAtConstruction).IsEqualTo(123);
    }
}

/// <summary>
/// Nested test: Fixture with nested IAsyncInitializer property.
/// Both outer and inner should be initialized before constructor.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<OuterFixture>]
public class NestedAsyncInitializerTests
{
    private readonly int? _outerValueAtConstruction;
    private readonly int? _innerValueAtConstruction;

    public NestedAsyncInitializerTests(OuterFixture outer)
    {
        _outerValueAtConstruction = outer.OuterValue;
        _innerValueAtConstruction = outer.Inner.InnerValue;
    }

    [Test]
    public async Task OuterFixtureShouldBeInitialized()
    {
        await Assert.That(_outerValueAtConstruction).IsEqualTo(100);
    }

    [Test]
    public async Task InnerFixtureShouldBeInitialized()
    {
        await Assert.That(_innerValueAtConstruction).IsEqualTo(200);
    }
}

/// <summary>
/// Deep nesting test: Three levels of nested IAsyncInitializer.
/// All levels should be initialized depth-first before constructor.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<Level1Fixture>]
public class DeepNestedAsyncInitializerTests
{
    private readonly int? _level1Value;
    private readonly int? _level2Value;
    private readonly int? _level3Value;

    public DeepNestedAsyncInitializerTests(Level1Fixture level1)
    {
        _level1Value = level1.Level1Value;
        _level2Value = level1.Level2.Level2Value;
        _level3Value = level1.Level2.Level3.Level3Value;
    }

    [Test]
    public async Task Level1ShouldBeInitialized()
    {
        await Assert.That(_level1Value).IsEqualTo(1);
    }

    [Test]
    public async Task Level2ShouldBeInitialized()
    {
        await Assert.That(_level2Value).IsEqualTo(2);
    }

    [Test]
    public async Task Level3ShouldBeInitialized()
    {
        await Assert.That(_level3Value).IsEqualTo(3);
    }
}

/// <summary>
/// Multiple parameters test: Two fixtures injected via constructor.
/// Both should be initialized before constructor runs.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<FirstFixture, SecondFixture>]
public class MultipleConstructorParametersTests
{
    private readonly string? _firstName;
    private readonly string? _secondName;

    public MultipleConstructorParametersTests(FirstFixture first, SecondFixture second)
    {
        _firstName = first.Name;
        _secondName = second.Name;
    }

    [Test]
    public async Task FirstFixtureShouldBeInitialized()
    {
        await Assert.That(_firstName).IsEqualTo("First");
    }

    [Test]
    public async Task SecondFixtureShouldBeInitialized()
    {
        await Assert.That(_secondName).IsEqualTo("Second");
    }
}

/// <summary>
/// Combined injection test: Constructor parameter + property injection.
/// Both should be initialized.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<FirstFixture>]
public class CombinedInjectionTests
{
    private readonly string? _constructorFixtureName;

    [ClassDataSource<SecondFixture>]
    public required SecondFixture PropertyFixture { get; init; }

    public CombinedInjectionTests(FirstFixture constructorFixture)
    {
        _constructorFixtureName = constructorFixture.Name;
    }

    [Test]
    public async Task ConstructorFixtureShouldBeInitialized()
    {
        await Assert.That(_constructorFixtureName).IsEqualTo("First");
    }

    [Test]
    public async Task PropertyFixtureShouldBeInitialized()
    {
        await Assert.That(PropertyFixture.Name).IsEqualTo("Second");
    }
}

/// <summary>
/// Shared fixture test (PerClass): Same fixture instance shared across tests in the class.
/// Should be initialized only once.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<SharedFixture>(Shared = SharedType.PerClass)]
public class SharedPerClassTests
{
    private readonly int _initCountAtConstruction;
    private readonly SharedFixture _fixture;

    public SharedPerClassTests(SharedFixture fixture)
    {
        _initCountAtConstruction = fixture.MyInitCount;
        _fixture = fixture;
    }

    [Test]
    public async Task FixtureShouldBeInitializedOnce_Test1()
    {
        // Should be initialized (non-zero)
        await Assert.That(_initCountAtConstruction).IsGreaterThan(0);
    }

    [Test]
    public async Task FixtureShouldBeInitializedOnce_Test2()
    {
        // Same instance, same init count
        await Assert.That(_fixture.MyInitCount).IsEqualTo(_initCountAtConstruction);
    }

    [After(Class)]
    public static void ResetCounter()
    {
        SharedFixture.ResetCount();
    }
}

/// <summary>
/// Non-shared fixture test (SharedType.None): Each test gets its own instance.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<SimpleFixture>(Shared = SharedType.None)]
public class NonSharedFixtureTests
{
    private readonly int? _valueAtConstruction;

    public NonSharedFixtureTests(SimpleFixture fixture)
    {
        _valueAtConstruction = fixture.Value;
    }

    [Test]
    public async Task Test1_FixtureShouldBeInitialized()
    {
        await Assert.That(_valueAtConstruction).IsEqualTo(123);
    }

    [Test]
    public async Task Test2_FixtureShouldBeInitialized()
    {
        await Assert.That(_valueAtConstruction).IsEqualTo(123);
    }
}

#endregion
