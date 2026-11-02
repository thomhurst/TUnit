using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7214;

// A typed [Arguments<T>] row that infers the same method type arguments as another row still runs, in both
// modes (#7214). [Arguments<T>] is a typed data source, not an [Arguments] row, although its symbol name
// is also "ArgumentsAttribute".

// An [Arguments] row and an [Arguments<int>] row both infer Run<int>: Run<int>(1) and Run<int>(2).
[EngineTest(ExpectedResult.Pass)]
public class ArgumentsAndTypedArgumentsRow
{
    [Test]
    [Arguments(1)]
    [Arguments<int>(2)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg! is 1 or 2).IsTrue();
    }
}

// The typed row first: Run<int>(2) and Run<int>(1).
[EngineTest(ExpectedResult.Pass)]
public class TypedArgumentsRowThenArguments
{
    [Test]
    [Arguments<int>(2)]
    [Arguments(1)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg! is 1 or 2).IsTrue();
    }
}

// Two typed rows inferring the same type: Run<int>(1) and Run<int>(2).
[EngineTest(ExpectedResult.Pass)]
public class TwoTypedArgumentsRows
{
    [Test]
    [Arguments<int>(1)]
    [Arguments<int>(2)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg! is 1 or 2).IsTrue();
    }
}

// The same on a generic class closed by [GenerateGenericTest]: Run<int>(1) and Run<int>(2).
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class ArgumentsAndTypedArgumentsRowOnGenericClass<T>
{
    [Test]
    [Arguments(1)]
    [Arguments<int>(2)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg! is 1 or 2).IsTrue();
    }
}

// With a method [GenerateGenericTest] of the same type, the typed row joins the [Arguments] row's
// instantiation instead of being dropped: Run<int>(1) and Run<int>(2).
[EngineTest(ExpectedResult.Pass)]
public class ArgumentsAndTypedArgumentsRowWithGenerateGenericTest
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(1)]
    [Arguments<int>(2)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg! is 1 or 2).IsTrue();
    }
}

// The same with two typed rows: Run<int>(1) and Run<int>(2).
[EngineTest(ExpectedResult.Pass)]
public class TwoTypedArgumentsRowsWithGenerateGenericTest
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments<int>(1)]
    [Arguments<int>(2)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg! is 1 or 2).IsTrue();
    }
}

// A typed row of another type than the [GenerateGenericTest] type runs on its own type: Run<int>(1) and
// Run<long>(2).
[EngineTest(ExpectedResult.Pass)]
public class TypedArgumentsRowOfOtherTypeWithGenerateGenericTest
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(1)]
    [Arguments<long>(2L)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod) == typeof(int) || typeof(TMethod) == typeof(long)).IsTrue();
        await Assert.That(Convert.ToInt64(arg)).IsEqualTo(typeof(TMethod) == typeof(int) ? 1L : 2L);
    }
}

// A typed row is matched against the parameter type, not taken as the type argument whole: an [Arguments<int[]>]
// row on an IEnumerable<T> parameter infers nothing and joins the [GenerateGenericTest] instantiation, so only
// Run<int> runs (no Run<int[]>, whose parameter could not take the row).
[EngineTest(ExpectedResult.Pass)]
public class TypedCollectionRowWithGenerateGenericTest
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments<int[]>(new[] { 1, 2 })]
    public async Task Run<TMethod>(IEnumerable<TMethod> values)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That(values.Count()).IsEqualTo(2);
    }
}


// The declared row type wins over the value's run-time type in both modes, without [GenerateGenericTest]:
// [Arguments<object>(1)] runs Run<object>(1), not Run<int>.
[EngineTest(ExpectedResult.Pass)]
public class TypedRowDeclaredTypeWinsOverValueType
{
    [Test]
    [Arguments<object>(1)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(object));
        await Assert.That(arg).IsEqualTo((object)1);
    }
}

// A scalar typed row binds to the first data parameter when the rest are optional: Run<int>(1).
[EngineTest(ExpectedResult.Pass)]
public class TypedRowWithOptionalParameter
{
    [Test]
    [Arguments<int>(1)]
    public async Task Run<TMethod>(TMethod arg, int other = 5)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg!).IsEqualTo(1);
        await Assert.That(other).IsEqualTo(5);
    }
}

// A T? parameter takes a row of its underlying type: Run<int>(1).
[EngineTest(ExpectedResult.Pass)]
public class TypedRowOnNullableParameter
{
    [Test]
    [Arguments<int>(1)]
    public async Task Run<TMethod>(TMethod? arg) where TMethod : struct
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That(arg.HasValue).IsTrue();
    }
}

// An omitted C# 13 params collection is optional, as a params array is: [Arguments<object>(1)] runs
// Run<object>(1) in both modes.
[EngineTest(ExpectedResult.Pass)]
public class TypedRowWithParamsCollection
{
    [Test]
    [Arguments<object>(1)]
    public async Task Run<TMethod>(TMethod arg, params IEnumerable<string> rest)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(object));
        await Assert.That(arg).IsEqualTo((object)1);
        await Assert.That(rest).IsEmpty();
    }
}

// A typed source of TestDataRow<int> rows is unwrapped before invocation, so it infers Run<int>, not
// Run<TestDataRow<int>>: Run<int>(1).
[EngineTest(ExpectedResult.Pass)]
public class TypedTestDataRowSource
{
    [Test]
    [IntTestDataRowSource]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg!).IsEqualTo(1);
    }
}

public sealed class IntTestDataRowSourceAttribute : TypedDataSourceAttribute<TestDataRow<int>>
{
    public override async IAsyncEnumerable<Func<Task<TestDataRow<int>>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await Task.CompletedTask;
        yield return () => Task.FromResult(new TestDataRow<int>(1));
    }
}

// A typed source of Func<int> rows is invoked before invocation, so it infers Run<int>, not Run<Func<int>>:
// Run<int>(1).
public class TypedFuncRowSource
{
    [Test]
    [IntFuncRowSource]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg!).IsEqualTo(1);
    }
}

public sealed class IntFuncRowSourceAttribute : TypedDataSourceAttribute<Func<int>>
{
    public override async IAsyncEnumerable<Func<Task<Func<int>>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await Task.CompletedTask;
        yield return () => Task.FromResult<Func<int>>(() => 1);
    }
}

// A Func<int> inside a TestDataRow is invoked after unwrapping, so it infers Run<int> too: Run<int>(1).
public class TypedTestDataRowOfFuncSource
{
    [Test]
    [IntFuncTestDataRowSource]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)arg!).IsEqualTo(1);
    }
}

public sealed class IntFuncTestDataRowSourceAttribute : TypedDataSourceAttribute<TestDataRow<Func<int>>>
{
    public override async IAsyncEnumerable<Func<Task<TestDataRow<Func<int>>>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await Task.CompletedTask;
        yield return () => Task.FromResult(new TestDataRow<Func<int>>(() => 1));
    }
}
