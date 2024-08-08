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
    
    [Test]
    [Arguments("foo", 1, true)]
    [Arguments("bar", 2, false)]
    [DisplayName("Test with: $value1 $value2 $value3!")]
    public async Task Test3(string value1, int value2, bool value3)
    {
        await Task.CompletedTask;
    }
}