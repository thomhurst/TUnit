---
sidebar_position: 7
---

# Test Configuration

TUnit supports having a `testconfig.json` file within your test project.

Then can be used to store key-value configuration pairs.

To retrieve these within your tests, you can use the static method `TestContext.Configuration.Get(key)`

`testconfig.json`
```json
{
  "MyKey1": "MyValue1",
  "Nested": {
    "MyKey2": "MyValue2"
  }
}
```

`Tests.cs`
```csharp
    [Test]
    public async Task Test()
    {
        var value1 = TestContext.Configuration.Get("MyKey1"); // MyValue1 - As defined above
        var value2 = TestContext.Configuration.Get("Nested:MyKey2"); // MyValue2 - As defined above
        
        ...
    }
```
