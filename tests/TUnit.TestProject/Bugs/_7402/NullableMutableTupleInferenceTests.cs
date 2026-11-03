using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7402;

[EngineTest(ExpectedResult.Pass)]
public class NullableMutableTupleInferenceTests
{
    public static (Fixture?, int) Row() => ((Fixture?)new Fixture(), 1);
    public static Tuple<Fixture?, int> ReferenceRow() => Tuple.Create<Fixture?, int>(new Fixture(), 1);

    [Test, MethodDataSource(nameof(Row))]
    public async Task Nullable_Value_Element_Infers_Underlying_Type<T>((T?, int) row) where T : struct
        => await Check(row.Item1, row.Item2);

    [Test, MethodDataSource(nameof(ReferenceRow))]
    public async Task Nullable_Reference_Element_Infers_Underlying_Type<T>(Tuple<T?, int> row) where T : struct
        => await Check(row.Item1, row.Item2);

    private static async Task Check<T>(T? value, int number) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(Fixture));
        await Assert.That(((Fixture)(object)value!.Value).Initialized).IsTrue();
        await Assert.That(number).IsEqualTo(1);
    }

    public struct Fixture : IAsyncInitializer
    {
        public bool Initialized;
        public Task InitializeAsync()
        {
            Initialized = true;
            return Task.CompletedTask;
        }
    }
}
