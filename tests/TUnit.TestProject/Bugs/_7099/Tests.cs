using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7099;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7099: a trailing <c>params T[]</c>
/// on a generic test method failed to compile in source-gen mode (CS0246, CS1503).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericParamsArrayTests
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(1, 2, 3)]
    public async Task Explicit_Many<T>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 1, 2, 3 });
    }

    [Test]
    [GenerateGenericTest(typeof(string))]
    [Arguments("a")]
    public async Task Explicit_One_Reference<T>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { "a" });
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments]
    public async Task Explicit_Zero<T>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values).IsEmpty();
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments("label", 1, 2)]
    public async Task Explicit_Leading<T>(string label, params T[] values)
    {
        await Assert.That(label).IsEqualTo("label");
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 1, 2 });
    }

    [Test]
    [GenerateGenericTest(typeof(string))]
    [Arguments("a", "b", "c")]
    public async Task Explicit_Many_Reference<T>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { "a", "b", "c" });
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(new[] { 4, 5 })]
    public async Task Explicit_ArrayArgument<T>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 4, 5 });
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(1, 2)]
    public async Task Explicit_PlainArray<T>(T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 1, 2 });
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class GenericClassParamsArrayTests<T>
{
    [Test]
    [Arguments(1, 2)]
    public async Task ClassTypeParameter(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 1, 2 });
    }
}

/// <summary>
/// Instantiations inferred from a leading argument. Source-gen mode binds these; reflection mode
/// still fails to resolve T because it matches every row value against the declared parameter
/// types (see https://github.com/thomhurst/TUnit/issues/7099), so these are not engine-tested yet.
/// </summary>
public class InferredGenericParamsArrayTests
{
    [Test]
    [Arguments(1, 2, 3)]
    public async Task Inferred_FromLeading<T>(T first, params T[] rest)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(first).IsEqualTo((T)(object)1);
        await Assert.That(rest.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 2, 3 });
    }

    [Test]
    [Arguments("a", "b")]
    public async Task Inferred_FromLeading_Reference<T>(T first, params T[] rest)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(first).IsEqualTo((T)(object)"a");
        await Assert.That(rest.Cast<object>().ToArray()).IsEquivalentTo(new object[] { "b" });
    }
}
