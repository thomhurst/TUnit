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
        await Assert.That(value).IsNotNull();
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
