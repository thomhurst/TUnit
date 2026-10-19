namespace TUnit.TestProject.Bugs._7116;

// A generic test method's type arguments are inferred from its data source. Source-generated mode
// (the default, and the only mode under Native AOT) emits a call per type argument at compile time,
// so it infers them from the data source's DECLARED type. Reflection mode infers them from each
// value's run-time type with MakeGenericMethod. A broad declared type (object, an interface, a
// non-sealed class or int?) can therefore break a constraint in source-generated mode that the
// run-time value meets (#7116).
//
// Not marked [EngineTest]: the two modes differ by design for the first two methods, and
// TUnit.Engine.Tests (Issue7116Tests) checks each mode's outcome by name.
public class DeclaredTypeInferenceTests
{
    public static IEnumerable<Func<(object, int)>> Streams() => [() => (new MemoryStream(), 1)];

    public static IEnumerable<Func<(int?, int)>> NullableNumbers() => [() => (1, 1)];

    public static IEnumerable<Func<object>> SingleStreams() => [() => new MemoryStream()];

    // TUnit0001 reports both of these at compile time and explains the declared-type inference.
    // It is suppressed here so that the run-time behavior can be checked.
#pragma warning disable TUnit0001
    [Test]
    [MethodDataSource(nameof(Streams))]
    public async Task ObjectDeclaredType<T>(T value, int marker) where T : IDisposable
        => await Assert.That(value is MemoryStream).IsTrue();

    [Test]
    [MethodDataSource(nameof(NullableNumbers))]
    public async Task NullableDeclaredType<T>(T value, int marker) where T : IComparable
        => await Assert.That(value is int).IsTrue();
#pragma warning restore TUnit0001

    // The documented workaround: [GenerateGenericTest] names the type argument, and the broad values
    // are converted to it. Both modes run exactly this one test.
    [Test]
    [GenerateGenericTest(typeof(MemoryStream))]
    [MethodDataSource(nameof(Streams))]
    public async Task GenerateGenericTestWithObjectRows<T>(T value, int marker) where T : IDisposable
        => await Assert.That(typeof(T)).IsEqualTo(typeof(MemoryStream));

    [Test]
    [GenerateGenericTest(typeof(MemoryStream))]
    [MethodDataSource(nameof(SingleStreams))]
    public async Task GenerateGenericTestWithSingleObjectRows<T>(T value) where T : IDisposable
        => await Assert.That(typeof(T)).IsEqualTo(typeof(MemoryStream));
}
