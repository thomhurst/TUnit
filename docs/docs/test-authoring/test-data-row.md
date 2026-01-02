# Test Data Row Metadata

When using data sources like `[MethodDataSource]` or `[ClassDataSource]`, you may want to customize individual test cases with specific display names, skip reasons, or categories. TUnit provides the `TestDataRow<T>` wrapper type for this purpose.

## Basic Usage

Wrap your test data in `TestDataRow<T>` to add metadata:

```csharp
using TUnit.Core;

public static class LoginTestData
{
    public static IEnumerable<TestDataRow<(string Username, string Password)>> GetCredentials()
    {
        yield return new(("admin", "secret123"), DisplayName: "Admin login");
        yield return new(("guest", "guest"), DisplayName: "Guest login");
        yield return new(("", ""), DisplayName: "Empty credentials", Skip: "Not implemented yet");
    }
}

public class LoginTests
{
    [Test]
    [MethodDataSource(typeof(LoginTestData), nameof(LoginTestData.GetCredentials))]
    public async Task TestLogin(string username, string password)
    {
        // Test implementation
    }
}
```

## Available Properties

`TestDataRow<T>` provides these optional properties:

| Property | Type | Description |
|----------|------|-------------|
| `DisplayName` | `string?` | Custom name shown in test output and IDE |
| `Skip` | `string?` | Skip reason; when set, the test is skipped |
| `Categories` | `string[]?` | Categories for filtering tests |

## Display Name Substitution

Display names support parameter substitution using `$paramName` or positional `$arg1`, `$arg2` syntax:

```csharp
public static IEnumerable<TestDataRow<(int A, int B, int Expected)>> GetMathData()
{
    yield return new((2, 3, 5), DisplayName: "Adding $A + $B = $Expected");
    yield return new((10, 5, 15), DisplayName: "$arg1 plus $arg2 equals $arg3");
}
```

The placeholders are replaced with the actual argument values at test discovery time.

## Working with Complex Types

For complex objects, wrap the entire object:

```csharp
public record UserTestCase(string Email, bool IsAdmin, string ExpectedRole);

public static class UserTestData
{
    public static IEnumerable<TestDataRow<UserTestCase>> GetUserCases()
    {
        yield return new(
            new UserTestCase("admin@test.com", true, "Administrator"),
            DisplayName: "Admin user gets admin role",
            Categories: ["Admin", "Roles"]
        );

        yield return new(
            new UserTestCase("user@test.com", false, "Standard"),
            DisplayName: "Regular user gets standard role"
        );
    }
}

public class UserRoleTests
{
    [Test]
    [MethodDataSource(typeof(UserTestData), nameof(UserTestData.GetUserCases))]
    public async Task TestUserRole(UserTestCase testCase)
    {
        // testCase.Email, testCase.IsAdmin, testCase.ExpectedRole
    }
}
```

## With Func<T> for Reference Types

When returning reference types, combine with `Func<T>` to ensure fresh instances:

```csharp
public static IEnumerable<TestDataRow<Func<HttpClient>>> GetHttpClients()
{
    yield return new(
        () => new HttpClient { BaseAddress = new Uri("https://api.example.com") },
        DisplayName: "Production API client"
    );

    yield return new(
        () => new HttpClient { BaseAddress = new Uri("https://staging.example.com") },
        DisplayName: "Staging API client"
    );
}
```

## Skipping Individual Test Cases

Use the `Skip` property to skip specific test cases while keeping others active:

```csharp
public static IEnumerable<TestDataRow<(string Browser, string Version)>> GetBrowsers()
{
    yield return new(("Chrome", "120"), DisplayName: "Chrome latest");
    yield return new(("Firefox", "121"), DisplayName: "Firefox latest");
    yield return new(("Safari", "17"), DisplayName: "Safari", Skip: "Safari not installed on CI");
    yield return new(("Edge", "120"), DisplayName: "Edge latest");
}
```

## Categorizing Test Cases

Apply categories to individual test cases for filtering:

```csharp
public static IEnumerable<TestDataRow<(string Endpoint, string Method)>> GetApiEndpoints()
{
    yield return new(
        ("/users", "GET"),
        DisplayName: "List users",
        Categories: ["API", "Users", "ReadOnly"]
    );

    yield return new(
        ("/users", "POST"),
        DisplayName: "Create user",
        Categories: ["API", "Users", "Write"]
    );

    yield return new(
        ("/admin/config", "PUT"),
        DisplayName: "Update config",
        Categories: ["API", "Admin", "Write"]
    );
}
```

Run only specific categories:
```bash
dotnet run -- --filter "Category=Admin"
```

## With ClassDataSource

`TestDataRow<T>` works with `[ClassDataSource]` too:

```csharp
public class DatabaseTestData : IEnumerable<TestDataRow<(string ConnectionString, string DbName)>>
{
    public IEnumerator<TestDataRow<(string ConnectionString, string DbName)>> GetEnumerator()
    {
        yield return new(
            ("Server=localhost;Database=TestDb1", "TestDb1"),
            DisplayName: "Local database"
        );
        yield return new(
            ("Server=remote;Database=TestDb2", "TestDb2"),
            DisplayName: "Remote database",
            Skip: "Remote server unavailable"
        );
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class DatabaseTests
{
    [Test]
    [ClassDataSource<DatabaseTestData>]
    public async Task TestDatabaseConnection(string connectionString, string dbName)
    {
        // Test implementation
    }
}
```

## Universal Data Source Support

`TestDataRow<T>` works with any data source that implements `IDataSourceAttribute`, including custom data source attributes. TUnit automatically detects and unwraps `TestDataRow<T>` instances, extracting the metadata regardless of the data source type.

## See Also

- [Arguments Attribute](./arguments.md) - For compile-time constant data with inline metadata
- [Method Data Sources](./method-data-source.md) - For dynamic test data generation
- [Class Data Sources](./class-data-source.md) - For class-based test data
- [Display Names](../customization-extensibility/display-names.md) - For global display name formatting
