namespace TUnit.TestProject;

[Retry(3)] // Class-level retry
public class SimpleRetryPriorityTest
{
    public static int TestCount { get; set; }
    
    [Test]
    [Retry(1)] // Method-level retry - should override class level
    public void TestWithMethodRetry()
    {
        TestCount++;
        throw new Exception("Test failure");
    }
}