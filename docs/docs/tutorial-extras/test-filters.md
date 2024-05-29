<!-- ---
sidebar_position: 8
---

# Test Filters

Running TUnit via `dotnet test` supports test filters. Information on how to use them is available [Here](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests)

TUnit supports the following properties for filtering:

- TestName
- TestClass
- Category

TUnit also supports filtering by your own [properties](properties). So you could do:

`dotnet test --filter "MyFilterName~SomeValue"`

And if your test had a property with the name "MyFilterName" and its value contained "SomeValue", then your test would be executed. -->