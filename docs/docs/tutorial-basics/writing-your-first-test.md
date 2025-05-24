---
sidebar_position: 2
---

# Writing your first test

Start by creating a new class:

```csharp
namespace MyTestProject;

public class MyTestClass
{
    
}
```

Now add a method, with a `[Test]` attribute on it:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    public async Task MyTest()
    {
        
    }
}
```

That's it. That is your runnable test.

We haven't actually made it do anything yet, but we should be able to build our project and run that test.

Tests will pass if they execute successfully without any exceptions.

Let's add some code to show you how a test might look once finished:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    public async Task MyTest()
    {
        var result = Add(1, 2);

        await Assert.That(result).IsEqualTo(3);
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

Here you can see we've executed some code and added an assertion. We'll go more into that later. 
