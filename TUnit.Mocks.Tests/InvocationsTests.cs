namespace TUnit.Mocks.Tests;

public class InvocationsTests
{
    public interface IService
    {
        string GetValue(string key);
        void Process(int id);
    }

    [Test]
    public async Task Invocations_Returns_All_Calls()
    {
        var mock = IService.Mock();
        mock.GetValue(Any()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");
        svc.GetValue("key2");
        svc.Process(42);

        await Assert.That(mock.Invocations.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Invocations_Contains_Correct_Method_Names()
    {
        var mock = IService.Mock();
        mock.GetValue(Any()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");
        svc.Process(99);

        var invocations = mock.Invocations;
        await Assert.That(invocations[0].MemberName).IsEqualTo("GetValue");
        await Assert.That(invocations[1].MemberName).IsEqualTo("Process");
    }

    [Test]
    public async Task Invocations_Contains_Correct_Arguments()
    {
        var mock = IService.Mock();
        mock.GetValue(Any()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("hello");

        await Assert.That(mock.Invocations[0].Arguments[0]).IsEqualTo("hello");
    }

    [Test]
    public async Task Invocations_Is_Empty_When_No_Calls_Made()
    {
        var mock = IService.Mock();
        await Assert.That(mock.Invocations.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Invocations_Is_Empty_After_Reset()
    {
        var mock = IService.Mock();
        mock.GetValue(Any()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");

        mock.Reset();

        await Assert.That(mock.Invocations.Count).IsEqualTo(0);
    }
}
