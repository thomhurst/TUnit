using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7107;

// Each type argument inferred from a row or named by [GenerateGenericTest] that breaks a generic
// constraint must surface as one failed test, with the same name and message in source-generated
// and reflection modes. Valid rows keep running, and a bad class row must never stop discovery of
// the other tests in the assembly (#7107).
//
// Classes that mix valid and invalid rows carry no [EngineTest] attribute: TUnit.Engine.Tests
// targets them by name. The `/**[EngineTest=Pass]` run must not pick up their failing rows.

// Case 1: class-level rows; "x" breaks `where T : struct`.
[Arguments(1)]
[Arguments("x")]
public class ClassRowConstraintTests<T>(T value) where T : struct
{
    [Test]
    public async Task Run() => await Assert.That(value).IsNotEqualTo(default(T));
}

// Case 1b: the only class row is invalid.
[Arguments("x")]
public class OnlyInvalidClassRowConstraintTests<T>(T value) where T : struct
{
    [Test]
    public async Task Run() => await Assert.That(value).IsNotEqualTo(default(T));
}

// Case 2: class-level [GenerateGenericTest] with an invalid type.
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(string))]
public class ClassGenerateGenericConstraintTests<T> where T : struct
{
    [Test]
    public async Task Run() => await Assert.That(typeof(T).IsValueType).IsTrue();
}

// Case 3: a method-level row infers the class type argument.
public class MethodRowInfersClassConstraintTests<T> where T : struct
{
    [Test]
    [Arguments(1)]
    [Arguments("x")]
    public async Task Run(T value) => await Assert.That(value).IsNotEqualTo(default(T));
}

public class MethodGenerateGenericConstraintTests
{
    // Case 4: method-level [GenerateGenericTest] with an invalid type.
    [Test]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(string))]
    public async Task Run<T>() where T : struct
        => await Assert.That(typeof(T).IsValueType).IsTrue();
}

public class MatrixInfersTypeConstraintTests
{
    // Case 5: IInfersType<T> infers an invalid type.
    [Test]
    [MatrixDataSource]
    [EngineTest(ExpectedResult.Failure)]
    public async Task Invalid<T>([Matrix<string>("a", "b")] T value) where T : struct
        => await Assert.That(value).IsNotEqualTo(default(T));

    // Control: a valid IInfersType<T>.
    [Test]
    [MatrixDataSource]
    [EngineTest(ExpectedResult.Pass)]
    public async Task Valid<T>([Matrix<int>(1, 2)] T value) where T : struct
        => await Assert.That(value).IsNotEqualTo(default(T));
}

// Several invalid class rows that infer the same type: one failed test per row and test method.
// The interface constraint refers to T, so the message names the substituted interface.
[Arguments("x")]
[Arguments(DayOfWeek.Monday)]
[Arguments("y")]
public class SeveralInvalidClassRowsConstraintTests<T>(T value) where T : struct, IEquatable<T>
{
    [Test]
    public async Task First() => await Assert.That(value).IsNotEqualTo(default(T));

    [Test]
    public async Task Second() => await Assert.That(value).IsNotEqualTo(default(T));
}

public class SeveralInvalidMethodRowsConstraintTests
{
    // Two invalid method rows that infer the same type: one failed test per row.
    [Test]
    [Arguments(1)]
    [Arguments("x")]
    [Arguments("y")]
    public async Task Run<T>(T value) where T : struct
        => await Assert.That(value).IsNotEqualTo(default(T));
}

// Control: an ordinary test in the same assembly. A bad class row above must not hide it.
[EngineTest(ExpectedResult.Pass)]
public class ConstraintViolationNeighbourTests
{
    [Test]
    public void StillDiscovered()
    {
    }
}
