---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 689.5 ns | 6.56 ns | 6.14 ns | 3.13 KB |
| Imposter | 459.8 ns | 1.45 ns | 1.29 ns | 2.66 KB |
| Mockolate | 525.1 ns | 1.52 ns | 1.27 ns | 1.8 KB |
| Moq | 135,900.5 ns | 637.16 ns | 564.82 ns | 13.29 KB |
| NSubstitute | 4,148.8 ns | 12.77 ns | 10.66 ns | 7.93 KB |
| FakeItEasy | 4,558.1 ns | 19.52 ns | 17.30 ns | 7.44 KB |

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
  title "Callback Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 163081
  bar [689.5, 459.8, 525.1, 135900.5, 4148.8, 4558.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 782.6 ns | 2.12 ns | 1.98 ns | 3.22 KB |
| Imposter | 557.1 ns | 1.88 ns | 1.76 ns | 2.82 KB |
| Mockolate | 753.7 ns | 1.33 ns | 1.04 ns | 2.13 KB |
| Moq | 141,620.2 ns | 1,333.37 ns | 1,182.00 ns | 13.73 KB |
| NSubstitute | 4,593.7 ns | 15.91 ns | 14.11 ns | 8.53 KB |
| FakeItEasy | 5,469.7 ns | 27.34 ns | 22.83 ns | 9.26 KB |

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
  title "Callback (with args) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 169945
  bar [782.6, 557.1, 753.7, 141620.2, 4593.7, 5469.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-16T03:23:00.282Z*
