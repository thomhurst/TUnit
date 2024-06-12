---
sidebar_position: 8
---

# Test Filters

Running TUnit via `dotnet run` supports test filters. Information on how to use them is available [Here](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests)

TUnit can select tests by:

- Assembly
- Namespace
- Class name
- Test name

The syntax is (without the angled brackets) `/<Assembly>/<Namespace>/<Class name>/<Test name>`

Will cards are also supported with `*`

So an example could be:

`dotnet run /*/*/LoginTests/*` - To run all tests in the class `LoginTests`

or

`dotnet run /*/*/*/AcceptCookiesTest` - To run all tests with the name `AcceptCookiesTest`

TUnit also supports filtering by your own [properties](properties). So you could do:

`dotnet run --treenode-filter /*/*/*/*[MyFilterName=*SomeValue*]`

And if your test had a property with the name "MyFilterName" and its value contained "SomeValue", then your test would be executed.
