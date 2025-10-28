---
sidebar_position: 9
---

# Type Assertions

TUnit provides comprehensive assertions for testing types and type properties. These assertions work with both runtime values and `Type` objects themselves.

## Value Type Assertions

### IsTypeOf&lt;T&gt;

Tests that a value is exactly of a specific type:

```csharp
[Test]
public async Task Value_Is_Type()
{
    object value = "Hello";

    await Assert.That(value).IsTypeOf<string>();
}
```

Works with all types:

```csharp
[Test]
public async Task Various_Types()
{
    await Assert.That(42).IsTypeOf<int>();
    await Assert.That(3.14).IsTypeOf<double>();
    await Assert.That(true).IsTypeOf<bool>();
    await Assert.That(new List<int>()).IsTypeOf<List<int>>();
}
```

### IsAssignableTo&lt;T&gt;

Tests that a type can be assigned to a target type (inheritance/interface):

```csharp
[Test]
public async Task Type_Is_Assignable()
{
    var list = new List<int>();

    await Assert.That(list).IsAssignableTo<IList<int>>();
    await Assert.That(list).IsAssignableTo<IEnumerable<int>>();
    await Assert.That(list).IsAssignableTo<object>();
}
```

With inheritance:

```csharp
public class Animal { }
public class Dog : Animal { }

[Test]
public async Task Inheritance_Assignability()
{
    var dog = new Dog();

    await Assert.That(dog).IsAssignableTo<Animal>();
    await Assert.That(dog).IsAssignableTo<Dog>();
    await Assert.That(dog).IsAssignableTo<object>();
}
```

### IsNotAssignableTo&lt;T&gt;

Tests that a type cannot be assigned to a target type:

```csharp
[Test]
public async Task Type_Not_Assignable()
{
    var value = 42;

    await Assert.That(value).IsNotAssignableTo<string>();
    await Assert.That(value).IsNotAssignableTo<double>();
}
```

## Type Object Assertions

All the following assertions work on `Type` objects directly:

```csharp
[Test]
public async Task Type_Object_Assertions()
{
    var type = typeof(string);

    await Assert.That(type).IsClass();
    await Assert.That(type).IsNotInterface();
}
```

### Class and Interface

#### IsClass / IsNotClass

```csharp
[Test]
public async Task Is_Class()
{
    await Assert.That(typeof(string)).IsClass();
    await Assert.That(typeof(List<int>)).IsClass();
    await Assert.That(typeof(object)).IsClass();

    await Assert.That(typeof(IEnumerable)).IsNotClass();
    await Assert.That(typeof(int)).IsNotClass(); // Value type
}
```

#### IsInterface / IsNotInterface

```csharp
[Test]
public async Task Is_Interface()
{
    await Assert.That(typeof(IEnumerable)).IsInterface();
    await Assert.That(typeof(IDisposable)).IsInterface();

    await Assert.That(typeof(string)).IsNotInterface();
}
```

### Modifiers

#### IsAbstract / IsNotAbstract

```csharp
public abstract class AbstractBase { }
public class Concrete : AbstractBase { }

[Test]
public async Task Is_Abstract()
{
    await Assert.That(typeof(AbstractBase)).IsAbstract();
    await Assert.That(typeof(Concrete)).IsNotAbstract();
}
```

#### IsSealed / IsNotSealed

```csharp
public sealed class SealedClass { }
public class OpenClass { }

[Test]
public async Task Is_Sealed()
{
    await Assert.That(typeof(SealedClass)).IsSealed();
    await Assert.That(typeof(string)).IsSealed(); // string is sealed
    await Assert.That(typeof(OpenClass)).IsNotSealed();
}
```

### Value Types and Enums

#### IsValueType / IsNotValueType

```csharp
[Test]
public async Task Is_Value_Type()
{
    await Assert.That(typeof(int)).IsValueType();
    await Assert.That(typeof(DateTime)).IsValueType();
    await Assert.That(typeof(Guid)).IsValueType();

    await Assert.That(typeof(string)).IsNotValueType();
    await Assert.That(typeof(object)).IsNotValueType();
}
```

#### IsEnum / IsNotEnum

```csharp
public enum Color { Red, Green, Blue }

[Test]
public async Task Is_Enum()
{
    await Assert.That(typeof(Color)).IsEnum();
    await Assert.That(typeof(DayOfWeek)).IsEnum();

    await Assert.That(typeof(int)).IsNotEnum();
}
```

#### IsPrimitive / IsNotPrimitive

```csharp
[Test]
public async Task Is_Primitive()
{
    // Primitives: bool, byte, sbyte, short, ushort, int, uint,
    //             long, ulong, char, double, float, IntPtr, UIntPtr
    await Assert.That(typeof(int)).IsPrimitive();
    await Assert.That(typeof(bool)).IsPrimitive();
    await Assert.That(typeof(char)).IsPrimitive();

    await Assert.That(typeof(string)).IsNotPrimitive();
    await Assert.That(typeof(decimal)).IsNotPrimitive();
}
```

### Visibility

#### IsPublic / IsNotPublic

```csharp
public class PublicClass { }
internal class InternalClass { }

[Test]
public async Task Is_Public()
{
    await Assert.That(typeof(PublicClass)).IsPublic();
    await Assert.That(typeof(string)).IsPublic();

    await Assert.That(typeof(InternalClass)).IsNotPublic();
}
```

### Generics

#### IsGenericType / IsNotGenericType

```csharp
[Test]
public async Task Is_Generic_Type()
{
    await Assert.That(typeof(List<int>)).IsGenericType();
    await Assert.That(typeof(Dictionary<string, int>)).IsGenericType();

    await Assert.That(typeof(string)).IsNotGenericType();
}
```

#### IsGenericTypeDefinition / IsNotGenericTypeDefinition

```csharp
[Test]
public async Task Is_Generic_Type_Definition()
{
    // Generic type definition (unbound)
    await Assert.That(typeof(List<>)).IsGenericTypeDefinition();
    await Assert.That(typeof(Dictionary<,>)).IsGenericTypeDefinition();

    // Constructed generic type (bound)
    await Assert.That(typeof(List<int>)).IsNotGenericTypeDefinition();
}
```

#### IsConstructedGenericType / IsNotConstructedGenericType

```csharp
[Test]
public async Task Is_Constructed_Generic_Type()
{
    await Assert.That(typeof(List<int>)).IsConstructedGenericType();
    await Assert.That(typeof(Dictionary<string, int>)).IsConstructedGenericType();

    await Assert.That(typeof(List<>)).IsNotConstructedGenericType();
    await Assert.That(typeof(string)).IsNotConstructedGenericType();
}
```

#### ContainsGenericParameters / DoesNotContainGenericParameters

```csharp
[Test]
public async Task Contains_Generic_Parameters()
{
    await Assert.That(typeof(List<>)).ContainsGenericParameters();

    await Assert.That(typeof(List<int>)).DoesNotContainGenericParameters();
    await Assert.That(typeof(string)).DoesNotContainGenericParameters();
}
```

### Arrays and Pointers

#### IsArray / IsNotArray

```csharp
[Test]
public async Task Is_Array()
{
    await Assert.That(typeof(int[])).IsArray();
    await Assert.That(typeof(string[])).IsArray();
    await Assert.That(typeof(int[,])).IsArray(); // Multi-dimensional

    await Assert.That(typeof(List<int>)).IsNotArray();
}
```

#### IsByRef / IsNotByRef

```csharp
[Test]
public async Task Is_By_Ref()
{
    var method = typeof(string).GetMethod(nameof(int.TryParse));
    var parameters = method!.GetParameters();
    var outParam = parameters.First(p => p.IsOut);

    await Assert.That(outParam.ParameterType).IsByRef();
}
```

#### IsByRefLike / IsNotByRefLike (.NET 5+)

```csharp
[Test]
public async Task Is_By_Ref_Like()
{
    await Assert.That(typeof(Span<int>)).IsByRefLike();
    await Assert.That(typeof(ReadOnlySpan<int>)).IsByRefLike();

    await Assert.That(typeof(string)).IsNotByRefLike();
}
```

#### IsPointer / IsNotPointer

```csharp
[Test]
public async Task Is_Pointer()
{
    unsafe
    {
        var intPtr = typeof(int*);
        await Assert.That(intPtr).IsPointer();
    }

    await Assert.That(typeof(int)).IsNotPointer();
}
```

### Nested Types

#### IsNested / IsNotNested

```csharp
public class Outer
{
    public class Inner { }
}

[Test]
public async Task Is_Nested()
{
    await Assert.That(typeof(Outer.Inner)).IsNested();
    await Assert.That(typeof(Outer)).IsNotNested();
}
```

#### IsNestedPublic / IsNotNestedPublic

```csharp
public class Container
{
    public class PublicNested { }
    private class PrivateNested { }
}

[Test]
public async Task Is_Nested_Public()
{
    await Assert.That(typeof(Container.PublicNested)).IsNestedPublic();
}
```

#### IsNestedPrivate / IsNotNestedPrivate

```csharp
[Test]
public async Task Is_Nested_Private()
{
    var privateType = typeof(Container)
        .GetNestedType("PrivateNested", BindingFlags.NonPublic);

    await Assert.That(privateType).IsNestedPrivate();
}
```

#### IsNestedAssembly / IsNotNestedAssembly

For internal nested types.

#### IsNestedFamily / IsNotNestedFamily

For protected nested types.

### Visibility Checks

#### IsVisible / IsNotVisible

```csharp
[Test]
public async Task Is_Visible()
{
    await Assert.That(typeof(string)).IsVisible();
    await Assert.That(typeof(List<int>)).IsVisible();

    // Internal types are not visible
    var internalType = Assembly.GetExecutingAssembly()
        .GetTypes()
        .FirstOrDefault(t => !t.IsPublic && !t.IsNested);

    if (internalType != null)
    {
        await Assert.That(internalType).IsNotVisible();
    }
}
```

### COM Interop

#### IsCOMObject / IsNotCOMObject

```csharp
[Test]
public async Task Is_COM_Object()
{
    await Assert.That(typeof(string)).IsNotCOMObject();
    // COM types would return true
}
```

## Practical Examples

### Dependency Injection Validation

```csharp
[Test]
public async Task Service_Implements_Interface()
{
    var service = GetService<IUserService>();

    await Assert.That(service).IsAssignableTo<IUserService>();
    await Assert.That(service).IsNotNull();
}
```

### Plugin System

```csharp
public interface IPlugin { }

[Test]
public async Task Plugin_Implements_Interface()
{
    var pluginType = LoadPluginType();

    await Assert.That(pluginType).IsAssignableTo<IPlugin>();
    await Assert.That(pluginType).IsClass();
    await Assert.That(pluginType).IsNotAbstract();
}
```

### Reflection Testing

```csharp
[Test]
public async Task Type_Has_Expected_Properties()
{
    var type = typeof(User);

    await Assert.That(type).IsClass();
    await Assert.That(type).IsPublic();
    await Assert.That(type).IsNotAbstract();
    await Assert.That(type).IsNotSealed();
}
```

### Generic Constraints

```csharp
[Test]
public async Task Validate_Generic_Constraints()
{
    var listType = typeof(List<int>);

    await Assert.That(listType).IsGenericType();
    await Assert.That(listType).IsAssignableTo<IEnumerable<int>>();
}
```

### Enum Validation

```csharp
[Test]
public async Task Type_Is_Enum()
{
    var statusType = typeof(OrderStatus);

    await Assert.That(statusType).IsEnum();
    await Assert.That(statusType).IsValueType();
}
```

### Abstract Class Validation

```csharp
[Test]
public async Task Base_Class_Is_Abstract()
{
    var baseType = typeof(BaseRepository);

    await Assert.That(baseType).IsClass();
    await Assert.That(baseType).IsAbstract();
}
```

## Chaining Type Assertions

```csharp
[Test]
public async Task Chained_Type_Assertions()
{
    var type = typeof(MyService);

    await Assert.That(type)
        .IsClass()
        .And.IsPublic()
        .And.IsNotAbstract()
        .And.IsNotSealed();
}
```

## Type Comparison

```csharp
[Test]
public async Task Compare_Types()
{
    var type1 = typeof(List<int>);
    var type2 = typeof(List<int>);
    var type3 = typeof(List<string>);

    await Assert.That(type1).IsEqualTo(type2);
    await Assert.That(type1).IsNotEqualTo(type3);
}
```

## Working with Base Types

```csharp
[Test]
public async Task Check_Base_Type()
{
    var type = typeof(ArgumentNullException);
    var baseType = type.BaseType;

    await Assert.That(baseType).IsEqualTo(typeof(ArgumentException));
}
```

## Interface Implementation

```csharp
[Test]
public async Task Implements_Multiple_Interfaces()
{
    var type = typeof(List<int>);

    await Assert.That(type).IsAssignableTo<IList<int>>();
    await Assert.That(type).IsAssignableTo<ICollection<int>>();
    await Assert.That(type).IsAssignableTo<IEnumerable<int>>();
}
```

## Common Patterns

### Factory Pattern Validation

```csharp
[Test]
public async Task Factory_Returns_Correct_Type()
{
    var instance = Factory.Create("user-service");

    await Assert.That(instance).IsTypeOf<UserService>();
    await Assert.That(instance).IsAssignableTo<IService>();
}
```

### ORM Entity Validation

```csharp
[Test]
public async Task Entity_Is_Properly_Configured()
{
    var entityType = typeof(Order);

    await Assert.That(entityType).IsClass();
    await Assert.That(entityType).IsPublic();
    await Assert.That(entityType).IsNotAbstract();

    // Check for required interfaces
    await Assert.That(entityType).IsAssignableTo<IEntity>();
}
```

### Serialization Requirements

```csharp
[Test]
public async Task Type_Is_Serializable()
{
    var type = typeof(DataTransferObject);

    await Assert.That(type).IsClass();
    await Assert.That(type).IsPublic();

    // All properties should be public
    var properties = type.GetProperties();
    await Assert.That(properties).All(p => p.GetMethod?.IsPublic ?? false);
}
```

### Test Double Validation

```csharp
[Test]
public async Task Mock_Implements_Interface()
{
    var mock = new Mock<IUserRepository>();
    var instance = mock.Object;

    await Assert.That(instance).IsAssignableTo<IUserRepository>();
}
```

## Struct Validation

```csharp
public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
}

[Test]
public async Task Struct_Properties()
{
    var type = typeof(Point);

    await Assert.That(type).IsValueType();
    await Assert.That(type).IsNotClass();
    await Assert.That(type).IsNotEnum();
}
```

## Record Validation

```csharp
public record Person(string Name, int Age);

[Test]
public async Task Record_Properties()
{
    var type = typeof(Person);

    await Assert.That(type).IsClass();
    // Records are classes with special properties
}
```

## See Also

- [Exceptions](exceptions.md) - Type checking for exceptions
- [Equality & Comparison](equality-and-comparison.md) - Comparing type objects
- [Collections](collections.md) - Type checking collection elements
