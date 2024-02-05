using TUnit.Core;

namespace TUnit.TestProject;

public class CombinativeTests
{
    [Test]
    [Combinative]
    public async Task CombinativeTest_One(
        [CombinativeValues("A", "B", "C", "D")] string str, 
        [CombinativeValues(1, 2, 3)] int i, 
        [CombinativeValues(true, false)] bool boolean)
    {
        await Task.CompletedTask;
    }
}