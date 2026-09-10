using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Regression tests for #6456: Microsoft.Kiota.Abstractions.IRequestAdapter could not be mocked
/// because its generic methods carry 'where ModelType : IParsable'. The generated explicit
/// interface implementations omitted the 'where ModelType : default' clause, so the compiler
/// parsed Task&lt;ModelType?&gt; as Task&lt;Nullable&lt;ModelType&gt;&gt; (CS0453/CS0539/CS0535/CS0314).
/// </summary>
public class Issue6456Tests
{
    private sealed class TestModel : IParsable
    {
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() =>
            new Dictionary<string, Action<IParseNode>>();

        public void Serialize(ISerializationWriter writer)
        {
        }
    }

    [Test]
    public async Task SendNoContentAsync_Can_Be_Mocked_And_Verified()
    {
        var mock = IRequestAdapter.Mock();
        var requestInfo = new RequestInformation();

        await mock.Object.SendNoContentAsync(requestInfo);

        mock.SendNoContentAsync(Any(), Any(), Any()).WasCalled(Times.Once);
    }

    [Test]
    public async Task SendAsync_With_Constrained_Generic_Returns_Configured_Value()
    {
        var mock = IRequestAdapter.Mock();
        var expected = new TestModel();
        mock.SendAsync<TestModel>(Any(), Any(), Any(), Any()).Returns(expected);

        var result = await mock.Object.SendAsync(
            new RequestInformation(),
            static _ => new TestModel(),
            errorMapping: null,
            CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(expected);
    }
}
