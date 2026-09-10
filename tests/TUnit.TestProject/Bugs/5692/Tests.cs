using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5692;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/5692
/// When the value passed to <c>Assert.That(...)</c> is null AND its static type
/// declares an <c>implicit operator string</c>, the source-generated path must
/// not invoke that operator — doing so NREs inside the user's conversion.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ImplicitStringOperatorNullTests
{
    public record Id(string Value)
    {
        public static implicit operator string(Id id) => id.Value;
    }

    public sealed class Envelope
    {
        public Id? ConversationId { get; set; }
    }

    [Test]
    public async Task IsNull_on_nullable_with_implicit_to_string()
    {
        var envelope = new Envelope();
        await Assert.That(envelope.ConversationId).IsNull();
    }

    [Test]
    public async Task IsEqualTo_null_on_nullable_with_implicit_to_string()
    {
        var envelope = new Envelope();
        await Assert.That(envelope.ConversationId).IsEqualTo((Id?)null);
    }
}
