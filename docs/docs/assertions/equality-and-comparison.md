---
sidebar_position: 2
---

# Equality and Comparison Assertions

TUnit provides comprehensive assertions for testing equality and comparing values. These assertions work with any type that implements the appropriate comparison interfaces.

## Basic Equality

### IsEqualTo

Tests that two values are equal using the type's `Equals()` method or `==` operator:

```csharp
[Test]
public async Task Basic_Equality()
{
    var result = 5 + 5;
    await Assert.That(result).IsEqualTo(10);

    var name = "Alice";
    await Assert.That(name).IsEqualTo("Alice");

    var isValid = true;
    await Assert.That(isValid).IsEqualTo(true);
}
```

### IsNotEqualTo

Tests that two values are not equal:

```csharp
[Test]
public async Task Not_Equal()
{
    var actual = CalculateResult();
    await Assert.That(actual).IsNotEqualTo(0);

    var username = GetUsername();
    await Assert.That(username).IsNotEqualTo("admin");
}
```

### EqualTo (Alias)

`EqualTo()` is an alias for `IsEqualTo()` for more natural chaining:

```csharp
[Test]
public async Task Using_EqualTo_Alias()
{
    var numbers = new[] { 1, 2, 3 };

    await Assert.That(numbers)
        .HasCount().EqualTo(3)
        .And.Contains(2);
}
```

## Reference Equality

### IsSameReferenceAs

Tests that two references point to the exact same object instance:

```csharp
[Test]
public async Task Same_Reference()
{
    var original = new Person { Name = "Alice" };
    var reference = original;

    await Assert.That(reference).IsSameReferenceAs(original);
}
```

### IsNotSameReferenceAs

Tests that two references point to different object instances:

```csharp
[Test]
public async Task Different_References()
{
    var person1 = new Person { Name = "Alice" };
    var person2 = new Person { Name = "Alice" };

    // Same values, different instances
    await Assert.That(person1).IsNotSameReferenceAs(person2);
    await Assert.That(person1).IsEqualTo(person2); // If equality is overridden
}
```

## Comparison Assertions

All comparison assertions work with types that implement `IComparable<T>` or `IComparable`.

### IsGreaterThan

```csharp
[Test]
public async Task Greater_Than()
{
    var score = 85;
    await Assert.That(score).IsGreaterThan(70);

    var temperature = 25.5;
    await Assert.That(temperature).IsGreaterThan(20.0);

    var date = DateTime.Now;
    await Assert.That(date).IsGreaterThan(DateTime.Now.AddDays(-1));
}
```

### IsGreaterThanOrEqualTo

```csharp
[Test]
public async Task Greater_Than_Or_Equal()
{
    var passingGrade = 60;
    await Assert.That(passingGrade).IsGreaterThanOrEqualTo(60);

    var age = 18;
    await Assert.That(age).IsGreaterThanOrEqualTo(18); // Exactly 18 passes
}
```

### IsLessThan

```csharp
[Test]
public async Task Less_Than()
{
    var response_time = 150; // milliseconds
    await Assert.That(response_time).IsLessThan(200);

    var price = 49.99m;
    await Assert.That(price).IsLessThan(50.00m);
}
```

### IsLessThanOrEqualTo

```csharp
[Test]
public async Task Less_Than_Or_Equal()
{
    var maxRetries = 3;
    var actualRetries = 3;
    await Assert.That(actualRetries).IsLessThanOrEqualTo(maxRetries);
}
```

## Range Assertions

### IsBetween

Tests that a value falls within a range (inclusive):

```csharp
[Test]
public async Task Between_Values()
{
    var percentage = 75;
    await Assert.That(percentage).IsBetween(0, 100);

    var temperature = 22.5;
    await Assert.That(temperature).IsBetween(20.0, 25.0);

    var age = 30;
    await Assert.That(age).IsBetween(18, 65);
}
```

Boundary values are included:

```csharp
[Test]
public async Task Between_Includes_Boundaries()
{
    await Assert.That(0).IsBetween(0, 10);    // ✅ Passes
    await Assert.That(10).IsBetween(0, 10);   // ✅ Passes
    await Assert.That(5).IsBetween(0, 10);    // ✅ Passes
}
```

## Numeric-Specific Assertions

### IsPositive

Tests that a numeric value is greater than zero:

```csharp
[Test]
public async Task Positive_Numbers()
{
    var profit = 1500.50m;
    await Assert.That(profit).IsPositive();

    var count = 5;
    await Assert.That(count).IsPositive();

    // Works with all numeric types
    await Assert.That(1.5).IsPositive();      // double
    await Assert.That(1.5f).IsPositive();     // float
    await Assert.That(1.5m).IsPositive();     // decimal
    await Assert.That((byte)1).IsPositive();  // byte
    await Assert.That((short)1).IsPositive(); // short
    await Assert.That(1L).IsPositive();       // long
}
```

### IsNegative

Tests that a numeric value is less than zero:

```csharp
[Test]
public async Task Negative_Numbers()
{
    var loss = -500.25m;
    await Assert.That(loss).IsNegative();

    var temperature = -5;
    await Assert.That(temperature).IsNegative();
}
```

## Tolerance for Floating-Point Numbers

When comparing floating-point numbers, you can specify a tolerance to account for rounding errors:

### Double Tolerance

```csharp
[Test]
public async Task Double_With_Tolerance()
{
    var actual = 1.0 / 3.0; // 0.333333...
    var expected = 0.333;

    // Without tolerance - might fail due to precision
    // await Assert.That(actual).IsEqualTo(expected);

    // With tolerance - passes
    await Assert.That(actual).IsEqualTo(expected).Within(0.001);
}
```

### Float Tolerance

```csharp
[Test]
public async Task Float_With_Tolerance()
{
    float actual = 3.14159f;
    float expected = 3.14f;

    await Assert.That(actual).IsEqualTo(expected).Within(0.01f);
}
```

### Decimal Tolerance

```csharp
[Test]
public async Task Decimal_With_Tolerance()
{
    decimal price = 19.995m;
    decimal expected = 20.00m;

    await Assert.That(price).IsEqualTo(expected).Within(0.01m);
}
```

### Long Tolerance

```csharp
[Test]
public async Task Long_With_Tolerance()
{
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    long expected = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // Allow 100ms difference
    await Assert.That(timestamp).IsEqualTo(expected).Within(100L);
}
```

## Chaining Comparisons

Combine multiple comparison assertions:

```csharp
[Test]
public async Task Chained_Comparisons()
{
    var score = 85;

    await Assert.That(score)
        .IsGreaterThan(0)
        .And.IsLessThan(100)
        .And.IsGreaterThanOrEqualTo(80);
}
```

Or use `IsBetween` for simpler range checks:

```csharp
[Test]
public async Task Range_Check_Simplified()
{
    var score = 85;

    // Instead of chaining IsGreaterThan and IsLessThan:
    await Assert.That(score).IsBetween(0, 100);
}
```

## Custom Equality Comparers

You can provide custom equality comparers for collections and complex types:

```csharp
[Test]
public async Task Custom_Comparer()
{
    var people1 = new[] { new Person("Alice"), new Person("Bob") };
    var people2 = new[] { new Person("ALICE"), new Person("BOB") };

    // Case-insensitive name comparison
    var comparer = new PersonNameComparer();

    await Assert.That(people1)
        .IsEquivalentTo(people2)
        .Using(comparer);
}

public class PersonNameComparer : IEqualityComparer<Person>
{
    public bool Equals(Person? x, Person? y) =>
        string.Equals(x?.Name, y?.Name, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode(Person obj) =>
        obj.Name?.ToLowerInvariant().GetHashCode() ?? 0;
}
```

Or use a predicate:

```csharp
[Test]
public async Task Custom_Equality_Predicate()
{
    var people1 = new[] { new Person("Alice"), new Person("Bob") };
    var people2 = new[] { new Person("ALICE"), new Person("BOB") };

    await Assert.That(people1)
        .IsEquivalentTo(people2)
        .Using((p1, p2) => string.Equals(p1.Name, p2.Name,
                          StringComparison.OrdinalIgnoreCase));
}
```

## Working with Value Types and Records

Equality works naturally with value types and records:

```csharp
public record Point(int X, int Y);

[Test]
public async Task Record_Equality()
{
    var point1 = new Point(10, 20);
    var point2 = new Point(10, 20);

    // Records have built-in value equality
    await Assert.That(point1).IsEqualTo(point2);
    await Assert.That(point1).IsNotSameReferenceAs(point2);
}
```

```csharp
public struct Coordinate
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

[Test]
public async Task Struct_Equality()
{
    var coord1 = new Coordinate { Latitude = 47.6, Longitude = -122.3 };
    var coord2 = new Coordinate { Latitude = 47.6, Longitude = -122.3 };

    await Assert.That(coord1).IsEqualTo(coord2);
}
```

## Practical Examples

### Validating Calculation Results

```csharp
[Test]
public async Task Calculate_Discount()
{
    var originalPrice = 100m;
    var discount = 0.20m; // 20%

    var finalPrice = originalPrice * (1 - discount);

    await Assert.That(finalPrice).IsEqualTo(80m);
    await Assert.That(finalPrice).IsLessThan(originalPrice);
    await Assert.That(finalPrice).IsGreaterThan(0);
}
```

### Validating Ranges

```csharp
[Test]
public async Task Temperature_In_Valid_Range()
{
    var roomTemperature = GetRoomTemperature();

    await Assert.That(roomTemperature)
        .IsBetween(18, 26) // Comfortable range in Celsius
        .And.IsPositive();
}
```

### Comparing with Mathematical Constants

```csharp
[Test]
public async Task Mathematical_Constants()
{
    var calculatedPi = CalculatePiUsingLeibniz(10000);

    await Assert.That(calculatedPi).IsEqualTo(Math.PI).Within(0.0001);
}
```

### API Response Validation

```csharp
[Test]
public async Task API_Response_Time()
{
    var stopwatch = Stopwatch.StartNew();
    await CallApiEndpoint();
    stopwatch.Stop();

    await Assert.That(stopwatch.ElapsedMilliseconds)
        .IsLessThan(500) // Must respond within 500ms
        .And.IsGreaterThan(0);
}
```

## Common Patterns

### Validating User Input

```csharp
[Test]
public async Task Username_Length()
{
    var username = GetUserInput();

    await Assert.That(username.Length)
        .IsBetween(3, 20)
        .And.IsGreaterThan(0);
}
```

### Percentage Validation

```csharp
[Test]
public async Task Percentage_Valid()
{
    var successRate = CalculateSuccessRate();

    await Assert.That(successRate)
        .IsBetween(0, 100)
        .And.IsGreaterThanOrEqualTo(0);
}
```

## See Also

- [Numeric Assertions](numeric.md) - Additional numeric-specific assertions
- [DateTime Assertions](datetime.md) - Time-based comparisons with tolerance
- [Collections](collections.md) - Comparing collections
- [Strings](string.md) - String equality with options
