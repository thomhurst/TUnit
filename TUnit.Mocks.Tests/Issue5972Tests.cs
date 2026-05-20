using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

public interface IIssue5972Service
{
    int GetInt(int i);
}

public class Issue5972Tests
{
    [Test]
    public async Task Callback_Chained_Before_Returns_Still_Returns_Configured_Value()
    {
        var mock = IIssue5972Service.Mock();
        var capture = 0;

        mock.GetInt(Any())
            .Callback((int i) => capture = i)
            .Returns(42);

        var result = mock.Object.GetInt(123);

        await Assert.That(result).IsEqualTo(42);
        await Assert.That(capture).IsEqualTo(123);
    }
}
