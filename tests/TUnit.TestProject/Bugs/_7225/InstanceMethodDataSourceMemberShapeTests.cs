using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7225;

/// <summary>
/// [InstanceMethodDataSource] read every member by reflection in source-gen mode before #7225, so its generated
/// factory must keep that behavior: an <c>object</c> value holding many rows supplies each row, a private method
/// is still callable (even beside a generic overload of the same name), a single tuple parameter (value or reference tuple)
/// still receives the whole tuple (a null or omitted optional tuple argument stays null), and a null value (including a null Task or IAsyncEnumerable) is an empty source, so SkipIfEmpty skips the test.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class InstanceMethodDataSourceMemberShapeTests
{
    public object ObjectProperty => new[] { 61, 62 };

    public object ObjectMethod() => new[] { 63, 64 };

    public IEnumerable<(int, int)> TupleRows => [(1, 2), (3, 4)];

    public IEnumerable<Tuple<int, int>> ReferenceTupleRows => [Tuple.Create(1, 2), Tuple.Create(3, 4)];

    public static IEnumerable<Tuple<int, int>> StaticReferenceTupleRows() => [Tuple.Create(5, 6)];

    public IEnumerable<Tuple<int>> OneElementReferenceTupleRows => [Tuple.Create(7)];

    public IEnumerable<Tuple<string?>> OneElementReferenceTupleOfNullRows => [Tuple.Create<string?>(null)];

    public static IEnumerable<Tuple<string?>> StaticOneElementReferenceTupleOfNullRows() => [Tuple.Create<string?>(null)];

    private IEnumerable<int> PrivateRows() => [65, 66];

    private IEnumerable<int> OverloadedRows() => [67, 68];

    private IEnumerable<T> OverloadedRows<T>() => [];

    public object? NullRows => null;

    public IAsyncEnumerable<int> NullAsyncRows => null!;

    public Task<IEnumerable<int>> NullTaskRows() => null!;

    [Test]
    [InstanceMethodDataSource(nameof(ObjectProperty))]
    public async Task Object_Property(int value)
    {
        await Assert.That(value).IsBetween(61, 62);
    }

    [Test]
    [InstanceMethodDataSource(nameof(ObjectMethod))]
    public async Task Object_Method(int value)
    {
        await Assert.That(value).IsBetween(63, 64);
    }

    [Test]
    [InstanceMethodDataSource(nameof(TupleRows))]
    public async Task Tuple_Single_Parameter((int, int) value)
    {
        await Assert.That(value.Item2 - value.Item1).IsEqualTo(1);
    }

    [Test]
    [InstanceMethodDataSource(nameof(ReferenceTupleRows))]
    public async Task Reference_Tuple_Single_Parameter(Tuple<int, int> value)
    {
        await Assert.That(value.Item2 - value.Item1).IsEqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(StaticReferenceTupleRows))]
    public async Task Static_Reference_Tuple_Single_Parameter(Tuple<int, int> value)
    {
        await Assert.That(value.Item2 - value.Item1).IsEqualTo(1);
    }

    [Test]
    [InstanceMethodDataSource(nameof(OneElementReferenceTupleRows))]
    public async Task One_Element_Reference_Tuple_Single_Parameter(Tuple<int> value)
    {
        await Assert.That(value.Item1).IsEqualTo(7);
    }

    [Test]
    [InstanceMethodDataSource(nameof(OneElementReferenceTupleOfNullRows))]
    public async Task One_Element_Reference_Tuple_Holding_Null_Stays_A_Tuple(Tuple<string?>? value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value!.Item1).IsNull();
    }

    [Test]
    [InstanceMethodDataSource(nameof(StaticOneElementReferenceTupleOfNullRows))]
    public async Task Static_One_Element_Reference_Tuple_Holding_Null_Stays_A_Tuple(Tuple<string?>? value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value!.Item1).IsNull();
    }

    [Test]
    [Arguments(null)]
    public async Task One_Element_Reference_Tuple_Null_Argument_Stays_Null(Tuple<string?>? value)
    {
        await Assert.That(value).IsNull();
    }

    [Test]
    [Arguments]
    public async Task Optional_Reference_Tuple_Gets_Its_Default(Tuple<int, int>? value = null)
    {
        await Assert.That(value).IsNull();
    }

    [Test]
    [InstanceMethodDataSource(nameof(PrivateRows))]
    public async Task Private_Method(int value)
    {
        await Assert.That(value).IsBetween(65, 66);
    }

    [Test]
    [InstanceMethodDataSource(nameof(OverloadedRows))]
    public async Task Private_Method_With_Generic_Overload(int value)
    {
        await Assert.That(value).IsBetween(67, 68);
    }

    [Test]
    [InstanceMethodDataSource(nameof(NullRows), SkipIfEmpty = true)]
    public void Null_Source_With_SkipIfEmpty_Is_Skipped(int value)
    {
        throw new InvalidOperationException($"Should have been skipped, but ran with '{value}'");
    }

    [Test]
    [InstanceMethodDataSource(nameof(NullAsyncRows), SkipIfEmpty = true)]
    public void Null_Async_Source_With_SkipIfEmpty_Is_Skipped(int value)
    {
        throw new InvalidOperationException($"Should have been skipped, but ran with '{value}'");
    }

    [Test]
    [InstanceMethodDataSource(nameof(NullTaskRows), SkipIfEmpty = true)]
    public void Null_Task_Source_With_SkipIfEmpty_Is_Skipped(int value)
    {
        throw new InvalidOperationException($"Should have been skipped, but ran with '{value}'");
    }
}
