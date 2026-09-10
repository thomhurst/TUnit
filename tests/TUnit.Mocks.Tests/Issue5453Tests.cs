namespace TUnit.Mocks.Tests;

// Compile-time regression test for https://github.com/thomhurst/TUnit/issues/5453
// CS9338 / CS0051: a public generic interface closed over an internal type argument
// (e.g. ILogger<InternalClass>) used to emit a `public` mock wrapper, leaking the
// internal type through the wrapper's base signature.
public class Issue5453Tests
{
    internal sealed class InternalConsumer
    {
    }

    // `partial` doesn't affect DeclaredAccessibility today, but the original report's class
    // was `internal sealed partial` (for [LoggerMessage]) — kept as a distinct test so a
    // future regression in partial-symbol handling is attributable.
    internal sealed partial class InternalPartialConsumer
    {
    }

    public interface IPublicGenericProcessor<T>
    {
        void Process(T value);
        T Get();
        Task<T> GetAsync();
    }

    [Test]
    public async Task Can_Mock_Public_Generic_Interface_With_Internal_Type_Argument()
    {
        var mock = IPublicGenericProcessor<InternalConsumer>.Mock(MockBehavior.Loose);
        var instance = new InternalConsumer();

        mock.Process(Arg.Any<InternalConsumer>()).Returns();
        mock.Get().Returns(instance);

        mock.Object.Process(instance);
        var result = mock.Object.Get();

        await Assert.That(result).IsSameReferenceAs(instance);
    }

    [Test]
    public async Task Can_Mock_Public_Generic_Interface_With_Internal_Partial_Type_Argument()
    {
        var mock = IPublicGenericProcessor<InternalPartialConsumer>.Mock(MockBehavior.Loose);
        var instance = new InternalPartialConsumer();

        mock.Get().Returns(instance);

        var result = mock.Object.Get();

        await Assert.That(result).IsSameReferenceAs(instance);
    }

    // Async path goes through a different generated extension overload (Task<T> Returns) than
    // the sync Get() above; both must be emitted with the correct visibility.
    [Test]
    public async Task Can_Configure_Method_Overload_With_Internal_Type_Argument()
    {
        var mock = IPublicGenericProcessor<InternalConsumer>.Mock(MockBehavior.Loose);
        var instance = new InternalConsumer();

        mock.GetAsync().Returns(instance);

        var result = await mock.Object.GetAsync();

        await Assert.That(result).IsSameReferenceAs(instance);
    }
}
