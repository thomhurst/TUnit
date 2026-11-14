namespace TUnit.Mocks.Tests;

public interface IPredicateLocalCollisionService
{
    string Send(string value, string __fa_value, string __fa_value_1);
    string Escaped(string @event, string @__fa_event);
    string Generic<__fa_value_1>(__fa_value_1 value, out int __fa_value);
}

public class PredicateLocalCollisionTests
{
    [Test]
    public async Task Predicates_With_Colliding_Parameter_Names_Match_The_Right_Arguments()
    {
        var mock = Mock.Of<IPredicateLocalCollisionService>();
        mock.Send(value => value == "a", value => value == "b", value => value == "c").Returns("matched");
        mock.Escaped(value => value == "event", "other").Returns("escaped");
        mock.Generic<string>(value => value == "generic").Returns("generic matched");

        await Assert.That(mock.Object.Generic("generic", out _)).IsEqualTo("generic matched");
        await Assert.That(mock.Object.Generic("other", out _)).IsEqualTo("");
        await Assert.That(mock.Object.Send("a", "b", "c")).IsEqualTo("matched");
        await Assert.That(mock.Object.Send("b", "a", "c")).IsEqualTo("");
        await Assert.That(mock.Object.Escaped("event", "other")).IsEqualTo("escaped");
        await Assert.That(mock.Object.Escaped("other", "event")).IsEqualTo("");
    }
}
