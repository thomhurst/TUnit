---
title: "Mock Benchmark: MockCreation"
description: "Mock instance creation performance — TUnit.Mocks vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# MockCreation Benchmark

:::info Awaiting First Run
This benchmark will be automatically populated after the first [Mock Benchmarks workflow](https://github.com/thomhurst/TUnit/actions/workflows/mock-benchmarks.yml) run completes.
:::

## 📊 About This Benchmark

This benchmark measures how fast each mocking library can create mock instances of interfaces. This is critical for test performance since every test typically creates one or more mocks.

**Libraries compared:**
- **TUnit.Mocks** — Source-generated mock factories (no runtime proxy generation)
- **Moq** — Runtime proxy generation via Castle.DynamicProxy
- **NSubstitute** — Runtime proxy generation via Castle.DynamicProxy
- **FakeItEasy** — Runtime proxy generation via Castle.DynamicProxy

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::
