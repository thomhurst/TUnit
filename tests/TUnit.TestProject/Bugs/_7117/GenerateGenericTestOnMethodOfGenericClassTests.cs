using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7117;

// [GenerateGenericTest] on a generic method of a generic class closes the method on every class the
// class sources produce, in both modes (#7117).

// The issue's shape: the class row and the method row infer int, which [GenerateGenericTest] also lists.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class MethodGenerateGenericTestOnClassRow<T>(T value)
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(2)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(arg).IsEqualTo((TMethod)(object)2);
    }
}

// Each class row closes the class; the method's [GenerateGenericTest] type runs on each of them.
[EngineTest(ExpectedResult.Pass)]
[Arguments("a")]
[Arguments(2)]
public class MethodGenerateGenericTestWithClassRows<T>(T value)
{
    [Test]
    [GenerateGenericTest(typeof(bool))]
    public async Task Run<TMethod>()
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(bool));
        await Assert.That(value is "a" or 2).IsTrue();
        await Assert.That(typeof(T)).IsEqualTo(value!.GetType());
    }
}

// A class-level [GenerateGenericTest] with a method row that infers the method's type argument.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class ClassGenerateGenericTestWithMethodRow<T>
{
    [Test]
    [Arguments(1)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That(arg).IsEqualTo((TMethod)(object)1);
    }
}

// A [GenerateGenericTest] type that no method row infers runs every method row, as on a non-generic
// class: Run<int>(2) from the row and Run<long>(2) from the attribute.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class MethodGenerateGenericTestTypeNotInferredByRow<T>(T value)
{
    [Test]
    [GenerateGenericTest(typeof(long))]
    [Arguments(2)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(typeof(TMethod) == typeof(int) || typeof(TMethod) == typeof(long)).IsTrue();
        await Assert.That(Convert.ToInt64(arg)).IsEqualTo(2L);
    }
}
