# Skipping Tests

If you want to simply skip a test, just place a `[Skip(reason)]` attribute on your test with an explanation of why you're skipping it.

```
using TUnit.Core;



namespace MyTestProject;



public class MyTestClass

{

    [Test, Skip("There's a bug! See issue #1")]

    public async Task MyTest()

    {

        ...

    }

}
```

## Custom Logic[​](#custom-logic "Direct link to Custom Logic")

The `SkipAttribute` can be inherited and custom logic plugged into it, so it only skips the test if it meets certain criteria.

As an example, this could be used to skip tests on certain operating systems.

```
public class WindowsOnlyAttribute() : SkipAttribute("This test is only supported on Windows")

{

    public override Task<bool> ShouldSkip(TestRegisteredContext context)

    {

        return Task.FromResult(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

    }

}
```

```
using TUnit.Core;



namespace MyTestProject;



public class MyTestClass

{

    [Test, WindowsOnly]

    public async Task MyTest()

    {

        ...

    }

}
```

## Global Skipping[​](#global-skipping "Direct link to Global Skipping")

In case you want to skip all tests in a project, you can add the attribute on the assembly level.

```
[assembly: Skip("Skipping all tests in this assembly")]
```

Or you can skip all the tests in a class like this:

```
[Skip("Skipping all tests in this class")]

public class MyTestClass

{

}
```

## Dynamic Skipping at Runtime[​](#dynamic-skipping-at-runtime "Direct link to Dynamic Skipping at Runtime")

Sometimes you need to determine whether to skip a test at runtime based on conditions that aren't known until the test executes. For this, you can use the static `Skip.Test(reason)` method.

### Skip.Test()[​](#skiptest "Direct link to Skip.Test()")

The `Skip.Test(reason)` method allows you to dynamically skip a test from within the test method or hooks. When called, it throws a `SkipTestException` that the test framework catches and marks the test as skipped.

```
using TUnit.Core;



namespace MyTestProject;



public class MyTestClass

{

    [Test]

    public async Task MyTest()

    {

        var apiAvailable = await CheckApiAvailability();

        

        if (!apiAvailable)

        {

            Skip.Test("API is not available");

        }

        

        // Test continues only if API is available

        await CallApi();

    }

}
```

### Skipping from Hooks[​](#skipping-from-hooks "Direct link to Skipping from Hooks")

You can also use `Skip.Test()` in test hooks to skip tests based on setup conditions:

```
using TUnit.Core;



namespace MyTestProject;



public class MyTestClass

{

    [Before(Test)]

    public void BeforeEachTest()

    {

        var databaseAvailable = CheckDatabaseConnection();

        

        if (!databaseAvailable)

        {

            Skip.Test("Database is not available");

        }

    }



    [Test]

    public void Test1()

    {

        // This test will be skipped if database is unavailable

    }



    [Test]

    public void Test2()

    {

        // This test will also be skipped if database is unavailable

    }

}
```

You can skip all tests in a class from a `Before(Class)` hook:

```
using TUnit.Core;



namespace MyTestProject;



public class MyTestClass

{

    [Before(Class)]

    public static void BeforeAllTests()

    {

        var serviceAvailable = CheckExternalService();

        

        if (!serviceAvailable)

        {

            Skip.Test("External service is not available");

        }

    }



    [Test]

    public void Test1()

    {

        // All tests in this class will be skipped if service is unavailable

    }

}
```

### When to Use Dynamic Skipping[​](#when-to-use-dynamic-skipping "Direct link to When to Use Dynamic Skipping")

Use `Skip.Test()` and its variants when:

* The skip condition depends on runtime state (external services, environment variables, etc.)
* You need to perform some logic or API calls to determine if a test should run
* The skip decision is based on test setup or initialization results

Use `[Skip]` attribute when:

* The skip condition is known at compile time or discovery time
* You want to skip tests based on static configuration or platform checks
* You need custom skip logic in a reusable attribute
