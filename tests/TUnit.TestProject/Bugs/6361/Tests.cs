using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6361;

[EngineTest(ExpectedResult.Pass)]
public sealed class Issue6361InstanceMethodDataSourceIsolationTests
{
    private static readonly ConcurrentBag<int> TestInstanceIds = [];

    private static int _nextInstanceId;
    private static int _dataSourceInstanceId;

    private readonly int _instanceId = Interlocked.Increment(ref _nextInstanceId);

    [Test]
    [MethodDataSource(nameof(GetCases), DeferEnumeration = true)]
    public async Task InstanceMethodDataSource_DoesNotReuseEnumerationInstance(string value)
    {
        TestInstanceIds.Add(_instanceId);

        await Assert.That(value).IsNotNullOrEmpty();
    }

    public IEnumerable<string> GetCases()
    {
        Interlocked.Exchange(ref _dataSourceInstanceId, _instanceId);

        yield return "Case1";
        yield return "Case2";
        yield return "Case3";
    }

    [After(Class)]
    public static async Task AssertEnumerationInstanceWasIsolated()
    {
        try
        {
            var testInstanceIds = TestInstanceIds.ToArray();
            var dataSourceInstanceId = Volatile.Read(ref _dataSourceInstanceId);

            await Assert.That(dataSourceInstanceId).IsNotEqualTo(0);
            await Assert.That(testInstanceIds).Count().IsEqualTo(3);
            await Assert.That(testInstanceIds.Distinct()).Count().IsEqualTo(3);
            await Assert.That(testInstanceIds).DoesNotContain(dataSourceInstanceId);
        }
        finally
        {
            TestInstanceIds.Clear();
            _nextInstanceId = 0;
            _dataSourceInstanceId = 0;
        }
    }
}
