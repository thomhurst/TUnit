using Amazon.SQS;

namespace TUnit.Mocks.Tests;

public class AmazonSqsStaticAbstractTests
{
    [Test]
    public async Task Mock_Extension_Does_Not_Require_GenerateMock_Attribute()
    {
        var mock = IAmazonSQS.Mock();

        await Assert.That(mock.Object).IsAssignableTo<IAmazonSQS>();
    }
}
