# Numeric Assertions

TUnit provides comprehensive assertions for testing numeric values, including specialized assertions for positive/negative values and comparison assertions with tolerance support.

## Sign Assertions[​](#sign-assertions "Direct link to Sign Assertions")

### IsPositive[​](#ispositive "Direct link to IsPositive")

Tests that a numeric value is greater than zero:

```
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

```
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

### IsNegative[​](#isnegative "Direct link to IsNegative")

Tests that a numeric value is less than zero:

```
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

### Zero is Neither Positive Nor Negative[​](#zero-is-neither-positive-nor-negative "Direct link to Zero is Neither Positive Nor Negative")

```
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

## Comparison Assertions[​](#comparison-assertions "Direct link to Comparison Assertions")

All comparison operators work with numeric types. See [Equality and Comparison](/docs/assertions/equality-and-comparison.md) for full details.

### Quick Reference[​](#quick-reference "Direct link to Quick Reference")

```
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

## Tolerance for Floating-Point Numbers[​](#tolerance-for-floating-point-numbers "Direct link to Tolerance for Floating-Point Numbers")

Floating-point arithmetic can introduce rounding errors. Use tolerance for safe comparisons:

### Double Tolerance[​](#double-tolerance "Direct link to Double Tolerance")

```
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

### Float Tolerance[​](#float-tolerance "Direct link to Float Tolerance")

```
[Test]

public async Task Float_Tolerance()

{

    float pi = 3.14159f;

    float approximation = 3.14f;



    await Assert.That(pi).IsEqualTo(approximation).Within(0.01f);

}
```

### Decimal Tolerance[​](#decimal-tolerance "Direct link to Decimal Tolerance")

Useful for monetary calculations:

```
[Test]

public async Task Decimal_Tolerance()

{

    decimal price = 19.995m;

    decimal rounded = 20.00m;



    await Assert.That(price).IsEqualTo(rounded).Within(0.01m);

}
```

### Long Tolerance[​](#long-tolerance "Direct link to Long Tolerance")

For timestamp or large number comparisons:

```
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

## Practical Examples[​](#practical-examples "Direct link to Practical Examples")

### Financial Calculations[​](#financial-calculations "Direct link to Financial Calculations")

```
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

### Temperature Conversions[​](#temperature-conversions "Direct link to Temperature Conversions")

```
[Test]

public async Task Celsius_To_Fahrenheit()

{

    double celsius = 20.0;

    double fahrenheit = celsius * 9.0 / 5.0 + 32.0;



    await Assert.That(fahrenheit).IsEqualTo(68.0).Within(0.1);

    await Assert.That(fahrenheit).IsGreaterThan(celsius);

}
```

### Percentage Calculations[​](#percentage-calculations "Direct link to Percentage Calculations")

```
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

### Statistical Calculations[​](#statistical-calculations "Direct link to Statistical Calculations")

```
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

## Range Validation[​](#range-validation "Direct link to Range Validation")

### Valid Range Checks[​](#valid-range-checks "Direct link to Valid Range Checks")

```
[Test]

public async Task Validate_Age()

{

    int age = 25;



    await Assert.That(age).IsBetween(0, 120);

    await Assert.That(age).IsGreaterThanOrEqualTo(0);

}
```

### Percentage Range[​](#percentage-range "Direct link to Percentage Range")

```
[Test]

public async Task Validate_Percentage()

{

    double successRate = 87.5;



    await Assert.That(successRate).IsBetween(0, 100);

    await Assert.That(successRate).IsPositive();

}
```

### Score Validation[​](#score-validation "Direct link to Score Validation")

```
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

## Chaining Numeric Assertions[​](#chaining-numeric-assertions "Direct link to Chaining Numeric Assertions")

```
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

## Nullable Numeric Types[​](#nullable-numeric-types "Direct link to Nullable Numeric Types")

```
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

## Special Floating-Point Values[​](#special-floating-point-values "Direct link to Special Floating-Point Values")

### Infinity[​](#infinity "Direct link to Infinity")

```
[Test]

public async Task Infinity_Checks()

{

    double positiveInfinity = double.PositiveInfinity;

    double negativeInfinity = double.NegativeInfinity;



    await Assert.That(positiveInfinity).IsEqualTo(double.PositiveInfinity);

    await Assert.That(negativeInfinity).IsEqualTo(double.NegativeInfinity);

}
```

### NaN (Not a Number)[​](#nan-not-a-number "Direct link to NaN (Not a Number)")

```
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

## Performance Metrics[​](#performance-metrics "Direct link to Performance Metrics")

```
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

## Common Patterns[​](#common-patterns "Direct link to Common Patterns")

### Boundary Testing[​](#boundary-testing "Direct link to Boundary Testing")

```
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

### Growth Rate Validation[​](#growth-rate-validation "Direct link to Growth Rate Validation")

```
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

### Ratio Calculations[​](#ratio-calculations "Direct link to Ratio Calculations")

```
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

## See Also[​](#see-also "Direct link to See Also")

* [Equality & Comparison](/docs/assertions/equality-and-comparison.md) - General comparison assertions
* [DateTime Assertions](/docs/assertions/datetime.md) - Time-based numeric values with tolerance
* [Collections](/docs/assertions/collections.md) - Numeric operations on collections (Count, Sum, Average)
