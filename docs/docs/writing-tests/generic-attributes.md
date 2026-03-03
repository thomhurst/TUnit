# Generic Attributes

TUnit provides generic versions of several attributes that offer enhanced type safety and better IDE support. These attributes allow you to specify types at compile time, reducing errors and improving code maintainability.

## Generic Test Attributes

### MethodDataSourceAttribute&lt;T&gt;

The generic version of `MethodDataSource` provides type safety for the class containing the data source method.

```csharp
public class TestDataProviders
{
    public static IEnumerable<(int, int, int)> AdditionTestCases()
    {
        yield return (1, 2, 3);
        yield return (5, 5, 10);
        yield return (-1, 1, 0);
    }
}

public class CalculatorTests
{
    [Test]
    [MethodDataSource<TestDataProviders>(nameof(TestDataProviders.AdditionTestCases))]
    public async Task Add_ShouldReturnCorrectSum(int a, int b, int expected)
    {
        var result = Calculator.Add(a, b);
        await Assert.That(result).IsEqualTo(expected);
    }
}
```

Benefits over non-generic version:
- Compile-time type checking
- IDE refactoring support
- Prevents typos in class names

### ClassDataSourceAttribute&lt;T&gt;

The generic version ensures type safety when referencing data source classes.

```csharp
public class UserTestData : IEnumerable<User>
{
    public IEnumerator<User> GetEnumerator()
    {
        yield return new User { Id = 1, Name = "Alice" };
        yield return new User { Id = 2, Name = "Bob" };
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class UserTests
{
    [Test]
    [ClassDataSource<UserTestData>]
    public async Task ValidateUser_ShouldPass(User user)
    {
        var isValid = await UserValidator.ValidateAsync(user);
        await Assert.That(isValid).IsTrue();
    }
}
```

### DependsOnAttribute&lt;T&gt;

The generic `DependsOn` attribute provides type-safe test dependency declarations.

```csharp
public class OrderProcessingTests
{
    [Test]
    public async Task CreateOrder()
    {
        // Create order logic
    }
    
    [Test]
    [DependsOn<OrderProcessingTests>(nameof(CreateOrder))]
    public async Task ProcessPayment()
    {
        // This test depends on CreateOrder from the same class
    }
}

public class ShippingTests
{
    [Test]
    [DependsOn<OrderProcessingTests>(nameof(OrderProcessingTests.ProcessPayment))]
    public async Task ShipOrder()
    {
        // This test depends on ProcessPayment from another class
    }
}
```

## Generic Data Source Attributes

### DataSourceGeneratorAttribute&lt;T&gt;

Create strongly-typed data source generators:

```csharp
public abstract class DataSourceGeneratorAttribute<T> : Attribute
{
    public abstract IEnumerable<T> GenerateData();
}

// Custom implementation
public class RandomNumbersAttribute : DataSourceGeneratorAttribute<int>
{
    private readonly int _count;
    private readonly int _min;
    private readonly int _max;
    
    public RandomNumbersAttribute(int count, int min = 0, int max = 100)
    {
        _count = count;
        _min = min;
        _max = max;
    }
    
    public override IEnumerable<int> GenerateData()
    {
        var random = new Random();
        for (int i = 0; i < _count; i++)
        {
            yield return random.Next(_min, _max);
        }
    }
}

// Usage
[Test]
[RandomNumbers(5, min: 1, max: 10)]
public async Task TestWithRandomNumbers(int number)
{
    await Assert.That(number).IsBetween(1, 10);
}
```

### AsyncDataSourceGeneratorAttribute&lt;T&gt;

For asynchronous data generation:

```csharp
public abstract class AsyncDataSourceGeneratorAttribute<T> : Attribute
{
    public abstract Task<IEnumerable<T>> GenerateDataAsync();
}

// Custom implementation
public class DatabaseUsersAttribute : AsyncDataSourceGeneratorAttribute<User>
{
    private readonly string _role;
    
    public DatabaseUsersAttribute(string role)
    {
        _role = role;
    }
    
    public override async Task<IEnumerable<User>> GenerateDataAsync()
    {
        using var db = new DatabaseContext();
        return await db.Users
            .Where(u => u.Role == _role)
            .ToListAsync();
    }
}

// Usage
[Test]
[DatabaseUsers("Admin")]
public async Task AdminUser_ShouldHaveFullPermissions(User adminUser)
{
    var permissions = await GetUserPermissions(adminUser);
    await Assert.That(permissions).Contains(Permission.FullAccess);
}
```

### TypedDataSourceAttribute&lt;T&gt;

Base class for creating custom typed data sources:

```csharp
public abstract class TypedDataSourceAttribute<T> : DataSourceAttribute
{
    public abstract IEnumerable<T> GetData();
}

// Custom implementation
public class SampleUsersAttribute : TypedDataSourceAttribute<User>
{
    public override IEnumerable<User> GetData()
    {
        yield return new User { Id = 1, Name = "Alice", Role = "Admin" };
        yield return new User { Id = 2, Name = "Bob", Role = "User" };
    }
}

// Usage
[Test]
[SampleUsers]
public async Task ValidateUser(User user)
{
    await Assert.That(user.Name).IsNotEmpty();
}
```

## Complex Generic Scenarios

### Combining Multiple Generic Attributes

```csharp
public interface ITestScenario<TInput, TExpected>
{
    TInput Input { get; }
    TExpected Expected { get; }
}

public class CalculationScenario : ITestScenario<(int, int), int>
{
    public (int, int) Input { get; set; }
    public int Expected { get; set; }
}

public class ScenarioDataSource<TScenario> : TypedDataSourceAttribute<TScenario>
    where TScenario : ITestScenario<(int, int), int>, new()
{
    public override IEnumerable<TScenario> GetData()
    {
        yield return new TScenario { Input = (1, 2), Expected = 3 };
        yield return new TScenario { Input = (5, 5), Expected = 10 };
    }
}

[Test]
[ScenarioDataSource<CalculationScenario>]
public async Task TestCalculation(CalculationScenario scenario)
{
    var (a, b) = scenario.Input;
    var result = Calculator.Add(a, b);
    await Assert.That(result).IsEqualTo(scenario.Expected);
}
```

### Generic Test Base Classes

⚠️ **Important Limitation**: C# does not allow generic type parameters to be used as attribute arguments. This is a known language limitation (see [dotnet/csharplang#124](https://github.com/dotnet/csharplang/issues/124)).

The following code **WILL NOT COMPILE** due to error CS8968:

```csharp
// ❌ This does NOT work - CS8968 error
public abstract class EntityTestBase<TEntity, TId>
    where TEntity : IEntity<TId>
{
    [Test]
    [MethodDataSource<EntityTestBase<TEntity, TId>>(nameof(GetTestIds))]  // ❌ Error!
    public async Task Entity_ShouldBeRetrievable(TId id) { }
}
```

#### Workaround 1: Use InstanceMethodDataSource

The recommended approach is to use `InstanceMethodDataSource` instead:

```csharp
public abstract class EntityTestBase<TEntity, TId>
    where TEntity : IEntity<TId>
    where TId : IEquatable<TId>
{
    protected abstract TEntity CreateEntity(TId id);
    protected abstract Task<TEntity> GetEntityAsync(TId id);

    // ✅ This works - instance method data source
    [Test]
    [InstanceMethodDataSource(nameof(GetTestIds))]
    public async Task Entity_ShouldBeRetrievable(TId id)
    {
        var entity = CreateEntity(id);
        await SaveEntityAsync(entity);

        var retrieved = await GetEntityAsync(id);
        await Assert.That(retrieved.Id).IsEqualTo(id);
    }

    // Instance method (not static)
    public IEnumerable<TId> GetTestIds()
    {
        return GetTestIdsCore();
    }

    protected abstract IEnumerable<TId> GetTestIdsCore();
}

public class UserEntityTests : EntityTestBase<User, Guid>
{
    protected override User CreateEntity(Guid id) =>
        new User { Id = id, Name = "Test User" };

    protected override Task<User> GetEntityAsync(Guid id) =>
        UserRepository.GetByIdAsync(id);

    protected override IEnumerable<Guid> GetTestIdsCore()
    {
        yield return Guid.NewGuid();
        yield return Guid.NewGuid();
    }
}
```

#### Workaround 2: Create Concrete Base Classes

For a limited set of types, create non-generic derived classes:

```csharp
// Base generic class (no data source attributes using generics)
public abstract class EntityTestBase<TEntity, TId>
    where TEntity : IEntity<TId>
    where TId : IEquatable<TId>
{
    protected abstract TEntity CreateEntity(TId id);
    protected abstract Task<TEntity> GetEntityAsync(TId id);

    protected async Task Entity_ShouldBeRetrievable(TId id)
    {
        var entity = CreateEntity(id);
        await SaveEntityAsync(entity);

        var retrieved = await GetEntityAsync(id);
        await Assert.That(retrieved.Id).IsEqualTo(id);
    }
}

// Concrete base class for Guid-based entities
public abstract class GuidEntityTestBase<TEntity> : EntityTestBase<TEntity, Guid>
    where TEntity : IEntity<Guid>
{
    [Test]
    [MethodDataSource<GuidEntityTestBase<TEntity>>(nameof(GetTestIds))]
    public async Task TestEntity(Guid id)
    {
        await Entity_ShouldBeRetrievable(id);
    }

    public static IEnumerable<Guid> GetTestIds()
    {
        yield return Guid.NewGuid();
        yield return Guid.NewGuid();
    }
}

// Your test class
public class UserEntityTests : GuidEntityTestBase<User>
{
    protected override User CreateEntity(Guid id) =>
        new User { Id = id, Name = "Test User" };

    protected override Task<User> GetEntityAsync(Guid id) =>
        UserRepository.GetByIdAsync(id);
}
```

## AOT Compatibility

Generic attributes work well with AOT compilation, but there are some considerations:

### DynamicallyAccessedMembers

When creating generic attributes that use reflection, add appropriate attributes:

```csharp
public class ReflectiveDataSource<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors | 
    DynamicallyAccessedMemberTypes.PublicProperties)] T> 
    : TypedDataSourceAttribute<T> where T : new()
{
    public override IEnumerable<T> GetData()
    {
        var type = typeof(T);
        var properties = type.GetProperties();
        
        // Create instances with different property values
        foreach (var prop in properties)
        {
            var instance = new T();
            // Set property values...
            yield return instance;
        }
    }
}
```

## Best Practices

### 1. Use Generic Attributes for Type Safety

```csharp
// ❌ Non-generic - prone to errors
[MethodDataSource(typeof(DataProvider), "GetData")]

// ✅ Generic - compile-time safety
[MethodDataSource<DataProvider>(nameof(DataProvider.GetData))]
```

### 2. Leverage Constraints

```csharp
public class ValidatableDataSource<T> : TypedDataSourceAttribute<T>
    where T : IValidatable
{
    public override IEnumerable<T> GetData()
    {
        // Only return valid instances
        return GenerateInstances().Where(x => x.IsValid());
    }
}
```

### 3. Create Reusable Generic Base Attributes

```csharp
public abstract class JsonFileDataSource<T> : TypedDataSourceAttribute<T>
{
    protected abstract string FilePath { get; }
    
    public override IEnumerable<T> GetData()
    {
        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<List<T>>(json) 
            ?? Enumerable.Empty<T>();
    }
}

public class UserJsonDataSource : JsonFileDataSource<User>
{
    protected override string FilePath => "TestData/users.json";
}
```

### 4. Document Generic Type Parameters

```csharp
/// <summary>
/// Provides test data from a CSV file
/// </summary>
/// <typeparam name="T">The type to deserialize CSV rows into. 
/// Must have a parameterless constructor.</typeparam>
public class CsvDataSource<T> : TypedDataSourceAttribute<T> 
    where T : new()
{
    // Implementation
}
```

## Summary

Generic attributes in TUnit provide:
- **Type Safety**: Compile-time checking prevents runtime errors
- **Better IDE Support**: Refactoring and navigation work correctly
- **Cleaner Code**: No magic strings or typeof expressions
- **AOT Compatibility**: Work well with ahead-of-time compilation
- **Reusability**: Easy to create generic base attributes for common patterns

Use generic attributes whenever possible to improve code quality and maintainability in your test suites.