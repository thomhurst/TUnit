using TUnit.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleTest;

public class MetadataPopulationTest
{    
    [Test]
    [Skip("Testing skip functionality")]
    public void SkippedTest()
    {
        // This test should be skipped
    }
    
    [Test]
    [Repeat(3)]
    [Category("Integration")]
    [Arguments(1, 2, 3)]
    [Arguments(4, 5, 6)]
    public async Task TestWithParameters(int a, int b, int expected)
    {
        await Assert.That(a + b).IsEqualTo(expected);
    }
    
    [Test]
    public async Task AsyncTestWithTimeout()
    {
        await Task.Delay(100);
        var result = 1 + 1;
        await Assert.That(result).IsEqualTo(2);
    }
    
    [Test]
    [MethodDataSource(nameof(GetTestData))]
    public async Task TestWithMethodDataSource(string input, int expected)
    {
        await Assert.That(input.Length).IsEqualTo(expected);
    }
    
    public static IEnumerable<(string, int)> GetTestData()
    {
        yield return ("test", 4);
        yield return ("hello", 5);
        yield return ("", 0);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class CategoryAttribute : Attribute
{
    public string Category { get; }
    
    public CategoryAttribute(string category)
    {
        Category = category;
    }
}