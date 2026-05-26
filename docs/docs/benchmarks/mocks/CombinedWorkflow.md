---
title: "Mock Benchmark: CombinedWorkflow"
description: "Full workflow: create → setup → invoke → verify — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 3
---

# CombinedWorkflow Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Full workflow: create → setup → invoke → verify:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1.865 μs | 0.0367 μs | 0.0360 μs | 6.11 KB |
| Imposter | 2.639 μs | 0.0528 μs | 0.0925 μs | 15.71 KB |
| Mockolate | 1.714 μs | 0.0338 μs | 0.0601 μs | 7.63 KB |
| Moq | 316.243 μs | 2.2704 μs | 2.0126 μs | 36.16 KB |
| NSubstitute | 16.613 μs | 0.1080 μs | 0.1010 μs | 26.72 KB |
| FakeItEasy | 15.737 μs | 0.2489 μs | 0.2328 μs | 25.52 KB |

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#1f2937',
    'primaryBorderColor': '#1e40af',
    'lineColor': '#6b7280',
    'secondaryColor': '#7c3aed',
    'tertiaryColor': '#dc2626',
    'background': '#ffffff',
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1'
  }
}}%%
xychart-beta
  title "CombinedWorkflow Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (μs)" 0 --> 380
  bar [1.865, 2.639, 1.714, 316.243, 16.613, 15.737]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-26T03:27:58.119Z*
