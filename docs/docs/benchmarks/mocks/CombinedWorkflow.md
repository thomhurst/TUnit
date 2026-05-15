---
title: "Mock Benchmark: CombinedWorkflow"
description: "Full workflow: create → setup → invoke → verify — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 3
---

# CombinedWorkflow Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Full workflow: create → setup → invoke → verify:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1.890 μs | 0.0251 μs | 0.0235 μs | 5.8 KB |
| Imposter | 3.106 μs | 0.0599 μs | 0.0615 μs | 15.71 KB |
| Mockolate | 1.849 μs | 0.0354 μs | 0.0379 μs | 7.63 KB |
| Moq | 314.666 μs | 1.2275 μs | 1.0250 μs | 36.42 KB |
| NSubstitute | 16.609 μs | 0.0883 μs | 0.0737 μs | 26.72 KB |
| FakeItEasy | 17.355 μs | 0.1014 μs | 0.0899 μs | 25.5 KB |

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
  y-axis "Time (μs)" 0 --> 378
  bar [1.89, 3.106, 1.849, 314.666, 16.609, 17.355]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-15T03:27:25.234Z*
