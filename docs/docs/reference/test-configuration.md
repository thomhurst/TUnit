# Test Configuration

TUnit supports having a `testconfig.json` file within your test project.

This can be used to store key-value configuration pairs. To retrieve these within tests, use the static method `TestContext.Configuration.Get(key)`.

## Example

`testconfig.json`
```json
{
  "MyKey1": "MyValue1",
  "BaseUrl": "https://api.example.com",
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
    var value1 = TestContext.Configuration.Get("MyKey1"); // "MyValue1"
    var value2 = TestContext.Configuration.Get("Nested:MyKey2"); // "MyValue2"

    await Assert.That(value1).IsEqualTo("MyValue1");
}
```

## Missing Keys and Files

If a key does not exist, `Get` returns `null`. If the `testconfig.json` file is missing entirely, all calls to `Get` return `null`. There is no exception thrown in either case.

```csharp
[Test]
public async Task Configuration_Returns_Null_For_Unknown_Key()
{
    var value = TestContext.Configuration.Get("DoesNotExist");

    await Assert.That(value).IsNull();
}
```

## Nested Key Syntax

Use a colon (`:`) to access values nested inside JSON objects. The path follows the same convention as `Microsoft.Extensions.Configuration`:

```json
{
  "Database": {
    "Connection": {
      "Timeout": "30"
    }
  }
}
```

```csharp
var timeout = TestContext.Configuration.Get("Database:Connection:Timeout"); // "30"
```

## Typed Configuration

All values are returned as `string?`. Convert to the required type as needed:

```csharp
[Test]
public async Task Respects_Configured_Timeout()
{
    var rawTimeout = TestContext.Configuration.Get("Database:Connection:Timeout");
    var timeout = int.Parse(rawTimeout!);

    await Assert.That(timeout).IsEqualTo(30);
}
```
