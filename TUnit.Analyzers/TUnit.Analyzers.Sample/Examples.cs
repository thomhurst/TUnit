// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using TUnit.Assertions;

namespace TUnit.Analyzers.Sample;

// If you don't see warnings, build the Analyzers Project.

public class Examples
{
    public class CommonClass // Try to apply quick fix using the IDE.
    {
    }

    public void ToStars()
    {
        Assert.That("1", Is.EqualTo("1").Or.Is.EqualTo("2").And.Is.EqualTo("1"));
        var spaceship = new Spaceship();
        spaceship.SetSpeed(300000000); // Invalid value, it should be highlighted.
        spaceship.SetSpeed(42);
    }
}