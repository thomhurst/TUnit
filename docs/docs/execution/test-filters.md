# Test Filters

Running TUnit via `dotnet run` supports test filters.

TUnit can select tests by:

- Assembly
- Namespace
- Class name
- Test name

You must use the `--treenode-filter` flag on the command line.

The syntax for the filter value is (without the angled brackets) `/<Assembly>/<Namespace>/<Class name>/<Test name>`

Wildcards are also supported with `*`

## Filter Operators

TUnit supports several operators for building complex filters:

- **Wildcard matching:** Use `*` for pattern matching (e.g., `LoginTests*` matches `LoginTests`, `LoginTestsSuite`, etc.)
- **Equality:** Use `=` for exact match (e.g., `[Category=Unit]`)
- **Negation:** Use `!=` for excluding values (e.g., `[Category!=Performance]`)
- **AND operator:** Use `&` to combine conditions (e.g., `[Category=Unit]&[Priority=High]`)
- **OR operator:** Use `|` to match either condition within a single path segment - requires parentheses (e.g., `/*/*/(Class1)|(Class2)/*`)

For full information on the treenode filters, see [Microsoft's documentation](https://github.com/microsoft/testfx/blob/main/docs/mstest-runner-graphqueryfiltering/graph-query-filtering.md)

So an example could be:

`dotnet run --treenode-filter /*/*/LoginTests/*` - To run all tests in the class `LoginTests`

or

`dotnet run --treenode-filter /*/*/*/AcceptCookiesTest` - To run all tests with the name `AcceptCookiesTest`

TUnit also supports filtering by your own [properties](../test-lifecycle/properties.md). So you could do:

`dotnet run --treenode-filter /*/*/*/*[MyFilterName=*SomeValue*]`

And if your test had a property with the name "MyFilterName" and its value contained "SomeValue", then your test would be executed.

