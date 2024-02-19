---
sidebar_position: 3
---

# And Conditions

TUnit can chain assertions together, using the `And` property. This reads very much like English, and aims to keep the test easy to read and understand, and doesn't require you repeat boilerplate code such as `Assert.That` over and over.

Every condition must pass when using `And`s:

```csharp
    [Test]
    public async Task MyTest()
    {
        var result = Add(1, 2);
        
        await Assert.That(result)
            .Is.Not.Null()
            .And.Is.Positive()
            .And.Is.EqualTo(3);
    }
```