using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

public class InvoiceAct { }

public interface IInvoiceActService
{
    Task<(T? act, string error)> ActFromInvoice<T>() where T : InvoiceAct, new();
    T? Find<T>() where T : InvoiceAct;
    T?[] FindAll<T>() where T : InvoiceAct?;
    void Accept<T>(T? value) where T : InvoiceAct;
}

public class Issue7820Tests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Nullable_Tuple_Return_Is_Set_Up_And_Verified(bool returnNull)
    {
        var mock = IInvoiceActService.Mock();
        InvoiceAct? expected = returnNull ? null : new InvoiceAct();
        mock.ActFromInvoice<InvoiceAct>().Returns((expected, "result"));

        // The wrapper and its Object each implement the interface through different forwards.
        IInvoiceActService wrapper = mock;
        var wrapperResult = await wrapper.ActFromInvoice<InvoiceAct>();
        var objectResult = await mock.Object.ActFromInvoice<InvoiceAct>();

        await Assert.That(wrapperResult.act).IsEqualTo(expected);
        await Assert.That(wrapperResult.error).IsEqualTo("result");
        await Assert.That(objectResult.act).IsEqualTo(expected);
        await Assert.That(objectResult.error).IsEqualTo("result");
        mock.ActFromInvoice<InvoiceAct>().WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Nullable_Return_And_Parameter_Are_Set_Up_And_Verified()
    {
        var mock = IInvoiceActService.Mock();
        var callbackCount = 0;
        mock.Find<InvoiceAct>().Returns((InvoiceAct?)null);
        mock.Accept<InvoiceAct>(null).Callback(() => callbackCount++);

        IInvoiceActService wrapper = mock;
        var result = wrapper.Find<InvoiceAct>();
        wrapper.Accept<InvoiceAct>(result);
        mock.Object.Accept<InvoiceAct>(result);

        await Assert.That(result).IsNull();
        await Assert.That(callbackCount).IsEqualTo(2);
        mock.Find<InvoiceAct>().WasCalled(Times.Once);
        mock.Accept<InvoiceAct>(null).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Nullable_Array_Allows_A_Nullable_Base_Class_Type_Argument()
    {
        var mock = IInvoiceActService.Mock();
        InvoiceAct?[] expected = [new InvoiceAct(), null];
        mock.FindAll<InvoiceAct?>().Returns(expected);

        IInvoiceActService wrapper = mock;
        var wrapperResult = wrapper.FindAll<InvoiceAct?>();
        var objectResult = mock.Object.FindAll<InvoiceAct?>();

        await Assert.That(wrapperResult).IsSameReferenceAs(expected);
        await Assert.That(objectResult).IsSameReferenceAs(expected);
        mock.FindAll<InvoiceAct?>().WasCalled(Times.Exactly(2));
    }
}
