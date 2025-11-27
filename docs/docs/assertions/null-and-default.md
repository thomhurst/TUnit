---
sidebar_position: 2.5
---

# Null and Default Value Assertions

TUnit provides assertions for testing null values and default values. These assertions integrate with C#'s nullability annotations to provide better compile-time safety.

## Null Assertions

### IsNull

Tests that a value is `null`:

```csharp
[Test]
public async Task Null_Value()
{
    string? result = GetOptionalValue();
    await Assert.That(result).IsNull();

    Person? person = FindPerson("unknown-id");
    await Assert.That(person).IsNull();
}
```

### IsNotNull

Tests that a value is not `null`:

```csharp
[Test]
public async Task Not_Null_Value()
{
    string? result = GetRequiredValue();
    await Assert.That(result).IsNotNull();

    var user = GetCurrentUser();
    await Assert.That(user).IsNotNull();
}
```

## Nullability Flow Analysis

When you use `IsNotNull()`, C#'s nullability analysis understands that the value is non-null afterward:

```csharp
[Test]
public async Task Nullability_Flow()
{
    string? maybeNull = GetValue();

    // After this assertion, compiler knows it's not null
    await Assert.That(maybeNull).IsNotNull();

    // No warning - compiler knows it's safe
    int length = maybeNull.Length;
}
```

This works with chaining too:

```csharp
[Test]
public async Task Chained_After_Null_Check()
{
    string? input = GetInput();

    await Assert.That(input)
        .IsNotNull()
        .And.IsNotEmpty()  // Compiler knows input is not null
        .And.Length().IsGreaterThan(5);
}
```

## Default Value Assertions

### IsDefault

Tests that a value equals the default value for its type:

```csharp
[Test]
public async Task Default_Values()
{
    // Reference types - default is null
    string? text = default;
    await Assert.That(text).IsDefault();

    // Value types - default is zero/false/empty struct
    int number = default;
    await Assert.That(number).IsDefault(); // 0

    bool flag = default;
    await Assert.That(flag).IsDefault(); // false

    DateTime date = default;
    await Assert.That(date).IsDefault(); // DateTime.MinValue

    Guid id = default;
    await Assert.That(id).IsDefault(); // Guid.Empty
}
```

### IsNotDefault

Tests that a value is not the default value for its type:

```csharp
[Test]
public async Task Not_Default_Values()
{
    var name = "Alice";
    await Assert.That(name).IsNotDefault();

    var count = 42;
    await Assert.That(count).IsNotDefault();

    var isValid = true;
    await Assert.That(isValid).IsNotDefault();

    var id = Guid.NewGuid();
    await Assert.That(id).IsNotDefault();
}
```

## Reference Types vs Value Types

### Reference Type Defaults

For reference types, default equals `null`:

```csharp
[Test]
public async Task Reference_Type_Defaults()
{
    string? text = default;
    object? obj = default;
    Person? person = default;

    await Assert.That(text).IsDefault();   // Same as IsNull()
    await Assert.That(obj).IsDefault();    // Same as IsNull()
    await Assert.That(person).IsDefault(); // Same as IsNull()
}
```

### Value Type Defaults

For value types, default is the zero-initialized value:

```csharp
[Test]
public async Task Value_Type_Defaults()
{
    // Numeric types default to 0
    int intVal = default;
    await Assert.That(intVal).IsDefault();
    await Assert.That(intVal).IsEqualTo(0);

    double doubleVal = default;
    await Assert.That(doubleVal).IsDefault();
    await Assert.That(doubleVal).IsEqualTo(0.0);

    // Boolean defaults to false
    bool boolVal = default;
    await Assert.That(boolVal).IsDefault();
    await Assert.That(boolVal).IsFalse();

    // Struct defaults to all fields/properties at their defaults
    Point point = default;
    await Assert.That(point).IsDefault();
    await Assert.That(point).IsEqualTo(new Point(0, 0));
}
```

### Nullable Value Types

Nullable value types (`T?`) are reference types, so their default is `null`:

```csharp
[Test]
public async Task Nullable_Value_Type_Defaults()
{
    int? nullableInt = default;
    await Assert.That(nullableInt).IsDefault(); // Same as IsNull()
    await Assert.That(nullableInt).IsNull();    // Also works

    DateTime? nullableDate = default;
    await Assert.That(nullableDate).IsDefault();
    await Assert.That(nullableDate).IsNull();
}
```

## Practical Examples

### Optional Parameters and Returns

```csharp
[Test]
public async Task Optional_Return_Value()
{
    // API might return null if item not found
    var item = await _repository.FindByIdAsync("unknown-id");
    await Assert.That(item).IsNull();

    // API should return value if item exists
    var existing = await _repository.FindByIdAsync("valid-id");
    await Assert.That(existing).IsNotNull();
}
```

### Initialization Checks

```csharp
[Test]
public async Task Uninitialized_Field()
{
    var service = new MyService();

    // Before initialization
    await Assert.That(service.Connection).IsNull();

    await service.InitializeAsync();

    // After initialization
    await Assert.That(service.Connection).IsNotNull();
}
```

### Dependency Injection Validation

```csharp
[Test]
public async Task Constructor_Injection()
{
    var logger = new Mock<ILogger>();
    var service = new UserService(logger.Object);

    // Verify dependency was injected
    await Assert.That(service.Logger).IsNotNull();
}
```

### Lazy Initialization

```csharp
[Test]
public async Task Lazy_Property()
{
    var calculator = new ExpensiveCalculator();

    // Before first access
    await Assert.That(calculator.CachedResult).IsNull();

    var result = calculator.GetResult();

    // After first access - cached
    await Assert.That(calculator.CachedResult).IsNotNull();
}
```

## Checking Multiple Properties

Use `Assert.Multiple()` to check multiple null conditions:

```csharp
[Test]
public async Task Validate_All_Required_Fields()
{
    var user = CreateUser();

    await using (Assert.Multiple())
    {
        await Assert.That(user).IsNotNull();
        await Assert.That(user.FirstName).IsNotNull();
        await Assert.That(user.LastName).IsNotNull();
        await Assert.That(user.Email).IsNotNull();
        await Assert.That(user.CreatedDate).IsNotDefault();
    }
}
```

Or chain them:

```csharp
[Test]
public async Task Required_Fields_With_Chaining()
{
    var config = LoadConfiguration();

    await Assert.That(config.DatabaseConnection)
        .IsNotNull()
        .And.Member(c => c.Server).IsNotNull()
        .And.Member(c => c.Database).IsNotNull();
}
```

## Default Values for Custom Types

### Structs

```csharp
public struct Rectangle
{
    public int Width { get; init; }
    public int Height { get; init; }
}

[Test]
public async Task Struct_Default()
{
    Rectangle rect = default;

    await Assert.That(rect).IsDefault();
    await Assert.That(rect.Width).IsEqualTo(0);
    await Assert.That(rect.Height).IsEqualTo(0);
}
```

### Records

```csharp
public record Person(string Name, int Age);

[Test]
public async Task Record_Default()
{
    Person? person = default;
    await Assert.That(person).IsDefault(); // null for reference types
    await Assert.That(person).IsNull();
}

public record struct Point(int X, int Y);

[Test]
public async Task Record_Struct_Default()
{
    Point point = default;
    await Assert.That(point).IsDefault();
    await Assert.That(point.X).IsEqualTo(0);
    await Assert.That(point.Y).IsEqualTo(0);
}
```

## Special Cases

### Empty Collections vs Null

```csharp
[Test]
public async Task Empty_vs_Null()
{
    List<string>? nullList = null;
    List<string> emptyList = new();

    await Assert.That(nullList).IsNull();
    await Assert.That(emptyList).IsNotNull();
    await Assert.That(emptyList).IsEmpty(); // Not null, but empty
}
```

### Empty Strings vs Null

```csharp
[Test]
public async Task Empty_String_vs_Null()
{
    string? nullString = null;
    string emptyString = "";

    await Assert.That(nullString).IsNull();
    await Assert.That(emptyString).IsNotNull();
    await Assert.That(emptyString).IsEmpty(); // Not null, but empty
}
```

### Default GUID

```csharp
[Test]
public async Task GUID_Default()
{
    Guid id = default;

    await Assert.That(id).IsDefault();
    await Assert.That(id).IsEqualTo(Guid.Empty);
    await Assert.That(id).IsEmptyGuid(); // TUnit specific assertion
}
```

### Default DateTime

```csharp
[Test]
public async Task DateTime_Default()
{
    DateTime date = default;

    await Assert.That(date).IsDefault();
    await Assert.That(date).IsEqualTo(DateTime.MinValue);
}
```

## Combining with Other Assertions

### Null Coalescing Validation

```csharp
[Test]
public async Task Null_Coalescing_Default()
{
    string? input = GetOptionalInput();
    string result = input ?? "default";

    if (input == null)
    {
        await Assert.That(result).IsEqualTo("default");
    }
    else
    {
        await Assert.That(result).IsEqualTo(input);
    }
}
```

### Null Conditional Operator

```csharp
[Test]
public async Task Null_Conditional()
{
    Person? person = FindPerson("id");
    string? name = person?.Name;

    if (person == null)
    {
        await Assert.That(name).IsNull();
    }
    else
    {
        await Assert.That(name).IsNotNull();
    }
}
```

## Common Patterns

### Validate Required Dependencies

```csharp
[Test]
public async Task All_Dependencies_Provided()
{
    var service = CreateService();

    await Assert.That(service.Logger).IsNotNull();
    await Assert.That(service.Repository).IsNotNull();
    await Assert.That(service.Cache).IsNotNull();
}
```

### Validate Optional Features

```csharp
[Test]
public async Task Optional_Feature_Not_Enabled()
{
    var config = LoadConfiguration();

    if (!config.EnableAdvancedFeatures)
    {
        await Assert.That(config.AdvancedSettings).IsNull();
    }
}
```

### State Machine Validation

```csharp
[Test]
public async Task State_Transitions()
{
    var workflow = new Workflow();

    // Initial state
    await Assert.That(workflow.CurrentState).IsDefault();

    await workflow.StartAsync();

    // After start
    await Assert.That(workflow.CurrentState).IsNotDefault();
}
```

## See Also

- [Equality & Comparison](equality-and-comparison.md) - Comparing values including defaults
- [Boolean Assertions](boolean.md) - Testing true/false values
- [String Assertions](string.md) - String-specific null and empty checks
- [Collections](collections.md) - Collection null and empty checks
