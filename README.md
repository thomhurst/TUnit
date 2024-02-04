# TUnit
T(est)Unit!

```csharp
    [Test]
    public async Task Test1()
    {
        var value = "Hello world!";
        
        await Assert.That(value)
            .Is.Not.Null
            .And.Is.EqualTo("hello world!", StringComparison.InvariantCultureIgnoreCase)
            .And.Has.Count().EqualTo(12)
            .And.Does.StartWith("H");
    }
```

## Motivations
There are only three main testing frameworks in the .NET world - xUnit, NUnit and MSTest.
More frameworks means more options, and more options motivates more features or improvements.

These testing frameworks are amazing, but I've had some issues with them. You might not have had any of these, but these are my experiences:

### xUnit
There is no way to tap into information about a test in a generic way. 
For example, I've had some Playwright tests run before, and I want them to save a screenshot or video ONLY when the test fails.
If the test passes, I don't have anything to investigate, and it'll use up unnecessary storage, and it'll probably slow my test suite down if I had hundreds or thousands of tests all trying to save screenshots.

However, if I'm in a Dispose method which is called when the test ends, then there's no way for me to know if my test succeeded or failed. I'd have to do some really clunky workaround involving try catch and setting a boolean or exception to a class field and checking that. And to do that for every test was just not ideal.

#### Assertions
I have stumbled across assertions so many times where the arguments are the wrong way round.
This can result in really confusing error messages.
```csharp
var one = 2;
Assert.Equal(1, one)
Assert.Equal(one, 1)
```

### NUnit

#### Assertions
I absolutely love the newer assertion syntax in NUnit. The `Assert.That(something, Is.Something)`. I think it's really clear to read, it's clear what is being asserted, and it's clear what you're trying to achieve.

However, there is a lack of type checking on assertions. (Yes, there are analyzer packages to help with this, but this still isn't strict type checking.)

`Assert.That("1", Throws.Exception);`

This assertion makes no sense, because we're passing in a string. This can never throw an exception because it isn't a delegate that can be executed. But it's still perfectly valid code that will compile.

As does this:
`Assert.That(1, Does.Contain("Foo!"));`

With TUnit assertions, I wanted to make these impossible to compile. So type constraints are built into the assertions themselves. There should be no way for a non-delegate to be able to do a `Throws` assertion, or for an `int` assertion to check for `string` conditions.

So in TUnit, this will compile:

`await Assert.That(() => 1).Throws.Nothing;`

This won't:

`await Assert.That(1).Throws.Nothing;`