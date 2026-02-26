# Intro

**TUnit** is another testing framework for C# / .NET.
It can be used for unit testing, integration testing, acceptance testing, you name it.

It provides you a skeleton framework to write, execute and assert tests, with little opinion on how your tests should be. In fact, it aims to be flexible, giving you various ways to inject test data, such as options for new data or shared instances, and a variety of hooks to run before and after tests.

That means you get more control over your setup, execution, and style of tests.

It is also built on top of the newer Microsoft Testing Platform, which was rewritten to make .NET testing simpler and more extensible.

:::performance
TUnit is designed for speed. Through source generation and compile-time optimizations, TUnit significantly outperforms traditional testing frameworks. See the [performance benchmarks](/docs/benchmarks) for real-world speed comparisons.
:::

## What's in These Docs

- **[Getting Started](getting-started/installation.md)** — Install TUnit, write your first test, and run it
- **[Writing Tests](writing-tests/things-to-know.md)** — Test attributes, data-driven testing, lifecycle hooks, and dependency injection
- **[Assertions](assertions/getting-started.md)** — Fluent assertion syntax for values, collections, strings, exceptions, and more
- **[Execution](execution/parallelism.md)** — Control parallelism, ordering, retries, and timeouts
- **[Extending TUnit](extending/built-in-extensions.md)** — Built-in extensions, custom data sources, and event subscribers
- **[Migration](migration/xunit.md)** — Guides for switching from xUnit, NUnit, or MSTest
- **[Comparison](comparison/framework-differences.md)** — Feature comparisons with other frameworks
- **[Guides](guides/best-practices.md)** — Best practices, cookbook recipes, and philosophy
