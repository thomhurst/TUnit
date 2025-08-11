using TUnit.Core;

namespace TUnit.TestProject;

/// <summary>
/// Test class to verify VSCode test discovery works correctly
/// This file can be used to manually test that VSCode shows play buttons in the editor gutter
/// </summary>
public class VSCodeIntegrationTest
{
    [Test]
    public void SimpleTest()
    {
        // This test should appear with a play button in VSCode editor gutter
        // after implementing the VSCode test discovery fix
        Assert.That(true).IsTrue();
    }

    [Test]
    public async Task AsyncTest()
    {
        await Task.Delay(1);
        // This async test should also show a play button
        Assert.That(2 + 2).IsEqualTo(4);
    }

    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(4, 5, 9)]
    public void ParameterizedTest(int a, int b, int expected)
    {
        // Parameterized tests should show play buttons for each argument set
        Assert.That(a + b).IsEqualTo(expected);
    }
}