# Culture

The `[Culture]` attribute is used to set the [current Culture](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentculture) for the duration of a test. It may be specified at the level of a test, fixture or assembly.
The culture remains set until the test or fixture completes and is then reset to its original value.

Specifying the culture is useful for comparing against expected output
that depends on the culture, e.g. decimal separators, etc.

Only one culture may be specified. If you wish to run the same test under multiple cultures,
you can achieve the same result by factoring out your test code into a private method
that is called by each individual test method.

## Examples

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test, Culture("de-AT")]
    public async Task Test3()
    {
        await Assert.That(double.Parse("3,5")).IsEqualTo(3.5);
    }
}
```
