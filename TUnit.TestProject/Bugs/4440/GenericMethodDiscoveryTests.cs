using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4440;

/// <summary>
/// Test fixtures for issue #4440: Generic method with [GenerateGenericTest] + [MethodDataSource]
/// fails to be discovered at runtime in reflection mode.
/// </summary>

#region Scenario 1: Non-generic class + generic method + data source (original bug)

/// <summary>
/// The original bug scenario. This should produce 4 tests:
/// - GenericMethodWithDataSource&lt;int&gt;("hello")
/// - GenericMethodWithDataSource&lt;int&gt;("world")
/// - GenericMethodWithDataSource&lt;double&gt;("hello")
/// - GenericMethodWithDataSource&lt;double&gt;("world")
/// </summary>
public class NonGenericClassWithGenericMethodAndDataSource
{
    public static IEnumerable<string> GetStrings()
    {
        yield return "hello";
        yield return "world";
    }

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(double))]
    [MethodDataSource(nameof(GetStrings))]
    public async Task GenericMethodWithDataSource<T>(string input)
    {
        await Assert.That(input).IsNotNullOrEmpty();
        await Assert.That(typeof(T).IsValueType).IsTrue();
    }
}

#endregion

#region Scenario 2: Non-generic class + generic method (no data source)

/// <summary>
/// Tests generic method without data source. Should produce 3 tests.
/// </summary>
public class Bug4440_NonGenericClassWithGenericMethod
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(string))]
    [GenerateGenericTest(typeof(object))]
    public async Task GenericMethod_Should_Work<T>()
    {
        await Assert.That(typeof(T)).IsNotNull();
    }
}

#endregion

#region Scenario 3: Generic class + non-generic method + data source

/// <summary>
/// Generic class with [GenerateGenericTest] and a non-generic method with data source.
/// Expected: 2 class types × 3 data items = 6 tests.
/// </summary>
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(object))]
public class Bug4440_GenericClassWithMethodDataSource<T> where T : class
{
    public static IEnumerable<int> GetNumbers()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    [MethodDataSource(nameof(GetNumbers))]
    public async Task TestWithDataSource(int number)
    {
        await Assert.That(typeof(T).IsClass).IsTrue();
        await Assert.That(number).IsGreaterThan(0);
    }
}

#endregion

#region Scenario 4: Generic class + generic method + data source (cartesian product)

/// <summary>
/// Cartesian product scenario: 2 class types × 2 method types × 2 data items = 8 tests.
/// </summary>
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(object))]
public class Bug4440_GenericClassGenericMethodWithDataSources<TClass> where TClass : class
{
    public static IEnumerable<bool> GetBools()
    {
        yield return true;
        yield return false;
    }

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(double))]
    [MethodDataSource(nameof(GetBools))]
    public async Task CartesianProduct<TMethod>(bool flag) where TMethod : struct
    {
        await Assert.That(typeof(TClass).IsClass).IsTrue();
        await Assert.That(typeof(TMethod).IsValueType).IsTrue();
        // Just verify we got a boolean value
        await Assert.That(flag == true || flag == false).IsTrue();
    }
}

#endregion

#region Additional Scenarios

/// <summary>
/// Tests method with multiple type parameters. Should produce 2 tests.
/// </summary>
public class GenericMethodMultipleTypeParams
{
    public static IEnumerable<string> GetValues()
    {
        yield return "value";
    }

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    [GenerateGenericTest(typeof(int), typeof(string))]
    [GenerateGenericTest(typeof(double), typeof(object))]
    [MethodDataSource(nameof(GetValues))]
    public async Task MultiTypeParamMethod<T1, T2>(string value)
    {
        await Assert.That(typeof(T1).IsValueType).IsTrue();
        await Assert.That(typeof(T2).IsClass).IsTrue();
        await Assert.That(value).IsNotNull();
    }
}

/// <summary>
/// Tests generic method with constraints. Should produce 2 tests.
/// </summary>
public class GenericMethodWithConstraints
{
    public static IEnumerable<int> GetData()
    {
        yield return 42;
    }

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(double))]
    [MethodDataSource(nameof(GetData))]
    public async Task ConstrainedGenericMethod<T>(int value) where T : struct
    {
        await Assert.That(typeof(T).IsValueType).IsTrue();
        await Assert.That(value).IsEqualTo(42);
    }
}

#endregion
