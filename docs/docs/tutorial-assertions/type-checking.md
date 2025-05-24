---
sidebar_position: 5
---

# Type Checking

TUnit assertions try to check the types at compile time. 
This gives faster developer feedback and helps speed up development time.
(Ever made a silly mistake on a test, but haven't realised till 15 minutes later after your build pipeline has finally told you? I know I have!)

So this wouldn't compile, because we're comparing an `int` and a `string`:

```csharp
    [Test]
    public async Task MyTest()
    {
        await Assert.That(1).IsEqualTo("1");
    }
```
