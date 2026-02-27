---
sidebar_position: 5
---

# Type Checking

TUnit assertions check types at compile time wherever possible. This gives faster feedback and catches mistakes before your build pipeline runs.

For example, this wouldn't compile because we're comparing an `int` and a `string`:

```csharp
[Test]
public async Task MyTest()
{
    await Assert.That(1).IsEqualTo("1");
}
```

## Runtime Type Assertions

When you need to verify types at runtime — for example, when working with polymorphic return types — TUnit provides dedicated assertions.

### IsTypeOf

Tests that a value is exactly the specified type (not a subclass):

```csharp
[Test]
public async Task Exact_Type()
{
    object result = GetAnimal();

    await Assert.That(result).IsTypeOf<Dog>();
}
```

### IsAssignableTo

Tests that a value can be assigned to the specified type, including base classes and interfaces:

```csharp
[Test]
public async Task Assignable_To_Base_Or_Interface()
{
    object result = GetAnimal();

    await Assert.That(result).IsAssignableTo<Animal>();
    await Assert.That(result).IsAssignableTo<IMovable>();
}
```

### IsNotTypeOf

Tests that a value is **not** exactly the specified type:

```csharp
[Test]
public async Task Not_Exact_Type()
{
    Animal animal = GetAnimal();

    await Assert.That(animal).IsNotTypeOf<Cat>();
}
```

### IsNotAssignableTo

Tests that a value cannot be assigned to the specified type:

```csharp
[Test]
public async Task Not_Assignable()
{
    object result = GetAnimal();

    await Assert.That(result).IsNotAssignableTo<string>();
}
```

### IsAssignableFrom

Tests that a value of the specified type can be assigned to a variable of this value's type. This is the reverse of `IsAssignableTo`:

```csharp
[Test]
public async Task Assignable_From_Derived()
{
    Animal animal = GetAnimal();

    // Animal variable can accept a Dog value
    await Assert.That(animal).IsAssignableFrom<Dog>();
}
```

### IsNotAssignableFrom

Tests that a value of the specified type cannot be assigned to a variable of this value's type:

```csharp
[Test]
public async Task Not_Assignable_From()
{
    Dog dog = GetDog();

    await Assert.That(dog).IsNotAssignableFrom<string>();
}
```

## Delegate Return Types

Type assertions also work on delegate return values, letting you verify the type returned by a method or lambda:

```csharp
[Test]
public async Task Delegate_Return_Type()
{
    await Assert.That(() => GetAnimal()).IsTypeOf<Dog>();
    await Assert.That(async () => await GetAnimalAsync()).IsAssignableTo<Animal>();
}
```
