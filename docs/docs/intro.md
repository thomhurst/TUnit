# Intro

**TUnit** is another testing framework for C# / .NET.
Use it for unit testing, integration testing, acceptance testing, or anything else.

TUnit provides flexible ways to inject test data (new or shared instances) and hooks to run before and after tests, with minimal opinions on test style.

Built on Microsoft.Testing.Platform for simpler, more extensible .NET testing.

:::performance
TUnit is designed for speed. Through source generation and compile-time optimizations, TUnit significantly outperforms traditional testing frameworks. See the [performance benchmarks](/docs/benchmarks) for real-world speed comparisons.
:::

## Install the agent skill

Install the **TUnit agent skill** to help your coding agent write, migrate, and troubleshoot TUnit tests. It fetches only the relevant Markdown pages from this documentation site, so detailed API guidance and examples stay up to date without loading the entire documentation into your agent's context. Network access is required to fetch those pages.

With a GitHub CLI version that supports [`gh skill`](https://cli.github.com/manual/gh_skill), preview the skill and install it:

```sh
gh skill preview thomhurst/TUnit tunit
gh skill install thomhurst/TUnit tunit
```

Use `--agent` and `--scope` to choose the installation target. Installation defaults to the latest release; until a release includes the skill, append `@main` to `tunit` in both commands to use the default branch. See the [installation options](https://cli.github.com/manual/gh_skill_install).

## What's in These Docs

- **[Getting Started](getting-started/installation.md)** — Install TUnit, write your first test, and run it
- **[Writing Tests](writing-tests/things-to-know.md)** — Test data, lifecycle hooks, dependency injection, parallelism, and mocking
- **[Assertions](assertions/getting-started.md)** — Fluent assertion syntax for values, collections, strings, exceptions, and more
- **[Running Tests](execution/test-filters.md)** — Filters, timeouts, retries, CI/CD reporting, and AOT
- **[Integrations](examples/aspnet.md)** — ASP.NET Core, Aspire, Playwright, and other integration examples
- **[Extending TUnit](extending/extension-points.md)** — Custom data sources, formatters, and event subscribers
- **[Comparing Frameworks](comparison/framework-differences.md)** — Feature comparisons with xUnit, NUnit, and MSTest
- **[Migration](migration/xunit.md)** — Step-by-step guides for switching from xUnit, NUnit, or MSTest
