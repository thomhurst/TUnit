using TUnit.Mocks;

namespace TUnit.Mocks.Generated
{
    // An existing user type must not interfere with the namespace-scoped closed alias.
    public sealed class TContext;
}

namespace TUnit.Mocks.Tests
{
    public class Issue7302AliasTests
    {
        [Test]
        public async Task User_Type_With_The_Alias_Name_Does_Not_Change_Closed_Setups()
        {
            var mock = Mock.Of<IMessageInvoker7302<int>>();
            mock.RoundTrip<long>(1, 2L).Returns(3L);
            await Assert.That(mock.Object.RoundTrip(1, 2L)).IsEqualTo(3L);
        }
    }
}
