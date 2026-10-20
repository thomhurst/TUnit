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
    [EngineTest(ExpectedResult.Failure)]
    public async Task GenericMethodWithClassConstraint_InvalidType<T>(T value) where T : class
    {
        // Never runs: test building fails with a GenericTypeResolutionException in both engine modes (#7097) - int is a value type
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

    // TUnit0001 reports the broad declared type at compile time; it is suppressed to check the run-time failure.
#pragma warning disable TUnit0001
    [Test]
    [NonImplementingClassDataSource]
    [EngineTest(ExpectedResult.Failure)]
    public async Task GenericMethodWithInterfaceConstraint_InvalidType<T>(T value) where T : ITestInterface
    {
        // Never runs: test building fails with a GenericTypeResolutionException in both engine modes (#7097) - NonImplementingClass doesn't implement ITestInterface
        await Task.CompletedTask;
    }
#pragma warning restore TUnit0001

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
    [EngineTest(ExpectedResult.Failure)]
    public async Task GenericMethodWithStructConstraint_InvalidType<T>(T value) where T : struct
    {
        // Never runs: test building fails with a GenericTypeResolutionException in both engine modes (#7097) - string is not a value type
        await Task.CompletedTask;
    }

    // One valid row and one invalid row: the valid row runs and the invalid row fails, in both modes (#7097)
    [Test]
    [Arguments(5)]
    [Arguments("invalid")]
    public async Task GenericMethodWithStructConstraint_MixedRows<T>(T value) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
    }

    // A constraint violation is a test-building error, so it is reported as a failure even when the
    // test is skipped. Both modes must agree (#7097).
    [Test]
    [Arguments("invalid")]
    [Skip("Skip does not hide a generic constraint violation")]
    [EngineTest(ExpectedResult.Failure)]
    public async Task GenericMethodWithStructConstraint_SkippedInvalidType<T>(T value) where T : struct
    {
        await Task.CompletedTask;
    }
}

// Generic class with IComparable constraint - uses the type inside method body, not as parameter
// This pattern matches ChildTest4431 which works correctly
[GenerateGenericTest(typeof(int))]
public class GenericClassWithComparableConstraint<T> where T : IComparable<T>
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task TestComparable()
    {
        // Create test value inside the method - T is int
        T value = (T)(object)42;
        await Assert.That(value).IsNotEqualTo(default(T));
        // This should compile because T has IComparable<T> constraint
        var comparison = value.CompareTo(value);
        await Assert.That(comparison).IsEqualTo(0);
    }
}

// Separate test class for string IComparable
[GenerateGenericTest(typeof(string))]
public class GenericClassWithComparableConstraintString<T> where T : IComparable<T>
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task TestComparable()
    {
        // Create test value inside the method - T is string
        T value = (T)(object)"hello";
        await Assert.That(value).IsNotEqualTo(default(T));
        var comparison = value.CompareTo(value);
        await Assert.That(comparison).IsEqualTo(0);
    }
}

// Generic class with struct constraint
[GenerateGenericTest(typeof(int))]
public class GenericClassWithStructConstraint<T> where T : struct
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task TestStruct()
    {
        // Create test value inside the method - T is int
        T value = (T)(object)42;
        await Assert.That(value).IsNotEqualTo(default(T));
        T defaultValue = default;
        await Assert.That(value).IsNotEqualTo(defaultValue);
    }
}

// Separate test class for double struct
[GenerateGenericTest(typeof(double))]
public class GenericClassWithStructConstraintDouble<T> where T : struct
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task TestStruct()
    {
        // Create test value inside the method - T is double
        T value = (T)(object)3.14;
        await Assert.That(value).IsNotEqualTo(default(T));
        T defaultValue = default;
        await Assert.That(value).IsNotEqualTo(defaultValue);
    }
}

// Generic class with class constraint
[GenerateGenericTest(typeof(string))]
public class GenericClassWithClassConstraint<T> where T : class
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task TestClass()
    {
        // Create test value inside the method - T is string
        T value = (T)(object)"hello";
        await Assert.That(value).IsNotEqualTo(default(T));
    }
}

// Generic class with interface constraint
[GenerateGenericTest(typeof(TestClass))]
public class GenericClassWithInterfaceConstraint<T> where T : ITestInterface, new()
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task TestInterface()
    {
        // Create test value inside the method using new() constraint
        T value = new T();
        await Assert.That(value).IsNotEqualTo(default(T));
        value.TestMethod(); // This should compile because T has ITestInterface constraint
    }
}

// Data source that yields two strings (each breaks a struct constraint)
public class TwoStringsDataSourceForConstraint : DataSourceGeneratorAttribute<string>
{
    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => "first";
        yield return () => "second";
    }
}

// Data source that yields no strings; SkipIfEmpty turns the empty source into a skipped test
public class EmptyStringsDataSourceForConstraint : DataSourceGeneratorAttribute<string>
{
    public EmptyStringsDataSourceForConstraint()
    {
        SkipIfEmpty = true;
    }

    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield break;
    }
}

public class ConstraintBaseClass
{
}

// Data source that yields the base-type constraint itself (a legal type argument for 'where T : ConstraintBaseClass')
public class ConstraintBaseClassDataSource : DataSourceGeneratorAttribute<ConstraintBaseClass>
{
    protected override IEnumerable<Func<ConstraintBaseClass>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => new ConstraintBaseClass();
    }
}

// Data source that yields the interface constraint itself (a legal type argument for 'where T : ITestInterface')
public class TestInterfaceDataSource : DataSourceGeneratorAttribute<ITestInterface>
{
    protected override IEnumerable<Func<ITestInterface>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => new TestClass();
    }
}

// Data source whose type only satisfies 'where T : IEnumerable<object>' through interface variance
public class StringListDataSourceForConstraint : DataSourceGeneratorAttribute<List<string>>
{
    protected override IEnumerable<Func<List<string>>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => ["a", "b"];
    }
}

// Every row whose inferred type argument breaks the method's constraints is its own failed test, and
// legal type arguments that only match a constraint exactly or through variance still run. Source-gen
// and reflection modes must produce the same results (#7097).
public class GenericConstraintRowParityTests
{
    // Two attributes that infer the same invalid type: two failed tests
    [Test]
    [Arguments("first")]
    [Arguments("second")]
    [EngineTest(ExpectedResult.Failure)]
    public async Task TwoInvalidArgumentsRows<T>(T value) where T : struct
    {
        await Task.CompletedTask;
    }

    // One data source that yields two invalid rows: two failed tests
    [Test]
    [TwoStringsDataSourceForConstraint]
    [EngineTest(ExpectedResult.Failure)]
    public async Task TwoInvalidDataSourceRows<T>(T value) where T : struct
    {
        await Task.CompletedTask;
    }

    // An empty source yields no rows, so there is nothing to fail: the empty source is skipped
    [Test]
    [EmptyStringsDataSourceForConstraint]
    public async Task EmptyInvalidDataSource<T>(T value) where T : struct
    {
        await Task.CompletedTask;
    }

    [Test]
    [ConstraintBaseClassDataSource]
    [EngineTest(ExpectedResult.Pass)]
    public async Task ExactBaseTypeConstraintMatch<T>(T value) where T : ConstraintBaseClass
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(ConstraintBaseClass));
    }

    [Test]
    [TestInterfaceDataSource]
    [EngineTest(ExpectedResult.Pass)]
    public async Task ExactInterfaceConstraintMatch<T>(T value) where T : ITestInterface
    {
        // Source-gen infers ITestInterface (the data source's declared type); reflection infers the
        // runtime type of the value (TestClass). Both satisfy the constraint.
        await Assert.That(typeof(ITestInterface).IsAssignableFrom(typeof(T))).IsTrue();
    }

    [Test]
    [StringListDataSourceForConstraint]
    [EngineTest(ExpectedResult.Pass)]
    public async Task VariantInterfaceConstraintMatch<T>(T value) where T : IEnumerable<object>
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(List<string>));
    }

    // List<string[]> only satisfies 'where T : IEnumerable<object[]>' through variance plus array covariance
    [Test]
    [StringArrayListDataSourceForConstraint]
    [EngineTest(ExpectedResult.Pass)]
    public async Task ArrayCovariantInterfaceConstraintMatch<T>(T value) where T : IEnumerable<object[]>
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(List<string[]>));
    }

    // (List<string[]>, string) satisfies 'where T : IEnumerable<U[]>' once U is substituted inside the array
    [Test]
    [StringArrayListAndStringDataSourceForConstraint]
    [EngineTest(ExpectedResult.Pass)]
    public async Task ArrayOfTypeParameterConstraintMatch<T, U>(T value, U element) where T : IEnumerable<U[]>
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(List<string[]>));
    }

    // List<Func<string>> only satisfies 'where T : IEnumerable<Func<object>>' through interface plus delegate variance
    [Test]
    [StringFuncListDataSourceForConstraint]
    [EngineTest(ExpectedResult.Pass)]
    public async Task DelegateVariantInterfaceConstraintMatch<T>(T value) where T : IEnumerable<Func<object>>
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(List<Func<string>>));
    }

    // A nested interface inherits its containing class's type arguments: variance on IOut's own argument
    // accepts ConstraintOuter<string>.IOut<string> for ConstraintOuter<string>.IOut<object>
    [Test]
    [MatchingOuterPayloadDataSource]
    [EngineTest(ExpectedResult.Pass)]
    public async Task NestedVariantInterfaceConstraintMatch<T>(T value) where T : ConstraintOuter<string>.IOut<object>
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(MatchingOuterPayload));
    }

    // ...but a different containing type argument (int, not string) breaks the constraint whatever IOut's own argument is
    // TUnit0001 reports the broad declared type at compile time; it is suppressed to check the run-time failure.
#pragma warning disable TUnit0001
    [Test]
    [MismatchedOuterPayloadDataSource]
    [EngineTest(ExpectedResult.Failure)]
    public async Task NestedInterfaceWithOtherContainingTypeArgument<T>(T value) where T : ConstraintOuter<string>.IOut<object>
    {
        await Task.CompletedTask;
    }
#pragma warning restore TUnit0001

    // An instance data source needs the test class instance to yield its rows, even when those rows
    // break the method's constraints: two failed tests with the constraint message
    [Test]
    [InstanceTwoStringsDataSourceForConstraint]
    [EngineTest(ExpectedResult.Failure)]
    public async Task TwoInvalidInstanceDataSourceRows<T>(T value) where T : struct
    {
        await Task.CompletedTask;
    }

    // The row's own factory runs before the constraint is checked, so its exception is the one reported
    [Test]
    [ThrowingStringDataSourceForConstraint]
    [EngineTest(ExpectedResult.Failure)]
    public async Task InvalidRowWhoseFactoryThrows<T>(T value) where T : struct
    {
        await Task.CompletedTask;
    }
}

// Data source whose type only satisfies 'where T : IEnumerable<object[]>' through variance and array covariance
public class StringArrayListDataSourceForConstraint : DataSourceGeneratorAttribute<List<string[]>>
{
    protected override IEnumerable<Func<List<string[]>>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => [["a"]];
    }
}

// Data source whose first type satisfies 'where T : IEnumerable<U[]>' once U is inferred from its second type
public class StringArrayListAndStringDataSourceForConstraint : DataSourceGeneratorAttribute<List<string[]>, string>
{
    protected override IEnumerable<Func<(List<string[]>, string)>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => ([["a"]], "a");
    }
}

// Data source whose type only satisfies 'where T : IEnumerable<Func<object>>' through interface and delegate variance
public class StringFuncListDataSourceForConstraint : DataSourceGeneratorAttribute<List<Func<string>>>
{
    protected override IEnumerable<Func<List<Func<string>>>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => [() => "a"];
    }
}

public class ConstraintOuter<TOuter>
{
    public interface IOut<out TValue>
    {
    }
}

public class MatchingOuterPayload : ConstraintOuter<string>.IOut<string>
{
}

public class MismatchedOuterPayload : ConstraintOuter<int>.IOut<string>
{
}

public class MatchingOuterPayloadDataSource : DataSourceGeneratorAttribute<MatchingOuterPayload>
{
    protected override IEnumerable<Func<MatchingOuterPayload>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => new MatchingOuterPayload();
    }
}

public class MismatchedOuterPayloadDataSource : DataSourceGeneratorAttribute<MismatchedOuterPayload>
{
    protected override IEnumerable<Func<MismatchedOuterPayload>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => new MismatchedOuterPayload();
    }
}

// Data source that needs the test class instance to yield its two strings (each breaks a struct constraint)
public class InstanceTwoStringsDataSourceForConstraint : DataSourceGeneratorAttribute<string>, IAccessesInstanceData
{
    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        if (metadata.TestClassInstance is null)
        {
            throw new InvalidOperationException("InstanceTwoStringsDataSourceForConstraint needs the test class instance");
        }

        yield return () => "first";
        yield return () => "second";
    }
}

// Data source whose row factory throws (its row would also break a struct constraint)
public class ThrowingStringDataSourceForConstraint : DataSourceGeneratorAttribute<string>
{
    public const string Message = "The row factory of ThrowingStringDataSourceForConstraint ran";

    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => throw new InvalidOperationException(Message);
    }
}

// A generic class and a generic method that both take [Arguments]: the invalid method row fails and
// the valid one runs, in both modes (#7097).
[Arguments(1)]
public class GenericConstraintClassAndMethodArgumentsTests<TClass>(TClass classValue)
{
    [Test]
    [Arguments(5)]
    [Arguments("invalid")]
    public async Task MixedMethodRows<TMethod>(TMethod value) where TMethod : struct
    {
        await Assert.That(classValue).IsEqualTo((TClass)(object)1);
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
    }
}

// A method constraint that mentions the class type parameter is checked against the class type argument:
// int satisfies 'IComparable<int>' and runs, string does not and fails, in both modes.
[Arguments(1)]
public class GenericConstraintMethodReferencesClassTests<TClass>(TClass classValue)
{
    [Test]
    [Arguments(5)]
    [Arguments("invalid")]
    public async Task ClassTypeParameterConstraintRows<TMethod>(TMethod value) where TMethod : IComparable<TClass>
    {
        await Assert.That(classValue).IsEqualTo((TClass)(object)1);
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
    }
}
