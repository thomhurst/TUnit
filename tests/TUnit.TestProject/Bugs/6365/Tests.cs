using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6365;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [MethodDataSource<Provider>(nameof(Provider.GetTestCases))]
    public async Task Typed_MethodDataSource_Uses_Provider_Instance(TestCase testCase)
    {
        await Assert.That(testCase).IsEqualTo(new TestCase(1, 2));
    }

    public sealed record TestCase(int A, double B);

    public sealed class Provider : ProviderBase
    {
        protected override IEnumerable<TestCase> GenerateTestCases()
        {
            yield return new TestCase(1, 2);
        }
    }

    public abstract class ProviderBase
    {
        public IEnumerable<TestCase> GetTestCases()
        {
            foreach (var testCase in GenerateTestCases())
            {
                yield return testCase;
            }
        }

        protected abstract IEnumerable<TestCase> GenerateTestCases();
    }
}
