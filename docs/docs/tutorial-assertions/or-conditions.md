---
sidebar_position: 4
---

# Or Conditions

Similar to the `And` property, there is also the `Or` property.

When using this, only one condition needs to pass:

```csharp
    [Test]
    [Repeat(100)]
    public async Task MyTest()
    {
        int[] array = [1, 2];
        var randomValue1 = Random.Shared.GetItems(array, 1).First();
        var randomValue2 = Random.Shared.GetItems(array, 1).First();
        
        var result = Add(randomValue1, randomValue2);

        await Assert.That(result)
            .IsEqualTo(2)
            .Or.IsEqualTo(3)
            .Or.IsEqualTo(4);
    }
```

