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

## Using Return Values from Awaited Assertions

When you `await` an assertion in TUnit, it returns a reference to the subject that was asserted on. This allows you to capture the validated value and use it in subsequent operations or assertions, creating a fluent and readable test flow.

This is particularly useful when you want to:
- Chain related assertions on the same value
- Use a validated value in further test logic
- Avoid redundant variable assignments
- Create more expressive tests

### Basic Return Value Usage

The simplest case is capturing the subject after asserting on it:

```csharp
[Test]
public async Task ReturnValue_BasicUsage()
{
    // The assertion returns the subject being asserted on
    int result = await Assert.That(42).IsGreaterThan(0);
    
    // You can now use 'result' which equals 42
    await Assert.That(result * 2).IsEqualTo(84);
}
```

### Practical Example: API Response Validation

```csharp
[Test]
public async Task ValidateAndUseApiResponse()
{
    var response = await _httpClient.GetAsync("/api/users/123");
    
    // Assert on status code and capture response for further use
    var validatedResponse = await Assert.That(response.StatusCode)
        .IsEqualTo(HttpStatusCode.OK);
    
    // Deserialize and validate user data
    var user = await response.Content.ReadFromJsonAsync<User>();
    var validatedUser = await Assert.That(user).IsNotNull();
    
    // Now use the validated user object with confidence
    await Assert.That(validatedUser.Email).Contains("@");
    await Assert.That(validatedUser.Id).IsEqualTo(123);
}
```

### Chaining Operations on Validated Values

```csharp
[Test]
public async Task ProcessValidatedConfiguration()
{
    var config = LoadConfiguration();
    
    // Validate and capture the timeout value
    int timeout = await Assert.That(config.TimeoutSeconds)
        .IsGreaterThan(0)
        .And.IsLessThan(300);
    
    // Use validated timeout in actual operation
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
    var result = await PerformOperationAsync(cts.Token);
    
    await Assert.That(result).IsNotNull();
}
```

### Type Conversion with Return Values

```csharp
[Test]
public async Task ParseAndValidateInput()
{
    string input = "12345";
    
    // Parse string to int and validate in one expression
    int parsedValue = await Assert.That(input)
        .HasLength().IsBetween(1, 10)
        .And.WhenParsedInto<int>()
        .IsGreaterThan(1000);
    
    // Use the parsed and validated value
    var doubled = parsedValue * 2;
    await Assert.That(doubled).IsEqualTo(24690);
}
```

### Exception Handling with Return Values

```csharp
[Test]
public async Task CaptureAndInspectException()
{
    // Capture the thrown exception for detailed inspection
    var exception = await Assert.That(() => DivideByZero(10, 0))
        .Throws<DivideByZeroException>();
    
    // Inspect exception properties
    await Assert.That(exception.Message).IsNotEmpty();
    await Assert.That(exception.StackTrace).Contains("DivideByZero");
    
    // Could log or analyze the exception further
    Console.WriteLine($"Caught expected exception: {exception.GetType().Name}");
}
```

### Collection Filtering with Return Values

```csharp
[Test]
public async Task FindAndValidateCollectionItem()
{
    var products = GetProducts();
    
    // Find a specific product and validate it exists
    var expensiveProduct = await Assert.That(products)
        .Contains(p => p.Price > 1000);
    
    // Use the found product for further assertions
    await Assert.That(expensiveProduct.Name).IsNotNull();
    await Assert.That(expensiveProduct.Category).IsEqualTo("Premium");
    await Assert.That(expensiveProduct.InStock).IsTrue();
    
    // Can even use it in business logic
    var discount = CalculateDiscount(expensiveProduct);
    await Assert.That(discount).IsGreaterThan(0);
}
```

### Complex Workflow Validation

```csharp
[Test]
public async Task ValidateMultiStepProcess()
{
    var order = CreateOrder();
    
    // Validate initial state and capture order
    var validatedOrder = await Assert.That(order.Status)
        .IsEqualTo(OrderStatus.Pending);
    
    // Process the order
    await ProcessOrderAsync(order);
    
    // Validate updated state
    var completedOrder = await Assert.That(order.Status)
        .IsEqualTo(OrderStatus.Completed);
    
    // Use validated order for final checks
    await Assert.That(order.ProcessedDate).IsNotNull();
    await Assert.That(order.ProcessedDate.Value)
        .IsGreaterThan(DateTime.UtcNow.AddMinutes(-1));
}
```

### Combining Multiple Return Values

```csharp
[Test]
public async Task ValidateRelatedObjects()
{
    var customer = await GetCustomerAsync(customerId: 42);
    
    // Validate customer and capture
    var validCustomer = await Assert.That(customer).IsNotNull();
    
    // Validate nested property and capture
    var address = await Assert.That(validCustomer.Address).IsNotNull();
    
    // Validate address fields using captured references
    await Assert.That(address.ZipCode).HasLength(5);
    await Assert.That(address.Country).IsEqualTo("USA");
    
    // Use both validated objects together
    var fullAddress = $"{validCustomer.Name}, {address.Street}, {address.City}";
    await Assert.That(fullAddress).Contains(validCustomer.Name);
}
```

### Type Casting with Confidence

```csharp
[Test]
public async Task CastAndUseSpecificType()
{
    object shape = new Circle { Radius = 5.0 };
    
    // Assert type and capture strongly-typed reference
    var circle = await Assert.That(shape).IsTypeOf<Circle>();
    
    // Now you can use circle-specific properties without casting
    await Assert.That(circle.Radius).IsEqualTo(5.0);
    
    var area = Math.PI * circle.Radius * circle.Radius;
    await Assert.That(area).IsEqualTo(Math.PI * 25).Within(0.0001);
}
```

### Best Practices

When using return values from assertions:

1. **Use descriptive variable names**: The returned value should have a meaningful name that indicates it's been validated
2. **Chain related assertions**: When asserting on the same value multiple times, use `.And` to chain assertions
3. **Leverage type conversions**: Use `WhenParsedInto<T>()` and `IsTypeOf<T>()` to get strongly-typed values
4. **Keep it readable**: Don't create overly complex chains - break into multiple assertions if needed
5. **Remember it's a reference**: The returned value is the same reference as the input (not a copy), so modifications affect the original

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
