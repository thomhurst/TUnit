using TUnit.Core;

namespace TUnit.TestProject;

/// <summary>
/// Simple test class to verify VSCode test discovery
/// </summary>
public class VSCodeTest
{
    [Test]
    public void SimpleTest()
    {
        // This test should appear with a play button in VSCode
        Assert.That(true).IsTrue();
    }

    [Test]
    public async Task AsyncTest()
    {
        await Task.Delay(1);
        // This async test should also appear with a play button in VSCode
        Assert.That(2 + 2).IsEqualTo(4);
    }

    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(4, 5, 9)]
    public void ParameterizedTest(int a, int b, int expected)
    {
        // Parameterized tests should also show play buttons
        Assert.That(a + b).IsEqualTo(expected);
    }
}