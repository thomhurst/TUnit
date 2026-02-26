# Culture

The `[Culture]` attribute sets the [current Culture](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentculture) for the duration of a test. It can be applied at the test, class, or assembly level. The culture is restored to its original value when the test completes.

## Why This Matters

Different locales format numbers, dates, and currencies differently. A test that passes on a developer's machine in the US may fail on a CI server in Germany if it parses or compares formatted strings. The `[Culture]` attribute locks the culture so results are predictable regardless of where the tests run.

**Without `[Culture]`** — this test passes in `en-US` but fails in `de-AT` (where the decimal separator is `,`):

```csharp
[Test]
public async Task Fragile_Without_Culture()
{
    // Fails in locales where "3.5" is not a valid double literal
    var value = double.Parse("3.5");
    await Assert.That(value).IsEqualTo(3.5);
}
```

**With `[Culture]`** — the test is stable everywhere:

```csharp
[Test, Culture("en-US")]
public async Task Stable_With_Culture()
{
    var value = double.Parse("3.5");
    await Assert.That(value).IsEqualTo(3.5);
}
```

## Examples

### Test-Level Culture

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test, Culture("de-AT")]
    public async Task Parse_German_Decimal()
    {
        await Assert.That(double.Parse("3,5")).IsEqualTo(3.5);
    }
}
```

### Class-Level Culture

Apply the attribute to the class to set the culture for every test in that fixture:

```csharp
[Culture("fr-FR")]
public class FrenchFormattingTests
{
    [Test]
    public async Task Currency_Format()
    {
        var formatted = 1234.56.ToString("C");
        await Assert.That(formatted).Contains("1");
    }
}
```

### Assembly-Level Culture

Lock the culture for the entire test assembly:

```csharp
[assembly: Culture("en-US")]
```

## Notes

- Only one culture can be specified per scope. To run the same test under multiple cultures, factor out the test logic into a private method and call it from separate test methods, each with its own `[Culture]` attribute.
- The attribute sets both `CurrentCulture` and `CurrentUICulture` for the executing thread.

## See Also

- [Command-Line Flags](../reference/command-line-flags.md) — Runtime configuration options
