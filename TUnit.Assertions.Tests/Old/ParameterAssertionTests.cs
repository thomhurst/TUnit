namespace TUnit.Assertions.UnitTests;

public class ParameterAssertionTests
{
    [Test]
    public void Greet_ShouldReturnCorrectGreeting_WhenAllParametersAreProvided()
    {
        string name = "TUnit";
        string greeting = "Hi";
        string punctuation = "?";

        string result = Greet(name, greeting, punctuation);

        NUnitAssert.That(result, Is.EqualTo("Hi, TUnit?"));
    }

    [Test]
    public void Greet_ShouldUseDefaultValues_WhenOptionalParametersAreNotProvided()
    {
        string name = "TUnit";

        string result = Greet(name);

        NUnitAssert.That(result, Is.EqualTo("Hello, TUnit!"));
    }

    [Test]
    public void Greet_ShouldUseDefaultForPunctuation_WhenOnlyGreetingIsProvided()
    {
        string name = "TUnit";
        string greeting = "Hi";

        string result = Greet(name, greeting);

        NUnitAssert.That(result, Is.EqualTo("Hi, TUnit!"));
    }

    private static string Greet(string name, string greeting = "Hello", string punctuation = "!")
    {
        return $"{greeting}, {name}{punctuation}";
    }
}

