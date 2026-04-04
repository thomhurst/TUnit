namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class WithInnerExceptionsTests
    {
        [Test]
        public async Task WithInnerExceptions_Count_Succeeds()
        {
            var aggregate = new AggregateException(
                new InvalidOperationException("one"),
                new ArgumentException("two"),
                new FormatException("three"));
            Action action = () => throw aggregate;

            await Assert.That(action)
                .Throws<AggregateException>()
                .WithInnerExceptions(exceptions => exceptions.Count().IsEqualTo(3));
        }

        [Test]
        public async Task WithInnerExceptions_Count_Fails()
        {
            var aggregate = new AggregateException(
                new InvalidOperationException("one"),
                new ArgumentException("two"));
            Action action = () => throw aggregate;

            var sut = async () => await Assert.That(action)
                .Throws<AggregateException>()
                .WithInnerExceptions(exceptions => exceptions.Count().IsEqualTo(5));

            await Assert.That(sut).ThrowsException();
        }

        [Test]
        public async Task WithInnerExceptions_AllSatisfy_Succeeds()
        {
            var aggregate = new AggregateException(
                new ArgumentException("one", "param1"),
                new ArgumentException("two", "param2"),
                new ArgumentException("three", "param3"));
            Action action = () => throw aggregate;

            await Assert.That(action)
                .Throws<AggregateException>()
                .WithInnerExceptions(exceptions => exceptions
                    .All().Satisfy(e => e.IsTypeOf<ArgumentException>()));
        }

        [Test]
        public async Task WithInnerExceptions_AllSatisfy_Fails_When_Mixed_Types()
        {
            var aggregate = new AggregateException(
                new ArgumentException("one"),
                new FormatException("two"));
            Action action = () => throw aggregate;

            var sut = async () => await Assert.That(action)
                .Throws<AggregateException>()
                .WithInnerExceptions(exceptions => exceptions
                    .All().Satisfy(e => e.IsTypeOf<ArgumentException>()));

            await Assert.That(sut).ThrowsException();
        }

        [Test]
        public async Task WithInnerExceptions_ThrowsExactly_Count_Succeeds()
        {
            var aggregate = new AggregateException(
                new InvalidOperationException("one"),
                new ArgumentException("two"));
            Action action = () => throw aggregate;

            await Assert.That(action)
                .ThrowsExactly<AggregateException>()
                .WithInnerExceptions(exceptions => exceptions.Count().IsEqualTo(2));
        }

        [Test]
        public async Task WithInnerExceptions_Fails_When_Not_AggregateException()
        {
            Action action = () => throw new InvalidOperationException("not aggregate");

            var sut = async () => await Assert.That(action)
                .Throws<InvalidOperationException>()
                .WithInnerExceptions(exceptions => exceptions.Count().IsEqualTo(1));

            await Assert.That(sut).ThrowsException();
        }
    }
}
