using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4431;

/// <summary>
/// Comprehensive tests for generic test classes and methods with [GenerateGenericTest].
/// Covers all combinations of generic class/method with and without data sources.
/// </summary>

#region Test Data Types

public class TypeA
{
    public string Value => "TypeA";
}

public class TypeB : TypeA
{
    public new string Value => "TypeB";
}

public class TypeC
{
    public int Number => 42;
}

#endregion

#region 1. Generic Class (no data source)

[GenerateGenericTest(typeof(TypeA))]
[GenerateGenericTest(typeof(TypeB))]
public class GenericClassNoDataSource<T> where T : TypeA, new()
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task Should_Create_Instance()
    {
        var instance = new T();
        await Assert.That(instance).IsNotNull();
    }
}

#endregion

#region 2. Generic Class with MethodDataSource

[GenerateGenericTest(typeof(TypeA))]
[GenerateGenericTest(typeof(TypeB))]
public class GenericClassWithMethodDataSource<T> where T : TypeA, new()
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
    public async Task Should_Have_Number(int number)
    {
        await Assert.That(number).IsGreaterThan(0);
    }
}

#endregion

#region 3. Non-Generic Class with Generic Method (no data source)

public class NonGenericClassWithGenericMethod
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(string))]
    [GenerateGenericTest(typeof(TypeA))]
    public async Task GenericMethod_Should_Work<T>()
    {
        await Assert.That(typeof(T)).IsNotNull();
    }
}

#endregion

#region 4. Non-Generic Class with Generic Method + MethodDataSource

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
    public async Task GenericMethod_With_DataSource<T>(string input)
    {
        await Assert.That(input).IsNotNull();
        await Assert.That(typeof(T).IsValueType).IsTrue();
    }
}

#endregion

#region 5. Generic Class with Generic Method (both generic, no data source)

[GenerateGenericTest(typeof(TypeA))]
[GenerateGenericTest(typeof(TypeB))]
public class GenericClassWithGenericMethod<TClass> where TClass : TypeA, new()
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(string))]
    public async Task BothGeneric_Should_Work<TMethod>()
    {
        var classInstance = new TClass();
        await Assert.That(classInstance).IsNotNull();
        await Assert.That(typeof(TMethod)).IsNotNull();
    }
}

#endregion

#region 6. Generic Class with Constructor Parameters

[GenerateGenericTest(typeof(TypeA))]
[GenerateGenericTest(typeof(TypeB))]
public class GenericClassWithConstructor<T> where T : TypeA, new()
{
    private readonly string _label;

    public GenericClassWithConstructor()
    {
        _label = "default";
    }

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task Should_Have_Label()
    {
        await Assert.That(_label).IsEqualTo("default");
    }
}

#endregion

#region 7. Multiple Type Parameters

[GenerateGenericTest(typeof(TypeA), typeof(TypeC))]
[GenerateGenericTest(typeof(TypeB), typeof(TypeC))]
public class MultipleTypeParameters<T1, T2> where T1 : TypeA, new() where T2 : class, new()
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task Should_Handle_Multiple_Type_Parameters()
    {
        var instance1 = new T1();
        var instance2 = new T2();
        await Assert.That(instance1).IsNotNull();
        await Assert.That(instance2).IsNotNull();
    }
}

#endregion

#region 8. Generic Class with Inheritance

public abstract class GenericBaseClass<T> where T : new()
{
    protected T CreateInstance() => new T();
}

[GenerateGenericTest(typeof(TypeA))]
[GenerateGenericTest(typeof(TypeB))]
public class DerivedGenericClass<T> : GenericBaseClass<T> where T : TypeA, new()
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task Should_Create_From_Base()
    {
        var instance = CreateInstance();
        await Assert.That(instance).IsNotNull();
        await Assert.That(instance.Value).IsNotNull();
    }
}

#endregion

#region 9. Generic Class with ClassDataSource (init-only property)

public class TestDataSource
{
    public string Data => "SharedData";
}

[GenerateGenericTest(typeof(TypeA))]
[GenerateGenericTest(typeof(TypeB))]
public class GenericClassWithClassDataSource<T> where T : TypeA, new()
{
    [ClassDataSource<TestDataSource>(Shared = SharedType.PerTestSession)]
    public TestDataSource DataSource { get; init; } = null!;

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task Should_Have_DataSource()
    {
        await Assert.That(DataSource).IsNotNull();
        await Assert.That(DataSource.Data).IsEqualTo("SharedData");
    }
}

#endregion

#region 10. Generic Class with Generic Method + ClassDataSource

[GenerateGenericTest(typeof(TypeA))]
[GenerateGenericTest(typeof(TypeB))]
public class GenericClassGenericMethodWithDataSources<TClass> where TClass : TypeA, new()
{
    [ClassDataSource<TestDataSource>(Shared = SharedType.PerTestSession)]
    public TestDataSource DataSource { get; init; } = null!;

    public static IEnumerable<bool> GetBooleans()
    {
        yield return true;
        yield return false;
    }

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(double))]
    [MethodDataSource(nameof(GetBooleans))]
    public async Task FullyGeneric_With_DataSources<TMethod>(bool flag) where TMethod : struct
    {
        await Assert.That(DataSource).IsNotNull();
        await Assert.That(typeof(TClass).IsClass).IsTrue();
        await Assert.That(typeof(TMethod).IsValueType).IsTrue();
    }
}

#endregion
