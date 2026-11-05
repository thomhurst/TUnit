using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

public class Issue7657Tests
{
    public interface IService
    {
        int Send(string @mock, string mock_, string @matchers, string matchers_);
        int Engine(int __engine);
        int Pack(int __i, int __paramsElementMatchers, params string[] matchers);
        int this[string mock, string matchers] { get; set; }
    }

    public interface IGeneric<T>
    {
        U Map<U>(T mock, U matchers);
    }

    public interface IPrimary { }

    [Test]
    public async Task Setup_Identifiers_Do_Not_Collide_With_Parameters()
    {
        var mock = Mock.Of<IService>();
        mock.Send("a", "b", "c", "d").Returns(1);
        mock.Send(s => s == "e", "f", "g", "h").Returns(2);
        mock.Pack(1, 2, Arg.Is("x"), Arg.Is("y")).Returns(3);
        mock.Item("a", "b").Returns(4);
        mock.SetItem("a", "b", 5);

        await Assert.That(mock.Object.Send("a", "b", "c", "d")).IsEqualTo(1);
        await Assert.That(mock.Object.Send("e", "f", "g", "h")).IsEqualTo(2);
        await Assert.That(mock.Object.Pack(1, 2, "x", "y")).IsEqualTo(3);
        await Assert.That(mock.Object["a", "b"]).IsEqualTo(4);
        mock.Object["a", "b"] = 5;
        mock.SetItem("a", "b", 5).WasCalled(Times.Once);
    }

    [Test]
    public async Task Generic_And_Secondary_Setups_Use_The_Allocated_Receiver()
    {
        var generic = Mock.Of<IGeneric<int>>();
        generic.Map<string>(1, "a").Returns("result");
        await Assert.That(generic.Object.Map(1, "a")).IsEqualTo("result");

        var combo = Mock.Of<IPrimary, IService>();
        combo.Send("a", "b", "c", "d").Returns(5);
        combo.Engine(7).Returns(8);
        await Assert.That(((IService)combo.Object).Engine(7)).IsEqualTo(8);
        await Assert.That(((IService)combo.Object).Send("a", "b", "c", "d")).IsEqualTo(5);
    }
}
