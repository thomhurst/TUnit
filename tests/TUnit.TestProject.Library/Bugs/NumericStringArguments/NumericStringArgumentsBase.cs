namespace TUnit.TestProject.Library.Bugs.NumericStringArguments;

#if NET
// String rows for numeric parameters, inherited from another assembly, are converted at runtime.
public abstract class NumericStringArgumentsBase
{
    [Test]
    [Arguments("42")]
    public void IntFromString(int value)
    {
        if (value != 42)
        {
            throw new InvalidOperationException($"Expected 42 but got {value}");
        }
    }

    [Test]
    [Arguments("1.5")]
    public void DoubleFromString(double value)
    {
        if (value != 1.5)
        {
            throw new InvalidOperationException($"Expected 1.5 but got {value}");
        }
    }
}
#endif
