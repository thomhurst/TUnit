namespace TUnit.TestProject;

public class FailedInitializationTests
{
    [Test]
    [MethodDataSource(nameof(Data))]
    public void Test(int value)
    {
    }

    public static int Data() => throw new NotImplementedException();
}