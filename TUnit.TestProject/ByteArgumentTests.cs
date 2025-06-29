namespace TUnit.TestProject;

public class ByteArgumentTests
{
    [Test]
    [Arguments((byte) 1)]
    public void Normal(byte b)
    {
        // Dummy method
    }

    [Test]
    [Arguments((byte) 1)]
    [Arguments(null)]
    public void Nullable(byte? b)
    {
        // Dummy method
    }
}
