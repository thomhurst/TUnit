# Test Filters

Running TUnit via `dotnet run` supports test filters.

TUnit can select tests by:

- Assembly
- Namespace
- Class name
- Test name

You must use the `--treenode-filter` flag on the command line.

The syntax for the filter value is (without the angled brackets) `/<Assembly>/<Namespace>/<Class name>/<Test name>`

Will cards are also supported with `*`

As well as `and`, `or`, `starts with`, `ends with`, `equals` and other operators. For full information on the treenode filters, see [here](https://github.com/microsoft/testfx/blob/main/docs/mstest-runner-graphqueryfiltering/graph-query-filtering.md)

So an example could be:

`dotnet run --treenode-filter /*/*/LoginTests/*` - To run all tests in the class `LoginTests`

or

`dotnet run --treenode-filter /*/*/*/AcceptCookiesTest` - To run all tests with the name `AcceptCookiesTest`

TUnit also supports filtering by your own [properties](properties). So you could do:

`dotnet run --treenode-filter /*/*/*/*[MyFilterName=*SomeValue*]`

And if your test had a property with the name "MyFilterName" and its value contained "SomeValue", then your test would be executed.

