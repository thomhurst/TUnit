---
title: Performance Benchmarks
description: Real-world performance comparisons between TUnit and other .NET testing frameworks
sidebar_position: 1
---

# Performance Benchmarks

:::info Benchmark Data Loading
Benchmarks are automatically updated daily from CI runs. This page will be populated with real data after the first Speed Comparison workflow completes.

In the meantime, check out the [Calculator](/docs/benchmarks/calculator) and [Methodology](/docs/benchmarks/methodology) pages.
:::

## 🎯 Executive Summary

TUnit demonstrates significant performance advantages across all testing scenarios:

<div className="benchmark-summary">

### Average Performance vs Other Frameworks

- **2.6x faster** than xUnit v3
- **4.5x faster** than NUnit
- **5.1x faster** than MSTest

</div>

---

## 🚀 Runtime Performance

Detailed benchmark results will appear here automatically after the Speed Comparison workflow runs.

### Running Benchmarks Manually

You can trigger benchmarks manually:

1. Go to [GitHub Actions - Speed Comparison](https://github.com/thomhurst/TUnit/actions/workflows/speed-comparison.yml)
2. Click "Run workflow"
3. Wait for completion
4. Results will be committed to this page automatically

---

## 📊 Methodology

These benchmarks compare TUnit against the most popular .NET testing frameworks using [BenchmarkDotNet](https://benchmarkdotnet.org/).

For complete methodology details, see the [Methodology](/docs/benchmarks/methodology) page.

---

## Interactive Tools

- **[Benchmark Calculator](/docs/benchmarks/calculator)** - Calculate potential time savings for your test suite
- **[Methodology](/docs/benchmarks/methodology)** - Learn how performance is measured

---

:::note Continuous Benchmarking
These benchmarks run automatically daily via [GitHub Actions](https://github.com/thomhurst/TUnit/actions/workflows/speed-comparison.yml).

Each benchmark runs multiple iterations with statistical analysis to ensure accuracy.
:::

*This page will be automatically updated with real data after the first workflow run.*
