# FsCheck (Property-Based Testing)

[FsCheck](https://fscheck.github.io/FsCheck/) is a property-based testing framework for .NET. Property-based testing generates random test data to verify that properties (invariants) hold true across many inputs.

There is a NuGet package to help integrate FsCheck with TUnit: `TUnit.FsCheck`

## Installation[​](#installation "Direct link to Installation")

```
dotnet add package TUnit.FsCheck
```

## Basic Usage[​](#basic-usage "Direct link to Basic Usage")

Use the `[Test, FsCheckProperty]` attributes together to create a property-based test:

```
using TUnit.FsCheck;



public class PropertyTests

{

    [Test, FsCheckProperty]

    public bool ReverseReverseIsOriginal(int[] array)

    {

        var reversed = array.Reverse().Reverse().ToArray();

        return array.SequenceEqual(reversed);

    }



    [Test, FsCheckProperty]

    public bool AdditionIsCommutative(int a, int b)

    {

        return a + b == b + a;

    }

}
```

## Return Types[​](#return-types "Direct link to Return Types")

Property tests can return:

* **`bool`** - The test passes if the property returns `true`
* **`void`** - The test passes if no exception is thrown
* **`Task` / `ValueTask`** - For async properties
* **`Property`** - For advanced FsCheck properties with custom generators

```
using FsCheck;

using FsCheck.Fluent;



// Boolean property

[Test, FsCheckProperty]

public bool StringConcatLength(string a, string b)

{

    if (a == null || b == null) return true; // Skip null cases

    return (a + b).Length == a.Length + b.Length;

}



// Void property (throws on failure)

[Test, FsCheckProperty]

public void MultiplicationByZeroIsZero(int value)

{

    if (value * 0 != 0)

        throw new InvalidOperationException("Expected zero");

}



// Async property

[Test, FsCheckProperty]

public async Task AsyncPropertyTest(int value)

{

    await Task.Delay(1);

    if (value * 0 != 0)

        throw new InvalidOperationException("Expected zero");

}



// FsCheck Property type for advanced scenarios

[Test, FsCheckProperty]

public Property StringReversalProperty()

{

    return Prop.ForAll<string>(str =>

    {

        var reversed = new string(str.Reverse().ToArray());

        var doubleReversed = new string(reversed.Reverse().ToArray());

        return str == doubleReversed;

    });

}
```

## Configuration Options[​](#configuration-options "Direct link to Configuration Options")

The `[FsCheckProperty]` attribute supports several configuration options:

| Property         | Default | Description                                                                                                      |
| ---------------- | ------- | ---------------------------------------------------------------------------------------------------------------- |
| `MaxTest`        | 100     | Maximum number of tests to run                                                                                   |
| `MaxFail`        | 1000    | Maximum rejected tests before failing                                                                            |
| `StartSize`      | 1       | Starting size for test generation                                                                                |
| `EndSize`        | 100     | Ending size for test generation                                                                                  |
| `Replay`         | null    | Replay a reported run: `"seed,gamma"` or `"seed,gamma,size"` (see [Reproducing Failures](#reproducing-failures)) |
| `Verbose`        | false   | Write every generated argument and shrink step to the test output                                                |
| `QuietOnSuccess` | false   | Suppress the `Ok, passed N tests.` summary on passing tests                                                      |
| `Parallelism`    | 1       | Degree of parallelism for property invocations; above 1, the property must be thread-safe                        |
| `Arbitrary`      | null    | Types containing custom Arbitrary instances                                                                      |

### Example with Configuration[​](#example-with-configuration "Direct link to Example with Configuration")

```
[Test, FsCheckProperty(MaxTest = 50, StartSize = 1, EndSize = 50)]

public bool ListConcatenationPreservesElements(int[] first, int[] second)

{

    var combined = first.Concat(second).ToArray();

    return combined.Length == first.Length + second.Length;

}
```

## Reproducing Failures[​](#reproducing-failures "Direct link to Reproducing Failures")

When a property test fails, FsCheck's message contains the `(seed,gamma)` the run started from and, after more than one test, a `Replay directly at failing step with (seed,gamma,size)` hint. Copy either value into the `Replay` property, with or without the parentheses:

* `"seed,gamma"` replays the whole run.
* `"seed,gamma,size"` replays only the failing step.

The gamma is required: FsCheck always reports it next to the seed, and the same seed with a different gamma generates a different run. A value in any other format fails the test with an explanation.

```
[Test, FsCheckProperty(Replay = "(12345,67891)")]

public bool MyProperty(int value)

{

    return value >= 0; // Will reproduce the same failing case

}
```

## Custom Generators[​](#custom-generators "Direct link to Custom Generators")

You can provide custom `Arbitrary` implementations for generating test data. FsCheck 3.x uses `ArbMap.Default` to access default arbitraries:

```
using FsCheck;

using FsCheck.Fluent;



public class PositiveIntArbitrary

{

    public static Arbitrary<int> PositiveInt()

    {

        // Use ArbMap.Default to get a generator for a type,

        // then filter with Where() and convert to Arbitrary

        return ArbMap.Default.GeneratorFor<int>()

            .Where(x => x > 0)

            .ToArbitrary();

    }

}



[Test, FsCheckProperty(Arbitrary = new[] { typeof(PositiveIntArbitrary) })]

public bool PositiveNumbersArePositive(int value)

{

    return value > 0;

}
```

### Alternative: Using Gen.Choose[​](#alternative-using-genchoose "Direct link to Alternative: Using Gen.Choose")

For simple ranges, use `Gen.Choose` directly:

```
using FsCheck;

using FsCheck.Fluent;



public class PositiveIntArbitrary

{

    public static Arbitrary<int> PositiveInt()

    {

        // Generate integers in a specific range

        return Gen.Choose(1, int.MaxValue).ToArbitrary();

    }

}
```

### Custom types[​](#custom-types "Direct link to Custom types")

For custom types, compose generators using LINQ:

```
using FsCheck;

using FsCheck.Fluent;



public class Person

{

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

}



public class PersonArbitrary

{

    public static Arbitrary<Person> Person()

    {

        var gen = from name in ArbMap.Default.GeneratorFor<string>()

                  from age in Gen.Choose(0, 120)

                  select new Person { Name = name, Age = age };

        return gen.ToArbitrary();

    }

}
```

## Limitations[​](#limitations "Direct link to Limitations")

* **Native AOT**: TUnit.FsCheck is not compatible with Native AOT publishing because FsCheck requires reflection and dynamic code generation
* **Parameter count**: Properties can have any number of parameters that FsCheck can generate

## When to Use Property-Based Testing[​](#when-to-use-property-based-testing "Direct link to When to Use Property-Based Testing")

Property-based testing is particularly useful for:

* Testing mathematical properties (commutativity, associativity, etc.)
* Serialization/deserialization round-trips
* Data structure invariants
* Parsing and formatting functions
* Any code where you can express general rules that should always hold
