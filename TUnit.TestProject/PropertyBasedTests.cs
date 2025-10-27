using TUnit.Assertions.Extensions;
using TUnit.PropertyTesting;

namespace TUnit.TestProject;

public class PropertyBasedTests
{
    [Test]
    [PropertyDataSource(TestCaseCount = 10, Seed = 42)]
    public async Task TestIntegerAddition(
        [PropertyData<int>(Min = 0, Max = 100)] int a,
        [PropertyData<int>(Min = 0, Max = 100)] int b)
    {
        // Property: addition is commutative
        var result1 = a + b;
        var result2 = b + a;

        await Assert.That(result1).IsEqualTo(result2);
    }

    [Test]
    [PropertyDataSource(TestCaseCount = 10, Seed = 123)]
    public async Task TestStringConcatenation(
        [PropertyData<string>(MinLength = 0, MaxLength = 10)] string s1,
        [PropertyData<string>(MinLength = 0, MaxLength = 10)] string s2)
    {
        // Property: concatenation length equals sum of lengths
        var result = s1 + s2;

        await Assert.That(result.Length).IsEqualTo(s1.Length + s2.Length);
    }

    [Test]
    [PropertyDataSource(TestCaseCount = 5, Seed = 999)]
    public async Task TestListReverse(
        [PropertyData<int>(Min = 1, Max = 10)] int count)
    {
        // Property: reversing twice gives original
        var list = Enumerable.Range(1, count).ToList();
        var reversed = list.AsEnumerable().Reverse().ToList();
        var doubleReversed = reversed.AsEnumerable().Reverse().ToList();

        await Assert.That(doubleReversed).IsEquivalentTo(list);
    }
}
