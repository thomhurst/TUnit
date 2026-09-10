namespace TUnit.Assertions.Tests.Old;

public class ParameterAssertionTests
{
    [Test]
    public async Task Greet_ShouldReturnCorrectGreeting_WhenAllParametersAreProvided()
    {
        var name = "TUnit";
        var greeting = "Hi";
        var punctuation = "?";

        var result = Greet(name, greeting, punctuation);

        await TUnitAssert.That(result).IsEqualTo("Hi, TUnit?");
    }

    [Test]
    public async Task Greet_ShouldUseDefaultValues_WhenOptionalParametersAreNotProvided()
    {
        var name = "TUnit";

        var result = Greet(name);

        await TUnitAssert.That(result).IsEqualTo("Hello, TUnit!");
    }

    [Test]
    public async Task Greet_ShouldUseDefaultForPunctuation_WhenOnlyGreetingIsProvided()
    {
        var name = "TUnit";
        var greeting = "Hi";

        var result = Greet(name, greeting);

        await TUnitAssert.That(result).IsEqualTo("Hi, TUnit!");
    }

    private static string Greet(string name, string greeting = "Hello", string punctuation = "!")
    {
        return $"{greeting}, {name}{punctuation}";
    }
}

