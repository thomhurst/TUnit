namespace TUnit.TestProject;

public class FailedInitializationTests
{
    [Test]
    [MethodDataSource(nameof(Data))]
    public void FailingDataSource(int value)
    {
    }

    public static int Data() => throw new Exception();
}