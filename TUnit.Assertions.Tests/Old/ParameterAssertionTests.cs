namespace TUnit.Assertions.Tests.Old;

public class ParameterAssertionTests
{
    [Test]
    public async Task Greet_ShouldReturnCorrectGreeting_WhenAllParametersAreProvided()
    {
        string name = "TUnit";
        string greeting = "Hi";
        string punctuation = "?";

        string result = Greet(name, greeting, punctuation);

        await TUnitAssert.That(result).IsEqualTo("Hi, TUnit?");
    }

    [Test]
    public async Task Greet_ShouldUseDefaultValues_WhenOptionalParametersAreNotProvided()
    {
        string name = "TUnit";

        string result = Greet(name);

        await TUnitAssert.That(result).IsEqualTo("Hello, TUnit!");
    }

    [Test]
    public async Task Greet_ShouldUseDefaultForPunctuation_WhenOnlyGreetingIsProvided()
    {
        string name = "TUnit";
        string greeting = "Hi";

        string result = Greet(name, greeting);

        await TUnitAssert.That(result).IsEqualTo("Hi, TUnit!");
    }

    private static string Greet(string name, string greeting = "Hello", string punctuation = "!")
    {
        return $"{greeting}, {name}{punctuation}";
    }
}

