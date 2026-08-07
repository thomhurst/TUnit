# Test Parameters

TUnit allows you to pass custom key-value parameters to your tests at runtime using the `--test-parameter` command-line option. These parameters are accessible via `TestContext.Parameters`.

## Passing Parameters

Pass parameters when running your tests using the `--test-parameter` flag with `KEY=VALUE` syntax:

```bash
dotnet run --test-parameter environment=staging
dotnet run --test-parameter environment=staging --test-parameter api-url=https://api.example.com
```

You can pass multiple values for the same key:

```bash
dotnet run --test-parameter browser=chrome --test-parameter browser=firefox
```

## Accessing Parameters in Tests

Parameters are available as a static dictionary on `TestContext`:

```csharp
public class MyTests
{
    [Test]
    public async Task ConnectsToCorrectEnvironment()
    {
        var environments = TestContext.Parameters["environment"];
        var environment = environments.First(); // "staging"

        // Use the parameter to configure your test
        var baseUrl = environment switch
        {
            "production" => "https://api.example.com",
            "staging" => "https://staging.api.example.com",
            _ => "http://localhost:5000"
        };

        // ...
    }
}
```

`TestContext.Parameters` is of type `IReadOnlyDictionary<string, List<string>>`. Each key maps to a list of values, since the same key can be specified multiple times on the command line.

## Common Use Cases

### Environment-specific configuration

```csharp
[Before(Test)]
public void SetupEnvironment()
{
    if (TestContext.Parameters.TryGetValue("environment", out var values))
    {
        Environment.SetEnvironmentVariable("TEST_ENV", values.First());
    }
}
```

### Conditional test logic

```csharp
[Test]
public async Task IntegrationTest()
{
    if (!TestContext.Parameters.ContainsKey("run-integration"))
    {
        Assert.Skip("Integration tests require --test-parameter run-integration=true");
    }

    // Run the integration test...
}
```

### Passing secrets or connection strings

```csharp
[Test]
public async Task DatabaseTest()
{
    var connectionStrings = TestContext.Parameters["connection-string"];
    using var connection = new SqlConnection(connectionStrings.First());
    // ...
}
```

```bash
dotnet run --test-parameter "connection-string=Server=localhost;Database=TestDb;..."
```

## Notes

- Parameters are available for the entire test session — they are not scoped to individual tests.
- The parameter format must be `KEY=VALUE`. Values containing `=` characters are supported (only the first `=` is used as the delimiter).
- Parameters are accessible from any test, hook, or data source via `TestContext.Parameters`.
