using TUnit.Core;

namespace TUnit.TestProject;

public class CombinativeTests
{
    [CombinativeTest]
    public async Task CombinativeTest_One(
        [CombinativeValues("A", "B", "C", "D")] string str, 
        [CombinativeValues(1, 2, 3)] int i, 
        [CombinativeValues(true, false)] bool boolean)
    {
        await Task.CompletedTask;
    }
    
    [CombinativeTest]
    public async Task CombinativeTest_Two(
        [CombinativeValues(1, 2)] int i, 
        [CombinativeValues(1, 2, 3)] int i2, 
        [CombinativeValues(1, 2, 3, 4)] int i3, 
        [CombinativeValues(true, false)] bool boolean)
    {
        await Task.CompletedTask;
    }
}