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
/// Instantiations inferred from the row values. T is inferred from a leading T argument or from a
/// lone array value; loose values collected by the params array are not used for inference yet.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
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

    [Test]
    [Arguments(new[] { 4, 5 })]
    public async Task Inferred_FromArrayArgument<T>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 4, 5 });
    }

    [Test]
    [Arguments(new[] { 4, 5 })]
    public async Task Inferred_FromPlainArrayArgument<T>(T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 4, 5 });
    }

    [Test]
    [Arguments(new[] { 4, 5 })]
    public async Task Inferred_FromPlainArrayArgument_With_Token<T>(T[] values, CancellationToken token)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 4, 5 });
    }

    [Test]
    [Arguments(new[] { 1 }, new[] { 2 })]
    public async Task Inferred_ArrayElements<T>(T first, params T[] rest)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int[]));
        await Assert.That(rest.Length).IsEqualTo(1);
        await Assert.That(((int[])(object)rest[0]!)[0]).IsEqualTo(2);
    }

    [Test]
    [Arguments(1, 2, 3)]
    public async Task Inferred_FromLeading_PlainArray<T>(T first, T[] rest)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(rest.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 2, 3 });
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenericParamsArrayEdgeCaseTests
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(1, 2)]
    public async Task Optional_Before_Params<T>(int count = 0, params T[] values)
    {
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 2 });
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments]
    public async Task Optional_Omitted_Before_Params<T>(int count = 42, params T[] values)
    {
        await Assert.That(count).IsEqualTo(42);
        await Assert.That(values).IsEmpty();
    }

    [Test]
    [Arguments]
    public async Task NonGeneric_Optional_Omitted_Before_Params(int count = 42, params int[] values)
    {
        await Assert.That(count).IsEqualTo(42);
        await Assert.That(values).IsEmpty();
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments]
    public async Task Optional_Omitted_Before_Array_With_Token<T>(int count = 42, T[]? values = null, CancellationToken token = default)
    {
        await Assert.That(count).IsEqualTo(42);
        // Both modes apply the declared default of the omitted optional array.
        await Assert.That(values).IsNull();
    }

    [Test]
    [Arguments]
    public async Task NonGeneric_Optional_Omitted_Before_Array_With_Token(int count = 42, int[]? values = null, CancellationToken token = default)
    {
        await Assert.That(count).IsEqualTo(42);
        // Both modes apply the declared default of the omitted optional array.
        await Assert.That(values).IsNull();
    }

    [Test]
    [Arguments(1)]
    public async Task Optional_Omitted_Without_Params(int first, int count = 42)
    {
        await Assert.That(first).IsEqualTo(1);
        await Assert.That(count).IsEqualTo(42);
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(Grids))]
    public async Task MultiDimensional<T>(T[,] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Length).IsEqualTo(4);
    }

    public static IEnumerable<Func<int[,]>> Grids() => [() => new[,] { { 1, 2 }, { 3, 4 } }];

    [Test]
    [Arguments(1)]
    public async Task Optional_Array_Omitted_After_Required_With_Token(int first, int[]? values = null, CancellationToken token = default)
    {
        await Assert.That(first).IsEqualTo(1);
        await Assert.That(values).IsNull();
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(1, 2)]
    public async Task PlainArray_Before_Token<T>(T[] values, CancellationToken token)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 1, 2 });
    }

    [Test]
    [Arguments(1, 2)]
    public async Task NonGeneric_PlainArray_Before_Token(int[] values, CancellationToken token)
    {
        await Assert.That(values).IsEquivalentTo(new[] { 1, 2 });
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(GridRows))]
    public async Task JaggedMultiDimensional<T>(params int[][,] values)
    {
        await Assert.That(values.Length).IsEqualTo(2);
        await Assert.That(values[1][0, 0]).IsEqualTo(5);
    }

    public static IEnumerable<Func<int[][,]>> GridRows() => [() => [new[,] { { 1 } }, new[,] { { 5 } }]];
}

public abstract class GenericParamsArrayBase<T>
{
    [Test]
    [GenerateGenericTest(typeof(string))]
    [Arguments(1, 2)]
    public async Task Inherited<U>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(typeof(U)).IsEqualTo(typeof(string));
        await Assert.That(values.Cast<object>().ToArray()).IsEquivalentTo(new object[] { 1, 2 });
    }
}

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class GenericParamsArrayDerived : GenericParamsArrayBase<int>;

/// <summary>
/// An omitted optional array must still bind to the test method, not to an overload without it.
/// The overloads throw, so a call that binds to them fails the test.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class OptionalArrayOverloadTests
{
    [Test]
    [Arguments]
    public async Task NonGeneric_Optional_Array(int[]? values = null)
    {
        await Assert.That(values).IsNull();
    }

    // internal: visible to the generated invoker, so it competes in overload resolution.
    internal Task NonGeneric_Optional_Array() => throw new InvalidOperationException("Bound to the parameterless overload");

    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments]
    public async Task Generic_Optional_Array<T>(T[]? values = null)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values).IsNull();
    }

    internal Task Generic_Optional_Array<T>() => throw new InvalidOperationException("Bound to the parameterless overload");

    [Test]
    [Arguments]
    public async Task Optional_Array_With_Token(int[]? values = null, CancellationToken token = default)
    {
        await Assert.That(values).IsNull();
    }

    internal Task Optional_Array_With_Token(CancellationToken token) => throw new InvalidOperationException("Bound to the token-only overload");
}
