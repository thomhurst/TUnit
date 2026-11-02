namespace TUnit.TestProject.Bugs._7222;

// A method-level [GenerateGenericTest] that lists no type arguments, or the wrong number of them, fails as a
// test of its own in both modes, with the same message (#7222). Source-generated mode used to drop it silently.
//
// Not marked [EngineTest]: the failures are intentional. TUnit.Engine.Tests (Issue7222Tests) checks each
// method's outcome by name in both modes.
public class GenerateGenericTestArityTests
{
    [Test]
    [GenerateGenericTest(typeof(int), typeof(string))]
    public void TooManyTypeArguments<T>()
    {
    }

    [Test]
    [GenerateGenericTest]
    public void NoTypeArguments<T>()
    {
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    public void TooFewTypeArguments<T1, T2>()
    {
    }

    // The valid one runs; the wrongly sized one between them fails on its own.
    [Test]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(int), typeof(string))]
    [GenerateGenericTest(typeof(string))]
    public async Task MixedArity<T>()
        => await Assert.That(typeof(T) == typeof(int) || typeof(T) == typeof(string)).IsTrue();

    // An array type argument counts like any other type.
    [Test]
    [GenerateGenericTest(typeof(int[]))]
    public async Task ArrayTypeArgument<T>()
        => await Assert.That(typeof(T)).IsEqualTo(typeof(int[]));
}

// Reflection mode resolves the method on each concrete class, so the wrongly sized one fails on each.
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(long))]
public class GenericClassGenerateGenericTestArityTests<TClass>
{
    [Test]
    [GenerateGenericTest(typeof(string))]
    [GenerateGenericTest(typeof(string), typeof(int))]
    public async Task MixedArity<TMethod>()
        => await Assert.That(typeof(TMethod)).IsEqualTo(typeof(string));
}

// No method-level [GenerateGenericTest] is valid, so no instantiation names the concrete class: it still fails on it.
[GenerateGenericTest(typeof(int))]
public class OnlyWronglySizedOnGenericClassTests<TClass>
{
    [Test]
    [GenerateGenericTest]
    public void NoTypeArguments<TMethod>()
    {
    }
}

// The arity failure and the constraint failure keep their declaration order, so each test id carries the
// same message in both modes.
[GenerateGenericTest(typeof(int))]
public class ArityAndConstraintOrderTests<TClass>
{
    [Test]
    [GenerateGenericTest(typeof(int), typeof(string))]
    [GenerateGenericTest(typeof(int))]
    public void ArityThenConstraint<TMethod>() where TMethod : class
    {
    }
}

// The failure does not repeat for each class row.
[Arguments(1)]
[Arguments(2)]
public class ClassRowsGenerateGenericTestArityTests<TClass>(TClass value)
{
    public TClass Value => value;

    [Test]
    [GenerateGenericTest(typeof(string))]
    [GenerateGenericTest]
    public void MixedArity<TMethod>()
    {
    }
}

// A non-generic class whose constructor takes the class row's value.
[Arguments(1)]
public class ConstructorArgumentGenerateGenericTestArityTests(int value)
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest]
    public async Task MixedArity<T>()
        => await Assert.That(value).IsEqualTo(1);
}

// A null type argument fails on its own, and it counts towards the arity, so a list with a null in it never
// passes as one of the right size.
public class NullTypeArgumentGenerateGenericTestTests
{
    [Test]
    [GenerateGenericTest((Type)null!)]
    public void NullTypeArgument<T>()
    {
    }

    [Test]
    [GenerateGenericTest(typeof(int), null!)]
    public void NullBesideType<T>()
    {
    }
}

// On a generic class too, a data source whose declared type (object) breaks the method's constraints defers to the
// method-level [GenerateGenericTest]: only the arity failure is reported, as in reflection mode.
[Arguments(1)]
public class BroadSourceOnGenericClassRowTests<TClass>(TClass value)
{
    public TClass Value => value;

    [Test]
    [GenerateGenericTest]
    [ObjectValues]
    public void EmptyGenerateGenericTest<TMethod>(TMethod item) where TMethod : struct
    {
    }
}

[GenerateGenericTest(typeof(int))]
public class BroadSourceOnGenericClassTypeTests<TClass>
{
    [Test]
    [GenerateGenericTest]
    [ObjectValues]
    public void EmptyGenerateGenericTest<TMethod>(TMethod item) where TMethod : struct
    {
    }
}

// A deferred broad source joins the instantiation that a method [Arguments] row already created on a class
// row: both rows run on BroadSourceJoinsClassRowTests<int>.Test<int>, as in reflection mode.
[Arguments(1)]
public class BroadSourceJoinsClassRowTests<TClass>(TClass value)
{
    public TClass Value => value;

    [Test]
    [Arguments(1)]
    [GenerateGenericTest(typeof(int))]
    [ObjectValues]
    public void Test<TMethod>(TMethod item) where TMethod : struct
    {
    }
}

public class ObjectValuesAttribute : DataSourceGeneratorAttribute<object>
{
    protected override IEnumerable<Func<object>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => 1;
    }
}
