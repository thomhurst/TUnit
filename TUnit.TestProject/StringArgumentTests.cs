namespace TUnit.TestProject;

public class StringArgumentTests
{
    [Test]
    [Arguments("")]
    [Arguments(@"\")]
    [Arguments(@"\t")]
    [Arguments("\t")]
    [Arguments("\\t")]
    [Arguments("\\\t")]
    [Arguments("\\\\t")]
    [Arguments(
        """
        Hello
        World
        """
    )]
    public void Normal(string s)
    {
        // Dummy method
    }

    [Test]
    [Arguments("")]
    [Arguments(@"\")]
    [Arguments(@"\t")]
    [Arguments("\t")]
    [Arguments("\\t")]
    [Arguments("\\\t")]
    [Arguments("\\\\t")]
    [Arguments(null)]
    public void Nullable(string? s)
    {
        // Dummy method
    }
}