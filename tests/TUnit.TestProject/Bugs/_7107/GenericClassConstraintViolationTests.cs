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

// Nullable<T> boxes to IComparable, but it does not satisfy 'where T : IComparable' (CS0313).
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(int?))]
public class NullableInterfaceConstraintTests<T> where T : IComparable
{
    [Test]
    public async Task Run() => await Assert.That(typeof(T)).IsEqualTo(typeof(int));
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

// A class row combined with generic method rows: the valid method row runs and the invalid one fails.
[Arguments(1)]
public class ClassAndMethodRowsConstraintTests<T>(T value)
{
    [Test]
    [Arguments(2)]
    [Arguments("x")]
    public async Task Run<TValue>(TValue other) where TValue : struct
        => await Assert.That(other).IsNotEqualTo(default(TValue));

    public T Value => value;
}

// A variant interface satisfies the constraint: List<string> is an IEnumerable<object>.
public class VariantInterfaceConstraintTests
{
    [Test]
    [GenerateGenericTest(typeof(List<string>))]
    [EngineTest(ExpectedResult.Pass)]
    public async Task Run<T>() where T : IEnumerable<object>
        => await Assert.That(typeof(IEnumerable<object>).IsAssignableFrom(typeof(T))).IsTrue();
}

[GenerateGenericTest(typeof(List<string>))]
[EngineTest(ExpectedResult.Pass)]
public class VariantInterfaceClassConstraintTests<T> where T : IEnumerable<object>
{
    [Test]
    public async Task Run() => await Assert.That(typeof(IEnumerable<object>).IsAssignableFrom(typeof(T))).IsTrue();
}

// A constraint naming another type parameter: 'String' is not a 'Stream'.
[GenerateGenericTest(typeof(MemoryStream), typeof(Stream))]
[GenerateGenericTest(typeof(string), typeof(Stream))]
public class TypeParameterConstraintTests<T, TBase> where T : TBase
{
    [Test]
    public async Task Run() => await Assert.That(typeof(TBase).IsAssignableFrom(typeof(T))).IsTrue();
}

// A new() constraint: a type without a public parameterless constructor fails.
[GenerateGenericTest(typeof(object))]
[GenerateGenericTest(typeof(NoParameterlessConstructor))]
public class NewConstraintTests<T> where T : new()
{
    [Test]
    public async Task Run() => await Assert.That(new T() is not null).IsTrue();
}

// Nullable<T> is a value type, but it does not satisfy 'where T : struct'.
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(int?))]
public class NullableStructConstraintTests<T> where T : struct
{
    [Test]
    public async Task Run() => await Assert.That(typeof(T).IsValueType).IsTrue();
}

// [GenerateGenericTest] supplies the class type argument, so the method row's value does not infer
// another one: SuppliedClassTypeMethodRowTests<object>.Run(1) is valid, and no violation is reported
// for the int that the row's value would otherwise infer.
[GenerateGenericTest(typeof(object))]
[EngineTest(ExpectedResult.Pass)]
public class SuppliedClassTypeMethodRowTests<T> where T : class
{
    [Test]
    [Arguments(1)]
    public async Task Run(T value) => await Assert.That(value).IsEqualTo((T)(object)1);
}

public class NoParameterlessConstructor(int value)
{
    public int Value => value;
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

// A method constraint that names the class type parameter (`where U : IEnumerable<T>`) is checked
// with the class's type argument substituted in: List<int> satisfies IEnumerable<int>, so the test runs.
[GenerateGenericTest(typeof(int))]
[EngineTest(ExpectedResult.Pass)]
public class MethodConstraintNamesClassTypeParameterTests<T>
{
    [Test]
    [GenerateGenericTest(typeof(List<int>))]
    public async Task Run<U>() where U : IEnumerable<T>
    {
        await Assert.That(typeof(U)).IsEqualTo(typeof(List<int>));
    }
}

// The same constraint broken: List<string> is not an IEnumerable<int>, so the instantiation fails.
[GenerateGenericTest(typeof(int))]
[EngineTest(ExpectedResult.Failure)]
public class MethodConstraintNamesClassTypeParameterViolationTests<T>
{
    [Test]
    [GenerateGenericTest(typeof(List<string>))]
    public async Task Run<U>() where U : IEnumerable<T>
    {
        await Task.CompletedTask;
    }
}
// A constraint whose type argument is an array of another type parameter (`where T : IEnumerable<U[]>`)
// is checked with U substituted into the array: List<int[]> satisfies IEnumerable<int[]>, so the test runs.
[EngineTest(ExpectedResult.Pass)]
public class ArrayElementConstraintTests
{
    [Test]
    [GenerateGenericTest(typeof(List<int[]>), typeof(int))]
    public async Task Run<T, U>() where T : IEnumerable<U[]>
        => await Assert.That(typeof(T)).IsEqualTo(typeof(List<int[]>));
}

// An array type argument breaks 'struct': the message names it as reflection does ('Int32[]'), in both modes.
public class ArrayTypeArgumentConstraintTests
{
    [Test]
    [Arguments(1)]
    [Arguments(new[] { 1 })]
    public async Task Run<T>(T value) where T : struct
        => await Assert.That(value).IsNotEqualTo(default(T));
}

// The same constraint broken: string[] is not an int[], so List<string[]> is not an IEnumerable<int[]>,
// and both modes name the interface constraint that fails.
public class ArrayElementConstraintViolationTests
{
    [Test]
    [GenerateGenericTest(typeof(List<string[]>), typeof(int))]
    public async Task Run<T, U>() where T : IEnumerable<U[]>
        => await Task.CompletedTask;
}

public class ConstraintOuter<TOuter>
{
    public interface IOut<TInner>;
}

public class NestedConstraintPayload : ConstraintOuter<int>.IOut<string>;

// A constraint on a nested type of a generic class (`where T : ConstraintOuter<U>.IOut<string>`) is
// checked with U substituted into the containing type: the payload implements
// ConstraintOuter<int>.IOut<string>, so the test runs.
[EngineTest(ExpectedResult.Pass)]
public class NestedTypeConstraintTests
{
    [Test]
    [GenerateGenericTest(typeof(NestedConstraintPayload), typeof(int))]
    public async Task Run<T, U>() where T : ConstraintOuter<U>.IOut<string>
        => await Assert.That(typeof(T)).IsEqualTo(typeof(NestedConstraintPayload));
}

// The same constraint broken: the payload is not a ConstraintOuter<string>.IOut<string>.
public class NestedTypeConstraintViolationTests
{
    [Test]
    [GenerateGenericTest(typeof(NestedConstraintPayload), typeof(string))]
    public async Task Run<T, U>() where T : ConstraintOuter<U>.IOut<string>
        => await Task.CompletedTask;
}
