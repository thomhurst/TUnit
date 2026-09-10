using System.Threading.Tasks;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Regression coverage for [GenerateAssertion] on a generic source method whose receiver is a
/// concrete non-sealed reference type. The generator previously prepended a covariant
/// receiver-type parameter, producing a two-type-parameter extension that no call site
/// supplying one explicit type argument (e.g. <c>.HasItem&lt;int&gt;(42)</c>) could bind to,
/// because C# does not permit partial type-argument specification. Control cases isolate the
/// other variables (sealed receiver, non-generic source, full inference); additional cases
/// cover multi-parameter generics, interface receivers, async-result returns, constrained
/// type parameters, and the documented upcast workaround for a more-derived static receiver.
/// </summary>
public class GenerateAssertionGenericMethodOnNonSealedReceiverTests
{
    [Test]
    public async Task GenericMethod_OnNonSealedReceiver_WithExplicitTypeArg()
    {
        var container = new NonSealedContainer();
        await Assert.That(container).HasItem<int>(42);
    }

    [Test]
    public async Task GenericMethod_OnSealedReceiver_WithExplicitTypeArg()
    {
        var container = new SealedContainer();
        await Assert.That(container).HasItemSealed<int>(42);
    }

    [Test]
    public async Task NonGenericMethod_OnNonSealedReceiver()
    {
        var container = new NonSealedContainer();
        await Assert.That(container).HasInt(42);
    }

    [Test]
    public async Task GenericMethod_OnNonSealedReceiver_WithFullInference()
    {
        var container = new NonSealedContainer();
        await Assert.That(container).HasItem(42);
    }

    [Test]
    public async Task GenericMethod_WithMultipleTypeParameters_WithExplicitTypeArgs()
    {
        var container = new NonSealedContainer();
        await Assert.That(container).HasPair<int, string>(42, "x");
    }

    [Test]
    public async Task GenericMethod_OnInterfaceReceiver_WithExplicitTypeArg()
    {
        IContainerInterface container = new NonSealedContainer();
        await Assert.That(container).HasInterfaceItem<int>(42);
    }

    [Test]
    public async Task GenericMethod_WithConstraint_OnNonSealedReceiver_WithExplicitTypeArg()
    {
        var container = new NonSealedContainer();
        await Assert.That(container).HasParsable<int>("42");
    }

    [Test]
    public async Task GenericMethod_AsyncResult_OnNonSealedReceiver_WithExplicitTypeArg()
    {
        var container = new NonSealedContainer();
        await Assert.That(container).HasItemAsync<int>(42);
    }

    [Test]
    public async Task GenericMethod_DerivedStaticReceiver_UpcastWorkaround()
    {
        var derived = new DerivedNonSealedContainer();
        await Assert.That((NonSealedContainer)derived).HasItem<int>(42);
    }
}

public class NonSealedContainer : IContainerInterface
{
}

public class DerivedNonSealedContainer : NonSealedContainer
{
}

public sealed class SealedContainer
{
}

public interface IContainerInterface
{
}

public static partial class GenerateAssertionGenericMethodOnNonSealedReceiverTestExtensions
{
    [GenerateAssertion]
    public static bool HasItem<T>(this NonSealedContainer container, T item) => true;

    [GenerateAssertion]
    public static bool HasItemSealed<T>(this SealedContainer container, T item) => true;

    [GenerateAssertion]
    public static bool HasInt(this NonSealedContainer container, int item) => true;

    [GenerateAssertion]
    public static bool HasPair<TFirst, TSecond>(this NonSealedContainer container, TFirst first, TSecond second) => true;

    [GenerateAssertion]
    public static bool HasInterfaceItem<T>(this IContainerInterface container, T item) => true;

    [GenerateAssertion]
    public static bool HasParsable<T>(this NonSealedContainer container, string text)
        where T : IParsable<T>
        => T.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out _);

    [GenerateAssertion]
    public static Task<AssertionResult> HasItemAsync<T>(this NonSealedContainer container, T item)
        => Task.FromResult(AssertionResult.Passed);
}
