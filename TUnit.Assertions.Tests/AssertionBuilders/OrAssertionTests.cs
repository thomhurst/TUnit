using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests.AssertionBuilders;

public sealed class OrAssertionTests
{
    [Test]
    public async Task Throws_When_Mixed_With_And()
    {
        char[] sut = "ABCD".ToCharArray();
#pragma warning disable TUnitAssertions0001
        var action = async () => await Assert.That(sut)
            .Contains('A').And
            .Contains('B').And
            .Contains('C').Or
            .Contains('D');
#pragma warning restore TUnitAssertions0001

        await Assert.That(action).Throws<MixedAndOrAssertionsException>();
    }
    
    
    [Test]
    public async Task Basic()
    {
        var foo = await Assert.That(ThrowException).ThrowsException();
        await Assert.That(foo)
                    .IsNotAssignableTo<ArgumentOutOfRangeException>()
                    .Or
                    .Satisfies(x => (ArgumentOutOfRangeException)x,
                               x => x.HasMember(y => y.ActualValue).EqualTo("foo"));
    }
    
    private void ThrowException() => throw new InvalidOperationException("foo");

    [Test]
    public async Task Does_Not_Throw_For_Multiple_Or()
    {
        char[] sut = "ABCD".ToCharArray();
#pragma warning disable TUnitAssertions0001
        var action = async () => await Assert.That(sut)
            .Contains('A').Or
            .Contains('B').Or
            .Contains('C').Or
            .Contains('D');
#pragma warning restore TUnitAssertions0001

        await Assert.That(action).ThrowsNothing();
    }
}