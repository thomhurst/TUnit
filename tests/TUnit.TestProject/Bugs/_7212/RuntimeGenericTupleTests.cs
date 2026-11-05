namespace TUnit.TestProject.Bugs._7212Runtime;

// Erased sources require runtime inference, which is supported in reflection mode only.
// The engine regression selects this separately from the dual-mode _7212 cases.
// Deliberately omit EngineTest(Pass): generated discovery reports an uninferable
// generic row, so this fixture must not join the generated passing-suite filter.
public class RuntimeGenericTupleTests
{
    public static IEnumerable<object[]> Rows()
        => _7212.MutableTupleWithOptionalParameterTests.ErasedNestedRows();

    [Test, MethodDataSource(nameof(Rows))]
    public async Task Retained_Nested_Tuple_Infers_Original_Type<T>(((_7212.MutableTupleWithOptionalParameterTests.Fixture, T), int) pair)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)pair.Item1.Item2!).IsEqualTo(26);
        await Assert.That(pair.Item1.Item1.Initialized).IsTrue();
        await Assert.That(pair.Item1.Item1.Started).IsTrue();
        await Assert.That(pair.Item1.Item1.Dependency).IsNotNull();
        await Assert.That(pair.Item2).IsEqualTo(27);
    }
}
