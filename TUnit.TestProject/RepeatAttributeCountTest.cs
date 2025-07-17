using TUnit.Core;

namespace TUnit.TestProject;

public class RepeatAttributeCountTest
{
    [Test]
    [Repeat(3)]
    public async Task TestWithRepeat()
    {
        await Task.CompletedTask;
    }
    
    [Test]
    [Repeat(2)]
    [Arguments(1)]
    [Arguments(2)]
    public async Task TestWithRepeatAndArguments(int value)
    {
        await Task.CompletedTask;
    }
}