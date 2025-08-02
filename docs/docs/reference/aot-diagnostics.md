# AOT Diagnostics Reference

TUnit's AOT-only mode provides comprehensive compile-time diagnostics to help you write compatible test code. These diagnostics catch issues early and provide actionable guidance for fixing them.

## Generic Test Diagnostics

### TUnit0058: Generic Test Missing Explicit Instantiation

**Problem**: Generic test classes or methods require explicit type instantiation for AOT compatibility.

#### Example Error:
```csharp
// ❌ This generates TUnit0058
[Test]
public async Task GenericTest<T>()
{
    var value = default(T);
    await Assert.That(value).IsNotNull().Or.IsNull();
}
```

#### Solution:
```csharp
// ✅ Add explicit generic instantiation
[Test]
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(DateTime))]
public async Task GenericTest<T>()
{
    var value = default(T);
    await Assert.That(value).IsNotNull().Or.IsNull();
}
```

#### For Generic Classes:
```csharp
// ❌ This generates TUnit0058
public class GenericTestClass<T>
{
    [Test]
    public async Task TestMethod()
    {
        var value = default(T);
        await Assert.That(value).IsNotNull().Or.IsNull();
    }
}

// ✅ Add explicit instantiation to the class
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(bool))]
public class GenericTestClass<T>
{
    [Test]
    public async Task TestMethod()
    {
        var value = default(T);
        await Assert.That(value).IsNotNull().Or.IsNull();
    }
}
```


## Data Source Diagnostics

### TUnit0059: Dynamic Data Source Not AOT-Compatible

**Problem**: Data sources that use dynamic resolution or reflection aren't compatible with AOT compilation.

#### Example Error:
```csharp
public class DataSourceDiagnostics
{
    // ❌ This generates TUnit0059 error
    [Test]
    [MethodDataSource(nameof(GetDynamicData))]
    public async Task TestWithDynamicDataSource(object value)
    {
        await Assert.That(value).IsNotNull();
    }

    public IEnumerable<object[]> GetDynamicData()
    {
        // This method uses reflection internally - not AOT compatible
        return ReflectionBasedDataGenerator.GetData();
    }
}
```

#### Solution - Use Static Data Sources:
```csharp
public class AotCompatibleDataSources
{
    // ✅ Static data source - AOT compatible
    [Test]
    [MethodDataSource(nameof(GetStaticData))]
    public async Task TestWithStaticDataSource(string value)
    {
        await Assert.That(value).IsNotNull();
    }

    public static IEnumerable<object[]> GetStaticData()
    {
        yield return new object[] { "static1" };
        yield return new object[] { "static2" };
        yield return new object[] { "static3" };
    }

    // ✅ Async static data source with proper typing
    [Test]
    [MethodDataSource(nameof(GetAsyncStaticData))]
    public async Task TestWithAsyncStaticDataSource(int id, string name)
    {
        await Assert.That(id).IsGreaterThan(0);
        await Assert.That(name).IsNotEmpty();
    }

    public static async IAsyncEnumerable<object[]> GetAsyncStaticData()
    {
        await Task.Delay(1); // Simulate async work
        yield return new object[] { 1, "first" };
        yield return new object[] { 2, "second" };
        yield return new object[] { 3, "third" };
    }
}
```

### TUnit0060: Open Generic Type Not AOT-Compatible

**Problem**: Using open generic types in data sources that can't be resolved at compile time.

#### Example Error:
```csharp
// ❌ This generates TUnit0060 error
[Test]
[MethodDataSource(nameof(GetGenericData))]
public async Task TestWithOpenGeneric<T>(T value)
{
    await Assert.That(value).IsNotNull();
}

public static IEnumerable<object[]> GetGenericData()
{
    // Can't determine T at compile time
    yield return new object[] { default(T) }; // Error: T is not available
}
```

#### Solution - Use Explicit Generic Instantiation:
```csharp
// ✅ Explicit generic instantiation
[Test]
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(int))]
[MethodDataSource(nameof(GetTypedData))]
public async Task TestWithExplicitGeneric<T>(T value)
{
    await Assert.That(value).IsNotNull().Or.IsEqualTo(default(T));
}

public static IEnumerable<object[]> GetTypedData()
{
    // Static data that works with known types
    yield return new object[] { "string_value" };
    yield return new object[] { 42 };
}
```

## Configuration Diagnostics

### TUnit0061: Invalid Configuration Value

**Problem**: Configuration values in EditorConfig or MSBuild properties are outside valid ranges.

#### Example Warnings:
```ini
# ❌ These generate configuration warnings
tunit.generic_depth_limit = 50          # Max is 20
tunit.max_generic_instantiations = 500  # Max is 100
tunit.generic_depth_limit = 0           # Min is 1
```

#### Solution:
```ini
# ✅ Valid configuration values
tunit.generic_depth_limit = 10          # Range: 1-20
tunit.max_generic_instantiations = 25   # Range: 1-100
tunit.enable_verbose_diagnostics = true # Boolean values
```

## Performance Diagnostics

### TUnit0062: Performance Warning

**Problem**: Configuration or code patterns that may impact performance.

#### Example Scenarios:
- Too many generic instantiations
- Very deep generic nesting
- Excessive async data source complexity

#### Solutions:
```ini
# Optimize for performance
tunit.generic_depth_limit = 5           # Lower depth for faster builds
tunit.max_generic_instantiations = 10   # Fewer instantiations
tunit.enable_auto_generic_discovery = false # Manual control
```

## Diagnostic Severity Levels

### Error Level (Build Fails)
- **TUnit0057**: Reflection usage in AOT mode
- **TUnit0058**: Generic test missing instantiation  
- **TUnit0059**: Dynamic data source not AOT-compatible
- **TUnit0060**: Open generic type not AOT-compatible

### Warning Level (Build Succeeds)
- **TUnit0061**: Invalid configuration value (uses default)
- **TUnit0062**: Performance warning
- **TUnit0063**: Deprecated pattern usage

### Info Level (Informational)
- **TUnit0064**: AOT optimization suggestion
- **TUnit0065**: Generic instantiation discovered
- **TUnit0066**: Performance metric report

## Enabling Verbose Diagnostics

To get more detailed diagnostic information:

### EditorConfig:
```ini
tunit.enable_verbose_diagnostics = true
```

### MSBuild:
```xml
<PropertyGroup>
    <TUnitEnableVerboseDiagnostics>true</TUnitEnableVerboseDiagnostics>
</PropertyGroup>
```

### Verbose Output Examples:
```
TUnit: Analyzing generic test class 'MyTestClass<T>'
TUnit: Generated instantiation for types: [System.String, System.Int32]
TUnit: Created strongly-typed delegate for 'MyTestClass<String>.TestMethod'
TUnit: Registered async data source factory 'GetAsyncTestData'
TUnit: AOT compilation mode: Enabled (0 reflection fallbacks)
TUnit: Performance: Generated 15 delegates in 245ms
```

## Common Resolution Patterns

### Migration from Reflection to AOT

#### Before (Reflection-based):
```csharp
[Test]
public async Task TestDynamicType()
{
    var type = Type.GetType("MyNamespace.MyClass");
    var instance = Activator.CreateInstance(type);
    var method = type.GetMethod("ProcessData");
    var result = method.Invoke(instance, new[] { "test" });
    
    await Assert.That(result).IsNotNull();
}
```

#### After (AOT-compatible):
```csharp
[Test]
public async Task TestStaticType()
{
    var instance = new MyClass();
    var result = instance.ProcessData("test");
    
    await Assert.That(result).IsNotNull();
}
```

### Generic Test Migration

#### Before:
```csharp
[Test]
public async Task TestGeneric<T>() where T : new()
{
    var instance = new T();
    // Test logic
}
```

#### After:
```csharp
[Test]
[GenerateGenericTest(typeof(MyClass))]
[GenerateGenericTest(typeof(AnotherClass))]
public async Task TestGeneric<T>() where T : new()
{
    var instance = new T();
    // Same test logic, now AOT-compatible
}
```

## IDE Integration

Most IDEs will show TUnit diagnostics inline with:

- **Red squiggly lines** for errors
- **Yellow squiggly lines** for warnings  
- **Blue squiggly lines** for info
- **Quick fixes** available via Ctrl+. (Cmd+. on Mac)

The diagnostics include fix suggestions and links to documentation for resolving issues.