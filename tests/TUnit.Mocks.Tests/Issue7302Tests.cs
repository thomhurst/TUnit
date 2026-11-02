using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

public interface IMessageInvoker7302<TContext>
{
    TResult Invoke<TResult>(object message);
    TState RoundTrip<TState>(TContext context, TState state) where TState : struct;
}

public interface IConstrainedInvoker7302<T>
{
    TOther Select<TOther>() where TOther : T;
    TOther Pick<TOther>(T? fallback);
    TOther Keep<TOther>(T value);
}

public class Outer7302<TOuter>
{
    public interface IInner<TInner>
    {
        TResult Map<TResult>(TOuter left, TInner right);
    }
}

public struct Point7302
{
    public int X;
}

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7302: generic-method setups
/// with explicit type arguments did not bind on <c>Mock.Of&lt;IGeneric&lt;X&gt;&gt;()</c>, only on
/// the typed wrapper returned by <c>IGeneric&lt;X&gt;.Mock()</c>.
/// </summary>
public class Issue7302Tests
{
    [Test]
    public async Task Explicit_Method_Type_Arguments_Bind_On_Mock_Of_Generic_Interface()
    {
        var mock = Mock.Of<IGenericKitchenSink<GenericPayload, int>>();
        mock.RoundTrip<Guid>(Arg.Any<Guid>()).Returns(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        var result = mock.Object.RoundTrip(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        await Assert.That(result).IsEqualTo(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        mock.RoundTrip<Guid>(Arg.Any<Guid>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task Inferred_Method_Type_Arguments_Still_Bind_On_Mock_Of_Generic_Interface()
    {
        var mock = Mock.Of<IGenericKitchenSink<GenericPayload, int>>();
        mock.RoundTrip(Arg.Any<int>()).Returns(7);

        await Assert.That(mock.Object.RoundTrip(1)).IsEqualTo(7);
    }

    [Test]
    public async Task Return_Only_Method_Type_Parameter_Can_Be_Set_Up_On_Mock_Of()
    {
        var mock = Mock.Of<IMessageInvoker7302<int>>();
        mock.Invoke<string>(Arg.Any<object>()).Returns("handled");
        mock.Invoke<int>(Arg.Any<object>()).Returns(42);

        await Assert.That(mock.Object.Invoke<string>("message")).IsEqualTo("handled");
        await Assert.That(mock.Object.Invoke<int>("message")).IsEqualTo(42);
        mock.Invoke<string>(Arg.Any<object>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task Model_Type_Argument_Is_Closed_In_Explicit_Setup()
    {
        var mock = Mock.Of<IMessageInvoker7302<int>>();
        mock.RoundTrip<long>(5, Arg.Any<long>()).Returns(10L);

        await Assert.That(mock.Object.RoundTrip(5, 1L)).IsEqualTo(10L);
        await Assert.That(mock.Object.RoundTrip(6, 1L)).IsEqualTo(0L);
    }

    [Test]
    public async Task Struct_Type_Argument_Binds_Explicit_Setup()
    {
        var mock = Mock.Of<IMessageInvoker7302<Point7302>>();
        mock.Invoke<Point7302>(Arg.Any<object>()).Returns(new Point7302 { X = 3 });

        await Assert.That(mock.Object.Invoke<Point7302>("message").X).IsEqualTo(3);
    }

    [Test]
    public async Task Nested_Generic_Interface_Binds_Explicit_Setup()
    {
        var mock = Mock.Of<Outer7302<int>.IInner<string>>();
        mock.Map<long>(1, "right").Returns(9L);

        await Assert.That(mock.Object.Map<long>(1, "right")).IsEqualTo(9L);
    }

    [Test]
    public async Task Skipped_Methods_Keep_Inferred_And_Wrapper_Setups()
    {
        // Select and Pick get no closed receiver (an alias would change their meaning), so the
        // explicit form needs the typed wrapper; Keep binds on Mock.Of directly.
        var mock = Mock.Of<IConstrainedInvoker7302<int>>();
        mock.Keep<string>(Arg.Any<int>()).Returns("kept");

        var wrapper = IConstrainedInvoker7302<int>.Mock();
        wrapper.Pick<string>(Arg.Any<int>()).Returns("picked");

        await Assert.That(mock.Object.Keep<string>(1)).IsEqualTo("kept");
        await Assert.That(wrapper.Object.Pick<string>(0)).IsEqualTo("picked");
    }
}
