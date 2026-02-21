namespace TUnit.Mock.Tests;

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
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");
        svc.GetValue("key2");
        svc.Process(42);

        await Assert.That(mock.Invocations.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Invocations_Contains_Correct_Method_Names()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");
        svc.Process(99);

        await Assert.That(mock.Invocations[0].MemberName).IsEqualTo("GetValue");
        await Assert.That(mock.Invocations[1].MemberName).IsEqualTo("Process");
    }

    [Test]
    public async Task Invocations_Contains_Correct_Arguments()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("hello");

        await Assert.That(mock.Invocations[0].Arguments[0]).IsEqualTo("hello");
    }

    [Test]
    public async Task Invocations_Is_Empty_When_No_Calls_Made()
    {
        var mock = Mock.Of<IService>();
        await Assert.That(mock.Invocations.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Invocations_Is_Empty_After_Reset()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");

        mock.Reset();

        await Assert.That(mock.Invocations.Count).IsEqualTo(0);
    }
}
