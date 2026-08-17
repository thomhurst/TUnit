# Type Checking

TUnit assertions check types at compile time wherever possible. This gives faster feedback and catches mistakes before your build pipeline runs.

For example, this wouldn't compile because we're comparing an `int` and a `string`:

```
[Test]

public async Task MyTest()

{

    await Assert.That(1).IsEqualTo("1");

}
```

## Runtime Type Assertions[​](#runtime-type-assertions "Direct link to Runtime Type Assertions")

When you need to verify types at runtime — for example, when working with polymorphic return types — TUnit provides dedicated assertions.

### IsTypeOf[​](#istypeof "Direct link to IsTypeOf")

Tests that a value is exactly the specified type (not a subclass):

```
[Test]

public async Task Exact_Type()

{

    object result = GetAnimal();



    await Assert.That(result).IsTypeOf<Dog>();

}
```

### IsAssignableTo[​](#isassignableto "Direct link to IsAssignableTo")

Tests that a value can be assigned to the specified type, including base classes and interfaces:

```
[Test]

public async Task Assignable_To_Base_Or_Interface()

{

    object result = GetAnimal();



    await Assert.That(result).IsAssignableTo<Animal>();

    await Assert.That(result).IsAssignableTo<IMovable>();

}
```

### IsNotTypeOf[​](#isnottypeof "Direct link to IsNotTypeOf")

Tests that a value is **not** exactly the specified type:

```
[Test]

public async Task Not_Exact_Type()

{

    Animal animal = GetAnimal();



    await Assert.That(animal).IsNotTypeOf<Cat>();

}
```

### IsNotAssignableTo[​](#isnotassignableto "Direct link to IsNotAssignableTo")

Tests that a value cannot be assigned to the specified type:

```
[Test]

public async Task Not_Assignable()

{

    object result = GetAnimal();



    await Assert.That(result).IsNotAssignableTo<string>();

}
```

### IsAssignableFrom[​](#isassignablefrom "Direct link to IsAssignableFrom")

Tests that a value of the specified type can be assigned to a variable of this value's type. This is the reverse of `IsAssignableTo`:

```
[Test]

public async Task Assignable_From_Derived()

{

    Animal animal = GetAnimal();



    // Animal variable can accept a Dog value

    await Assert.That(animal).IsAssignableFrom<Dog>();

}
```

### IsNotAssignableFrom[​](#isnotassignablefrom "Direct link to IsNotAssignableFrom")

Tests that a value of the specified type cannot be assigned to a variable of this value's type:

```
[Test]

public async Task Not_Assignable_From()

{

    Dog dog = GetDog();



    await Assert.That(dog).IsNotAssignableFrom<string>();

}
```

## Delegate Return Types[​](#delegate-return-types "Direct link to Delegate Return Types")

Type assertions also work on delegate return values, letting you verify the type returned by a method or lambda:

```
[Test]

public async Task Delegate_Return_Type()

{

    await Assert.That(() => GetAnimal()).IsTypeOf<Dog>();

    await Assert.That(async () => await GetAnimalAsync()).IsAssignableTo<Animal>();

}
```
