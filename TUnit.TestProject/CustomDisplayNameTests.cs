using TUnit.Core;

namespace TUnit.TestProject;

public class CustomDisplayNameTests
{
    [Test]
    [DisplayName("A super important test!")]
    public void Test()
    {
        // Dummy method
    }
    
    [Test]
    [DisplayName("Another super important test!")]
    public async Task Test2()
    {
        await Task.CompletedTask;
    }
}