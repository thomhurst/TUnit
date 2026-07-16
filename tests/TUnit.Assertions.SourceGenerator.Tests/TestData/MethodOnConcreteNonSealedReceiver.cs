using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: generic [GenerateAssertion] method on a concrete non-sealed receiver.
/// Generated extension must declare a single type parameter (T) targeting the exact
/// receiver type, not a two-parameter <c>&lt;TActual, T&gt;</c> covariant shape.
/// </summary>
public class MethodOnConcreteNonSealedReceiver
{
}

public static partial class MethodOnConcreteNonSealedReceiverExtensions
{
    [GenerateAssertion]
    public static bool HasItem<T>(this MethodOnConcreteNonSealedReceiver receiver, T item) => true;
}
