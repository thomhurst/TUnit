namespace TUnit.TestProject;

public class NumberArgumentTests
{
    [Test]
    [Arguments(1)]
    public void Int(int i)
    {
        // Dummy method
    }

    [Test]
    [Arguments(1.1)]
    public void Double(double d)
    {
        // Dummy method
    }

    [Test]
    [Arguments(1.1f)]
    public void Float(float f)
    {
        // Dummy method
    }

    [Test]
    [Arguments(1L)]
    public void Long(long l)
    {
        // Dummy method
    }

    [Test]
    [Arguments(1UL)]
    public void ULong(ulong l)
    {
        // Dummy method
    }

    [Test]
    [Arguments(1U)]
    public void UInt(uint i)
    {
        // Dummy method
    }

}