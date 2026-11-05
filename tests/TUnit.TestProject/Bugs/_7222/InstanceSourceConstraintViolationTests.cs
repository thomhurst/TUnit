namespace TUnit.TestProject.Bugs._7222;

// An instance data source whose rows break the method's constraints still reads them from a test class instance,
// so the failing entry keeps the class data sources that build it. Each row fails with the constraint message in
// both modes, not with a constructor error in source-generated mode.
//
// Not marked [EngineTest]: the failures are intentional. TUnit.Engine.Tests (Issue7222Tests) checks them.
[Arguments(1)]
public class InstanceSourceConstraintViolationTests(int value)
{
    public IEnumerable<string> Rows()
    {
        yield return value.ToString();
        yield return "b";
    }

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void StructOnly<T>(T item) where T : struct
    {
    }
}

// The int class closes T? successfully; the string class must report its rows rather than disappear.
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(string))]
public class ClosedClassUninferredInstanceSourceTests<TClass>
{
    public IEnumerable<TClass> Rows() => [default!];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void NullableElement<T>(T? row) where T : struct { }
}

[Arguments(1)]
[Arguments("value")]
public class ClosedClassRowUninferredInstanceSourceTests<TClass>(TClass value)
{
    public IEnumerable<TClass> Rows() => [value];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void NullableElement<T>(T? row) where T : struct { }
}

// Parameter inference already closes the method and runs the weakly typed source.
[Arguments(1)]
public class ClosedClassParameterInferredSourceTests<TClass>(TClass value)
{
    public IEnumerable<object> Rows() => [1];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public async Task ParameterInferred<T>([Matrix<int>(1)] T item)
    {
        await Assert.That(item).IsEqualTo((T)(object)1);
        await Assert.That(value is int).IsTrue();
    }
}

[GenerateGenericTest(typeof(int))]
public class ExplicitClassParameterInferredSourceTests<TClass>
{
    public IEnumerable<object> Rows() => [1];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public async Task ParameterInferred<T>([Matrix<int>(1)] T item)
    {
        await Assert.That(item).IsEqualTo((T)(object)1);
    }
}
