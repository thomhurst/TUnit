using TUnit.Assertions;

public class TestExceptionAPI
{
    public static async Task TestThrowsAsync()
    {
        // This should return the exception so we can make further assertions on it
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Test message");
        });

        // We should be able to assert on the exception
        await Assert.That(exception.Message).IsEqualTo("Test message");
        
        Console.WriteLine($"Exception caught: {exception.Message}");
    }
    
    public static async Task TestThrows()
    {
        // This should also return the exception
        var exception = await Assert.Throws<ArgumentNullException>(() => 
        {
            throw new ArgumentNullException("paramName", "Test message");
        });

        // We should be able to assert on the exception
        await Assert.That(exception.ParamName).IsEqualTo("paramName");
        
        Console.WriteLine($"Exception caught: {exception.ParamName}");
    }
    
    public static async Task Main()
    {
        try
        {
            await TestThrowsAsync();
            Console.WriteLine("TestThrowsAsync passed!");
            
            await TestThrows();
            Console.WriteLine("TestThrows passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex}");
        }
    }
}