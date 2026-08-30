# Boolean Assertions

TUnit provides simple, expressive assertions for testing boolean values. These assertions work with both `bool` and `bool?` (nullable boolean) types.

## Basic Boolean Assertions[​](#basic-boolean-assertions "Direct link to Basic Boolean Assertions")

### IsTrue[​](#istrue "Direct link to IsTrue")

Tests that a boolean value is `true`:

```
[Test]

public async Task Value_Is_True()

{

    var isValid = ValidateInput("test@example.com");

    await Assert.That(isValid).IsTrue();



    var hasPermission = user.HasPermission("write");

    await Assert.That(hasPermission).IsTrue();

}
```

### IsFalse[​](#isfalse "Direct link to IsFalse")

Tests that a boolean value is `false`:

```
[Test]

public async Task Value_Is_False()

{

    var isExpired = CheckIfExpired(futureDate);

    await Assert.That(isExpired).IsFalse();



    var isEmpty = list.Length == 0;

    await Assert.That(isEmpty).IsFalse();

}
```

## Alternative: Using IsEqualTo[​](#alternative-using-isequalto "Direct link to Alternative: Using IsEqualTo")

You can also use `IsEqualTo()` for boolean comparisons:

```
[Test]

public async Task Using_IsEqualTo()

{

    var result = PerformCheck();



    await Assert.That(result).IsEqualTo(true);

    // Same as: await Assert.That(result).IsTrue();



    await Assert.That(result).IsEqualTo(false);

    // Same as: await Assert.That(result).IsFalse();

}
```

However, `IsTrue()` and `IsFalse()` are more expressive and recommended for boolean values.

## Nullable Booleans[​](#nullable-booleans "Direct link to Nullable Booleans")

Both assertions work with nullable booleans (`bool?`):

```
[Test]

public async Task Nullable_Boolean_True()

{

    bool? result = GetOptionalFlag();



    await Assert.That(result).IsTrue();

    // This asserts both:

    // 1. result is not null

    // 2. result.Value is true

}



[Test]

public async Task Nullable_Boolean_False()

{

    bool? result = GetOptionalFlag();



    await Assert.That(result).IsFalse();

    // This asserts both:

    // 1. result is not null

    // 2. result.Value is false

}
```

### Null Nullable Booleans[​](#null-nullable-booleans "Direct link to Null Nullable Booleans")

If a nullable boolean is `null`, both `IsTrue()` and `IsFalse()` will fail:

```
[Test]

public async Task Nullable_Boolean_Null()

{

    bool? result = null;



    // These will both fail:

    // await Assert.That(result).IsTrue();  // ❌ Fails - null is not true

    // await Assert.That(result).IsFalse(); // ❌ Fails - null is not false



    // Check for null first:

    await Assert.That(result).IsNull();

}
```

## Chaining Boolean Assertions[​](#chaining-boolean-assertions "Direct link to Chaining Boolean Assertions")

Boolean assertions can be chained with other assertions:

```
[Test]

public async Task Chained_With_Other_Assertions()

{

    bool? flag = GetFlag();



    await Assert.That(flag)

        .IsNotNull()

        .And.IsTrue();

}
```

## Practical Examples[​](#practical-examples "Direct link to Practical Examples")

### Validation Results[​](#validation-results "Direct link to Validation Results")

```
[Test]

public async Task Email_Validation()

{

    var isValid = ValidateEmail("test@example.com");

    await Assert.That(isValid).IsTrue();



    var isInvalid = ValidateEmail("not-an-email");

    await Assert.That(isInvalid).IsFalse();

}
```

### Permission Checks[​](#permission-checks "Direct link to Permission Checks")

```
[Test]

public async Task User_Permissions()

{

    var user = await GetUserAsync("alice");



    await Assert.That(user.CanRead).IsTrue();

    await Assert.That(user.CanWrite).IsTrue();

    await Assert.That(user.CanDelete).IsFalse();

}
```

### State Flags[​](#state-flags "Direct link to State Flags")

```
[Test]

public async Task Service_State()

{

    var service = new ExampleBackgroundService();



    await Assert.That(service.IsRunning).IsFalse();



    await service.StartAsync();



    await Assert.That(service.IsRunning).IsTrue();

}
```

### Feature Flags[​](#feature-flags "Direct link to Feature Flags")

```
[Test]

public async Task Feature_Toggles()

{

    var config = LoadConfiguration();



    await Assert.That(config.EnableNewFeature).IsTrue();

    await Assert.That(config.EnableBetaFeature).IsFalse();

}
```

## Tip: Prefer Specific Assertions[​](#tip-prefer-specific-assertions "Direct link to Tip: Prefer Specific Assertions")

When testing the boolean result of a comparison, use the specific assertion instead for clearer failure messages:

```
[Test]

public async Task Prefer_Specific_Assertions()

{

    var count = GetCount();



    // Less clear — failure message says "expected true but was false":

    await Assert.That(count > 0).IsTrue();



    // More clear — failure message shows the actual value:

    await Assert.That(count).IsGreaterThan(0);

}
```

Use `IsTrue()` / `IsFalse()` for actual boolean values and flags. For comparisons, collections, strings, and types, TUnit provides [dedicated assertions](/docs/assertions/collections.md) with better failure messages.

## See Also[​](#see-also "Direct link to See Also")

* [Equality & Comparison](/docs/assertions/equality-and-comparison.md) - General equality testing
* [Null & Default](/docs/assertions/null-and-default.md) - Testing for null values
* [Collections](/docs/assertions/collections.md) - Collection-specific boolean tests (All, Any)
* [Type Assertions](/docs/assertions/types.md) - Type checking instead of `is` checks
