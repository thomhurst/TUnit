namespace TUnit.TestProject.Library.Bugs.NullableNumericStringArguments;

#if NET
// String rows for parsable parameters, inherited from another assembly, are converted at runtime.
public abstract class NullableNumericStringArgumentsBase
{
    [Test]
    [Arguments("42")]
    public void IntFromString(int? value)
    {
        if (value != 42)
        {
            throw new InvalidOperationException($"Expected 42 but got {value}");
        }
    }

    [Test]
    [Arguments("1.5")]
    public void DoubleFromString(double? value)
    {
        if (value != 1.5)
        {
            throw new InvalidOperationException($"Expected 1.5 but got {value}");
        }
    }

    [Test]
    [Arguments(null)]
    public void NullInt(int? value)
    {
        if (value is not null)
        {
            throw new InvalidOperationException($"Expected null but got {value}");
        }
    }

    [Test]
    [Arguments("00000000-0000-0000-0000-000000000000")]
    public void GuidFromString(System.Guid? value)
    {
        if (value != System.Guid.Empty)
        {
            throw new InvalidOperationException($"Expected empty guid but got {value}");
        }
    }
}
#endif
