---
sidebar_position: 9
---

# Skipping Tests

If you want to simply skip a test, just place a `[Skip(reason)]` attribute on your test with an explanation of why you're skipping it.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test, Skip("There's a bug! See issue #1")]
    public async Task MyTest()
    {
        ...
    }
}
```

## Custom Logic

The `SkipAttribute` can be inherited and custom logic plugged into it, so it only skips the test if it meets certain criteria.

As an example, this could be used to skip tests on certain operating systems.

```csharp
public class WindowsOnlyAttribute() : SkipAttribute("This test is only supported on Windows")
{
    public override Task<bool> ShouldSkip(BeforeTestContext context)
    {
        return Task.FromResult(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }
}
```

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test, WindowsOnly]
    public async Task MyTest()
    {
        ...
    }
}
```

