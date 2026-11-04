using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7224Unsupported;

[EngineTest(ExpectedResult.Failure)]
public class UninferableMethodSourceClassTests<T>
{
    public static IEnumerable<object> Rows() => [1];

    [Test, MethodDataSource(nameof(Rows))]
    public void CannotInfer(T value) { }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class ExplicitMethodSourceClassTests<T>
{
    public static IEnumerable<object> Rows() => [1];

    [Test, MethodDataSource(nameof(Rows))]
    public async Task ExplicitTypes(T value) => await Assert.That(value).IsEqualTo((T)(object)1);
}

[EngineTest(ExpectedResult.Failure)]
public class ConstrainedMethodSourceClassTests<T> where T : class
{
    public static IEnumerable<int> Rows() => [1];

    [Test, MethodDataSource(nameof(Rows))]
    public void InvalidClassType(T value) { }
}

public interface IInferenceMarker<T>;
public class AmbiguousInferenceRow : IInferenceMarker<int>, IInferenceMarker<string>;
public class ReversedAmbiguousInferenceRow : IInferenceMarker<string>, IInferenceMarker<int>;

[EngineTest(ExpectedResult.Failure)]
#pragma warning disable TUnit0001 // These incompatible sources deliberately exercise discovery failures.
public class AmbiguousMethodSourceClassTests<T>
{
    public static IEnumerable<AmbiguousInferenceRow> Rows() => [new()];
    public static IEnumerable<ReversedAmbiguousInferenceRow> ReversedRows() => [new()];

    [Test, MethodDataSource(nameof(Rows))]
    public void CannotInfer(IInferenceMarker<T> value) { }

    [Test, MethodDataSource(nameof(ReversedRows))]
    public void CannotInferReversed(IInferenceMarker<T> value) { }
}
