---
sidebar_position: 4.5
---

# Numeric Assertions

TUnit provides comprehensive assertions for testing numeric values, including specialized assertions for positive/negative values and comparison assertions with tolerance support.

## Sign Assertions

### IsPositive

Tests that a numeric value is greater than zero:

```csharp
[Test]
public async Task Positive_Values()
{
    var profit = 1500m;
    await Assert.That(profit).IsPositive();

    var count = 5;
    await Assert.That(count).IsPositive();

    var rating = 4.5;
    await Assert.That(rating).IsPositive();
}
```

Works with all numeric types:

```csharp
[Test]
public async Task All_Numeric_Types()
{
    // Integers
    await Assert.That(1).IsPositive();           // int
    await Assert.That(1L).IsPositive();          // long
    await Assert.That((short)1).IsPositive();    // short
    await Assert.That((byte)1).IsPositive();     // byte

    // Floating point
    await Assert.That(1.5).IsPositive();         // double
    await Assert.That(1.5f).IsPositive();        // float
    await Assert.That(1.5m).IsPositive();        // decimal
}
```

### IsNegative

Tests that a numeric value is less than zero:

```csharp
[Test]
public async Task Negative_Values()
{
    var loss = -250.50m;
    await Assert.That(loss).IsNegative();

    var temperature = -5;
    await Assert.That(temperature).IsNegative();

    var delta = -0.001;
    await Assert.That(delta).IsNegative();
}
```

### Zero is Neither Positive Nor Negative

```csharp
[Test]
public async Task Zero_Checks()
{
    var zero = 0;

    // These will both fail:
    // await Assert.That(zero).IsPositive();  // ❌ Fails
    // await Assert.That(zero).IsNegative();  // ❌ Fails

    // Instead, check for zero explicitly:
    await Assert.That(zero).IsEqualTo(0);
}
```

## Comparison Assertions

All comparison operators work with numeric types. See [Equality and Comparison](equality-and-comparison.md) for full details.

### Quick Reference

```csharp
[Test]
public async Task Numeric_Comparisons()
{
    var value = 42;

    await Assert.That(value).IsGreaterThan(40);
    await Assert.That(value).IsGreaterThanOrEqualTo(42);
    await Assert.That(value).IsLessThan(50);
    await Assert.That(value).IsLessThanOrEqualTo(42);
    await Assert.That(value).IsBetween(0, 100);
}
```

## Tolerance for Floating-Point Numbers

Floating-point arithmetic can introduce rounding errors. Use tolerance for safe comparisons:

### Double Tolerance

```csharp
[Test]
public async Task Double_Tolerance()
{
    double result = 1.0 / 3.0;  // 0.33333333...
    double expected = 0.333;

    // Without tolerance - might fail
    // await Assert.That(result).IsEqualTo(expected);

    // With tolerance - safe
    await Assert.That(result).IsEqualTo(expected).Within( 0.001);
}
```

### Float Tolerance

```csharp
[Test]
public async Task Float_Tolerance()
{
    float pi = 3.14159f;
    float approximation = 3.14f;

    await Assert.That(pi).IsEqualTo(approximation).Within(0.01f);
}
```

### Decimal Tolerance

Useful for monetary calculations:

```csharp
[Test]
public async Task Decimal_Tolerance()
{
    decimal price = 19.995m;
    decimal rounded = 20.00m;

    await Assert.That(price).IsEqualTo(rounded).Within(0.01m);
}
```

### Long Tolerance

For timestamp or large number comparisons:

```csharp
[Test]
public async Task Long_Tolerance()
{
    long timestamp1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    await Task.Delay(50);
    long timestamp2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // Allow 100ms difference
    await Assert.That(timestamp1).IsEqualTo(timestamp2).Within(100L);
}
```

## Practical Examples

### Financial Calculations

```csharp
[Test]
public async Task Calculate_Total_Price()
{
    decimal unitPrice = 9.99m;
    int quantity = 3;
    decimal tax = 0.08m; // 8%

    decimal subtotal = unitPrice * quantity;
    decimal total = subtotal * (1 + tax);

    await Assert.That(total).IsPositive();
    await Assert.That(total).IsGreaterThan(subtotal);
    await Assert.That(total).IsEqualTo(32.37m).Within(0.01m);
}
```

### Temperature Conversions

```csharp
[Test]
public async Task Celsius_To_Fahrenheit()
{
    double celsius = 20.0;
    double fahrenheit = celsius * 9.0 / 5.0 + 32.0;

    await Assert.That(fahrenheit).IsEqualTo(68.0).Within(0.1);
    await Assert.That(fahrenheit).IsGreaterThan(celsius);
}
```

### Percentage Calculations

```csharp
[Test]
public async Task Calculate_Percentage()
{
    int total = 200;
    int passed = 175;
    double percentage = (double)passed / total * 100;

    await Assert.That(percentage).IsPositive();
    await Assert.That(percentage).IsBetween(0, 100);
    await Assert.That(percentage).IsEqualTo(87.5).Within(0.1);
}
```

### Statistical Calculations

```csharp
[Test]
public async Task Calculate_Average()
{
    var numbers = new[] { 10, 20, 30, 40, 50 };
    double average = numbers.Average();

    await Assert.That(average).IsEqualTo(30.0).Within(0.01);
    await Assert.That(average).IsGreaterThan(numbers.Min());
    await Assert.That(average).IsLessThan(numbers.Max());
}
```

## Range Validation

### Valid Range Checks

```csharp
[Test]
public async Task Validate_Age()
{
    int age = 25;

    await Assert.That(age).IsBetween(0, 120);
    await Assert.That(age).IsGreaterThanOrEqualTo(0);
}
```

### Percentage Range

```csharp
[Test]
public async Task Validate_Percentage()
{
    double successRate = 87.5;

    await Assert.That(successRate).IsBetween(0, 100);
    await Assert.That(successRate).IsPositive();
}
```

### Score Validation

```csharp
[Test]
public async Task Validate_Score()
{
    int score = 85;
    int minPassing = 60;
    int maxScore = 100;

    await Assert.That(score).IsBetween(minPassing, maxScore);
    await Assert.That(score).IsGreaterThanOrEqualTo(minPassing);
}
```

## Mathematical Operations

### Addition

```csharp
[Test]
public async Task Addition()
{
    var result = 5 + 3;

    await Assert.That(result).IsEqualTo(8);
    await Assert.That(result).IsPositive();
    await Assert.That(result).IsGreaterThan(5);
}
```

### Subtraction

```csharp
[Test]
public async Task Subtraction()
{
    var result = 10 - 3;

    await Assert.That(result).IsEqualTo(7);
    await Assert.That(result).IsPositive();
}
```

### Multiplication

```csharp
[Test]
public async Task Multiplication()
{
    var result = 4 * 5;

    await Assert.That(result).IsEqualTo(20);
    await Assert.That(result).IsPositive();
}
```

### Division

```csharp
[Test]
public async Task Division()
{
    double result = 10.0 / 4.0;

    await Assert.That(result).IsEqualTo(2.5).Within(0.001);
    await Assert.That(result).IsPositive();
}
```

### Modulo

```csharp
[Test]
public async Task Modulo()
{
    var result = 17 % 5;

    await Assert.That(result).IsEqualTo(2);
    await Assert.That(result).IsGreaterThanOrEqualTo(0);
    await Assert.That(result).IsLessThan(5);
}
```

## Working with Math Library

### Rounding

```csharp
[Test]
public async Task Math_Round()
{
    double value = 3.7;
    double rounded = Math.Round(value);

    await Assert.That(rounded).IsEqualTo(4.0).Within(0.001);
}
```

### Ceiling and Floor

```csharp
[Test]
public async Task Math_Ceiling_Floor()
{
    double value = 3.2;

    double ceiling = Math.Ceiling(value);
    await Assert.That(ceiling).IsEqualTo(4.0);

    double floor = Math.Floor(value);
    await Assert.That(floor).IsEqualTo(3.0);
}
```

### Absolute Value

```csharp
[Test]
public async Task Math_Abs()
{
    int negative = -42;
    int positive = Math.Abs(negative);

    await Assert.That(positive).IsPositive();
    await Assert.That(positive).IsEqualTo(42);
}
```

### Power and Square Root

```csharp
[Test]
public async Task Math_Power_Sqrt()
{
    double squared = Math.Pow(5, 2);
    await Assert.That(squared).IsEqualTo(25.0).Within(0.001);

    double root = Math.Sqrt(25);
    await Assert.That(root).IsEqualTo(5.0).Within(0.001);
}
```

### Trigonometry

```csharp
[Test]
public async Task Math_Trigonometry()
{
    double angle = Math.PI / 4; // 45 degrees
    double sine = Math.Sin(angle);

    await Assert.That(sine).IsEqualTo(Math.Sqrt(2) / 2).Within(0.0001);
    await Assert.That(sine).IsPositive();
    await Assert.That(sine).IsBetween(0, 1);
}
```

## Increment and Decrement

```csharp
[Test]
public async Task Increment_Decrement()
{
    int counter = 0;

    counter++;
    await Assert.That(counter).IsEqualTo(1);
    await Assert.That(counter).IsPositive();

    counter--;
    await Assert.That(counter).IsEqualTo(0);

    counter--;
    await Assert.That(counter).IsEqualTo(-1);
    await Assert.That(counter).IsNegative();
}
```

## Chaining Numeric Assertions

```csharp
[Test]
public async Task Chained_Numeric_Assertions()
{
    int score = 85;

    await Assert.That(score)
        .IsPositive()
        .And.IsGreaterThan(70)
        .And.IsLessThan(100)
        .And.IsBetween(80, 90);
}
```

## Nullable Numeric Types

```csharp
[Test]
public async Task Nullable_Numerics()
{
    int? value = 42;

    await Assert.That(value).IsNotNull();
    await Assert.That(value).IsEqualTo(42);
    await Assert.That(value).IsPositive();
}

[Test]
public async Task Nullable_Null()
{
    int? value = null;

    await Assert.That(value).IsNull();
}
```

## Special Floating-Point Values

### Infinity

```csharp
[Test]
public async Task Infinity_Checks()
{
    double positiveInfinity = double.PositiveInfinity;
    double negativeInfinity = double.NegativeInfinity;

    await Assert.That(positiveInfinity).IsEqualTo(double.PositiveInfinity);
    await Assert.That(negativeInfinity).IsEqualTo(double.NegativeInfinity);
}
```

### NaN (Not a Number)

```csharp
[Test]
public async Task NaN_Checks()
{
    double nan = double.NaN;

    // NaN is never equal to itself
    await Assert.That(double.IsNaN(nan)).IsTrue();

    // Can't use IsEqualTo with NaN
    // await Assert.That(nan).IsEqualTo(double.NaN); // ❌ Won't work
}
```

## Performance Metrics

```csharp
[Test]
public async Task Response_Time_Check()
{
    var stopwatch = Stopwatch.StartNew();
    await PerformOperationAsync();
    stopwatch.Stop();

    long milliseconds = stopwatch.ElapsedMilliseconds;

    await Assert.That(milliseconds).IsPositive();
    await Assert.That(milliseconds).IsLessThan(1000); // Under 1 second
}
```

## Common Patterns

### Boundary Testing

```csharp
[Test]
public async Task Boundary_Values()
{
    int min = int.MinValue;
    int max = int.MaxValue;

    await Assert.That(min).IsNegative();
    await Assert.That(max).IsPositive();
    await Assert.That(min).IsLessThan(max);
}
```

### Growth Rate Validation

```csharp
[Test]
public async Task Growth_Rate()
{
    decimal previousValue = 100m;
    decimal currentValue = 125m;
    decimal growthRate = (currentValue - previousValue) / previousValue * 100;

    await Assert.That(growthRate).IsPositive();
    await Assert.That(growthRate).IsEqualTo(25m).Within(0.1m);
}
```

### Ratio Calculations

```csharp
[Test]
public async Task Success_Ratio()
{
    int successful = 85;
    int total = 100;
    double ratio = (double)successful / total;

    await Assert.That(ratio).IsPositive();
    await Assert.That(ratio).IsBetween(0, 1);
    await Assert.That(ratio).IsGreaterThan(0.8); // 80% threshold
}
```

## See Also

- [Equality & Comparison](equality-and-comparison.md) - General comparison assertions
- [DateTime Assertions](datetime.md) - Time-based numeric values with tolerance
- [Collections](collections.md) - Numeric operations on collections (Count, Sum, Average)
