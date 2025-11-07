# Intro

**TUnit** is another testing framework for C# / .NET.
It can be used for unit testing, integration testing, acceptance testing, you name it.

It provides you a skeleton framework to write, execute and assert tests, with little opinion on how your tests should be. In fact, it aims to be flexible, giving you various ways to inject test data, such as options for new data or shared instances, and a variety of hooks to run before and after tests.

That means you get more control over your setup, execution, and style of tests.

It is also built on top of the newer Microsoft Testing Platform, which was rewritten to make .NET testing simpler and more extensible.

:::performance
TUnit is designed for speed. Through source generation and compile-time optimizations, TUnit significantly outperforms traditional testing frameworks. See the [performance benchmarks](/docs/benchmarks) or try the [benchmark calculator](/docs/benchmarks/calculator) to estimate time savings for your test suite.
:::
