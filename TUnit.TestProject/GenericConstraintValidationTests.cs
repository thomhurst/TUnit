using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Custom data source that provides an int (which violates class constraint)
public class IntDataSourceForConstraint : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => 42;
    }
}

// Custom data source that provides a string (which satisfies class constraint)
public class StringDataSourceForConstraint : DataSourceGeneratorAttribute<string>
{
    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => "valid";
    }
}

// Interface for constraint testing
public interface ITestInterface
{
    void TestMethod();
}

// Class that implements the interface
public class TestClass : ITestInterface
{
    public void TestMethod() { }
}

// Class that doesn't implement the interface
public class NonImplementingClass
{
}

// Data source that provides TestClass (implements ITestInterface)
public class TestClassDataSource : DataSourceGeneratorAttribute<TestClass>
{
    protected override IEnumerable<Func<TestClass>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => new TestClass();
    }
}

// Data source that provides NonImplementingClass (doesn't implement ITestInterface)
public class NonImplementingClassDataSource : DataSourceGeneratorAttribute<NonImplementingClass>
{
    protected override IEnumerable<Func<NonImplementingClass>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => new NonImplementingClass();
    }
}

public class GenericConstraintValidationTests
{
    [Test]
    [StringDataSourceForConstraint]
    [EngineTest(ExpectedResult.Pass)]
    public async Task GenericMethodWithClassConstraint_ValidType<T>(T value) where T : class
    {
        // This should pass - string satisfies class constraint
        await Assert.That(value).IsNotEqualTo(default(T));
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
    }

    [Test]
    [IntDataSourceForConstraint]
    [Skip("Expected to fail - int violates class constraint")]
    public async Task GenericMethodWithClassConstraint_InvalidType<T>(T value) where T : class
    {
        // This should fail during constraint validation - int is a value type
        await Task.CompletedTask;
    }

    [Test]
    [TestClassDataSource]
    [EngineTest(ExpectedResult.Pass)]
    public async Task GenericMethodWithInterfaceConstraint_ValidType<T>(T value) where T : ITestInterface
    {
        // This should pass - TestClass implements ITestInterface
        await Assert.That(typeof(T)).IsEqualTo(typeof(TestClass));
        await Task.CompletedTask;
    }

    [Test]
    [NonImplementingClassDataSource]
    [Skip("Expected to fail - NonImplementingClass doesn't implement ITestInterface")]
    public async Task GenericMethodWithInterfaceConstraint_InvalidType<T>(T value) where T : ITestInterface
    {
        // This should fail during constraint validation - NonImplementingClass doesn't implement ITestInterface
        await Task.CompletedTask;
    }

    [Test]
    [Arguments(5)]
    [EngineTest(ExpectedResult.Pass)]
    public async Task GenericMethodWithStructConstraint_ValidType<T>(T value) where T : struct
    {
        // This should pass - int satisfies struct constraint
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
    }

    [Test]
    [Arguments("invalid")]
    [Skip("Expected to fail - string violates struct constraint")]
    public async Task GenericMethodWithStructConstraint_InvalidType<T>(T value) where T : struct
    {
        // This should fail during constraint validation - string is not a value type
        await Task.CompletedTask;
    }
}

// Generic class with IComparable constraint
[EngineTest(ExpectedResult.Pass)]
public class GenericClassWithComparableConstraint<T> where T : IComparable<T>
{
    [Test]
    [Arguments(42)]        // int implements IComparable<int> - should work
    [Arguments("hello")]   // string implements IComparable<string> - should work
    public async Task TestComparable(T value)
    {
        await Assert.That(value).IsNotEqualTo(default(T));
        // This should compile because T has IComparable<T> constraint
        var comparison = value.CompareTo(value);
        await Assert.That(comparison).IsEqualTo(0);
    }
}

// Generic class with struct constraint
[EngineTest(ExpectedResult.Pass)]
public class GenericClassWithStructConstraint<T> where T : struct
{
    [Test]
    [Arguments(42)]        // int is a struct - should work
    [Arguments(3.14)]      // double is a struct - should work
    public async Task TestStruct(T value)
    {
        await Assert.That(value).IsNotEqualTo(default(T));
        // This should compile because T has struct constraint
        T defaultValue = default;
        await Assert.That(value).IsNotEqualTo(defaultValue);
    }
}

// Generic class with class constraint
[EngineTest(ExpectedResult.Pass)]
public class GenericClassWithClassConstraint<T> where T : class
{
    [Test]
    [Arguments("hello")]     // string is a class - should work
    public async Task TestClass(T? value)
    {
        // This should compile because T has class constraint
        if (value != null)
        {
            await Assert.That(value).IsNotEqualTo(default(T));
        }
    }
}

// Generic class with interface constraint
[EngineTest(ExpectedResult.Pass)]
public class GenericClassWithInterfaceConstraint<T> where T : ITestInterface
{
    [Test]
    [TestClassDataSource]  // TestClass implements ITestInterface - should work
    public async Task TestInterface(T value)
    {
        await Assert.That(value).IsNotEqualTo(default(T));
        value.TestMethod(); // This should compile because T has ITestInterface constraint
    }
}
