# Awaiting

In TUnit you `await` your assertions, and this serves two purposes:
- the `await` keyword is responsible for performing the assertion, before you call await we're building a chain of assertion rules.
- it allows executing and asserting on `async` delegates without performing sync-over-async

Because of this, your tests should be `async` and return a `Task`.

Don't worry about forgetting to `await` - There's an analyzer built in that will notify you if you've missed any!  
If you forget to `await`, your assertion will not actually be executed, and your test may pass when it should fail.

This will error:

```csharp
    [Test]
    public void MyTest()
    {
        var result = Add(1, 2);

        Assert.That(result).IsEqualTo(3);
    }
```

This won't: 

```csharp
    [Test]
    public async Task MyTest()
    {
        var result = Add(1, 2);

        await Assert.That(result).IsEqualTo(3);
    }
```

TUnit is able to take in asynchronous delegates. To be able to assert on these, we need to execute the code. We want to avoid sync-over-async, as this can cause problems and block the thread pool, slowing down your test suite.
And with how fast .NET has become, the overhead of `Task`s and `async` methods shouldn't be noticeable.

## Complex Assertion Examples

### Chaining Multiple Assertions

You can chain multiple assertions together for more complex validations:

```csharp
[Test]
public async Task ComplexObjectValidation()
{
    var user = await GetUserAsync("john.doe");

    // Chain multiple member assertions
    await Assert.That(user)
        .IsNotNull()
        .And.Member(u => u.Email, email => email.IsEqualTo("john.doe@example.com"))
        .And.Member(u => u.Age, age => age.IsGreaterThan(18))
        .And.Member(u => u.Roles, roles => roles.Contains("Admin"));
}
```

### Collection Assertions with Complex Conditions

```csharp
[Test]
public async Task ComplexCollectionAssertions()
{
    var orders = await GetOrdersAsync();

    // Assert multiple conditions on a collection
    await Assert.That(orders)
        .HasCount().IsGreaterThan(0)
        .And.Contains(o => o.Status == OrderStatus.Completed)
        .And.DoesNotContain(o => o.Total < 0)
        .And.HasDistinctItems();

    // Assert on filtered subset
    var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed);
    await Assert.That(completedOrders)
        .All(o => o.CompletedDate != null)
        .And.Any(o => o.Total > 1000);
}
```

### Async Operation Assertions

```csharp
[Test]
public async Task AsyncOperationAssertions()
{
    // Assert that async operation completes within time limit
    await Assert.That(async () => await LongRunningOperationAsync())
        .CompletesWithin(TimeSpan.FromSeconds(5));

    // Assert that async operation throws specific exception
    await Assert.That(async () => await RiskyOperationAsync())
        .Throws<InvalidOperationException>()
        .WithMessageContaining("connection failed");

    // Assert on result of async operation
    var result = await CalculateAsync(10, 20);
    await Assert.That(result).IsEqualTo(30);
}
```

### Exception Assertions with Details

```csharp
[Test]
public async Task DetailedExceptionAssertions()
{
    var invalidData = new { Id = -1, Name = "" };

    // Assert exception with specific message
    await Assert.That(() => ProcessDataAsync(invalidData))
        .Throws<ValidationException>()
        .WithMessage("Validation failed");

    // Assert ArgumentException with parameter name
    await Assert.That(() => ProcessInvalidData(null))
        .Throws<ArgumentException>()
        .WithParameterName("data");

    // Assert aggregate exception
    var exception = await Assert.That(() => ParallelOperationAsync())
        .Throws<AggregateException>();

    await Assert.That(exception.InnerExceptions).HasCount(3);
    await Assert.That(exception.InnerExceptions).All(e => e is TaskCanceledException);
}
```

### Custom Assertion Conditions

```csharp
[Test]
public async Task CustomAssertionConditions()
{
    var measurements = await GetMeasurementsAsync();
    
    // Use custom conditions for complex validations
    await Assert.That(measurements)
        .Satisfies(m => {
            var average = m.Average();
            var stdDev = CalculateStandardDeviation(m);
            return stdDev < average * 0.1; // Less than 10% deviation
        }, "Measurements should have low standard deviation");
    
    // Combine built-in and custom assertions
    await Assert.That(measurements)
        .HasCount().GreaterThan(100)
        .And.All(m => m > 0)
        .And.Satisfies(IsNormallyDistributed, "Data should be normally distributed");
}
```

### DateTime and TimeSpan Assertions

```csharp
[Test]
public async Task DateTimeAssertions()
{
    var order = await CreateOrderAsync();

    // Complex datetime assertions
    await Assert.That(order.CreatedAt)
        .IsGreaterThan(DateTime.UtcNow.AddMinutes(-1))
        .And.IsLessThan(DateTime.UtcNow.AddMinutes(1));

    // TimeSpan assertions
    var processingTime = order.CompletedAt - order.CreatedAt;
    await Assert.That(processingTime)
        .IsLessThan(TimeSpan.FromMinutes(5))
        .And.IsGreaterThan(TimeSpan.Zero);
}
```

### Floating Point Comparisons

```csharp
[Test]
public async Task FloatingPointAssertions()
{
    var calculations = await PerformComplexCalculationsAsync();

    // Use tolerance for floating point comparisons
    await Assert.That(calculations.Pi)
        .IsEqualTo(Math.PI).Within(0.0001);

    // Assert on collections of floating point numbers
    await Assert.That(calculations.Results)
        .All(r => Math.Abs(r) < 1000000); // No overflow

    // Check for approximate value in collection
    var hasApproximate42 = calculations.Results.Any(r => Math.Abs(r - 42.0) < 0.1);
    await Assert.That(hasApproximate42).IsTrue();

    // Assert on sum with tolerance
    var sum = calculations.Results.Sum();
    await Assert.That(sum).IsEqualTo(expectedSum).Within(0.01);
}
```

### String Pattern Matching

```csharp
[Test]
public async Task StringPatternAssertions()
{
    var logs = await GetLogEntriesAsync();

    // Complex string assertions
    await Assert.That(logs)
        .All(log => Regex.IsMatch(log, @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\]"))
        .And.Any(log => log.Contains("ERROR"))
        .And.DoesNotContain(log => log.Contains("SENSITIVE_DATA"));

    // Assert on formatted output
    var report = await GenerateReportAsync();
    await Assert.That(report)
        .StartsWith("Report Generated:")
        .And.Contains("Total Items:")
        .And.DoesNotContain("null")
        .And.HasLength().IsBetween(1000, 5000);
}
```

### Combining Or and And Conditions

```csharp
[Test]
public async Task ComplexLogicalConditions()
{
    var product = await GetProductAsync();

    // Complex logical combinations
    await Assert.That(product.Status)
        .IsEqualTo(ProductStatus.Active)
        .Or.IsEqualTo(ProductStatus.Pending);

    await Assert.That(product.Price)
        .IsGreaterThan(0)
        .And.IsLessThan(10000);

    // Category-based conditional checks
    if (product.Category == "Electronics")
    {
        await Assert.That(product.Warranty).IsNotNull();
    }
    else if (product.Category == "Books")
    {
        await Assert.That(product.ISBN).IsNotNull();
    }
}
```

### Performance Assertions

```csharp
[Test]
public async Task PerformanceAssertions()
{
    var stopwatch = Stopwatch.StartNew();
    var results = new List<long>();
    
    // Measure multiple operations
    for (int i = 0; i < 100; i++)
    {
        var start = stopwatch.ElapsedMilliseconds;
        await PerformOperationAsync();
        results.Add(stopwatch.ElapsedMilliseconds - start);
    }
    
    // Assert on performance metrics
    await Assert.That(results.Average())
        .IsLessThan(100); // Average under 100ms
    
    await Assert.That(results.Max())
        .IsLessThan(500); // No operation over 500ms
    
    await Assert.That(results.Where(r => r > 200).HasCount())
        .IsLessThan(5); // Less than 5% over 200ms
}
```

### State Machine Assertions

```csharp
[Test]
public async Task StateMachineAssertions()
{
    var workflow = new OrderWorkflow();

    // Initial state
    await Assert.That(workflow.State).IsEqualTo(OrderState.New);

    // State transition assertions
    await workflow.StartProcessing();
    await Assert.That(workflow.State).IsEqualTo(OrderState.Processing);
    await Assert.That(workflow.CanTransitionTo(OrderState.Completed)).IsTrue();
    await Assert.That(workflow.CanTransitionTo(OrderState.New)).IsFalse();

    // Complex workflow validation
    await workflow.Complete();
    await Assert.That(workflow.State).IsEqualTo(OrderState.Completed);
    await Assert.That(workflow.CompletedAt).IsNotNull();
    await Assert.That(workflow.History).Contains(h => h.State == OrderState.Processing);
}
```

These examples demonstrate the power and flexibility of TUnit's assertion system, showing how you can build complex, readable assertions for various testing scenarios.
