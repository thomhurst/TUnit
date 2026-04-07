namespace TUnit.Mocks.Tests;

// Reproduction and regression tests for https://github.com/thomhurst/TUnit/issues/5426
// CS9338 / CS0051: Generated mock wrapper and extension classes were always emitted as `public`,
// even when the source interface (or its containing type) was non-public. This produced
// inconsistent-accessibility errors at compile time.
//
// These tests are *compile-time* regressions: if the source generator emits `public` for any of
// the types below, this file fails to compile and the test project will not build.
public class Issue5426Tests
{
    internal interface IInternalDatabaseService
    {
        Task<int> GetOpenOrdersAsync();
        Task UpdateOrderProgressAsync(int orderId, InternalProcessingStatus status);
    }

    internal enum InternalProcessingStatus
    {
        Pending,
        Done,
    }

    internal sealed record InternalOrderId(int Value);

    internal interface IInternalOrderRepository
    {
        InternalOrderId Get(int id);
    }

    [Test]
    public async Task Can_Mock_Internal_Interface_With_Generic_Task_Method()
    {
        var mock = Mock.Of<IInternalDatabaseService>(MockBehavior.Loose);
        mock.GetOpenOrdersAsync().Returns(42);

        var result = await mock.Object.GetOpenOrdersAsync();

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Can_Mock_Internal_Interface_With_Internal_Parameter_Type()
    {
        var mock = Mock.Of<IInternalDatabaseService>(MockBehavior.Loose);
        mock.UpdateOrderProgressAsync(Arg.Any<int>(), Arg.Any<InternalProcessingStatus>())
            .Returns();

        await mock.Object.UpdateOrderProgressAsync(1, InternalProcessingStatus.Done);
    }

    [Test]
    public async Task Can_Mock_Internal_Interface_With_Internal_Return_Type()
    {
        var mock = Mock.Of<IInternalOrderRepository>(MockBehavior.Loose);
        mock.Get(Arg.Any<int>()).Returns(new InternalOrderId(7));

        var result = mock.Object.Get(1);

        await Assert.That(result.Value).IsEqualTo(7);
    }
}
