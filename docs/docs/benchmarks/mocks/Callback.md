---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 555.2 ns | 6.44 ns | 5.71 ns | 3.11 KB |
| Imposter | 384.1 ns | 6.90 ns | 6.45 ns | 2.66 KB |
| Mockolate | 280.2 ns | 5.47 ns | 8.01 ns | 1.8 KB |
| Moq | 107,389.9 ns | 589.29 ns | 522.39 ns | 13.29 KB |
| NSubstitute | 3,478.1 ns | 68.91 ns | 64.46 ns | 7.85 KB |
| FakeItEasy | 3,680.7 ns | 65.22 ns | 57.82 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 128868
  bar [555.2, 384.1, 280.2, 107389.9, 3478.1, 3680.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 707.6 ns | 10.56 ns | 9.87 ns | 3.2 KB |
| Imposter | 425.3 ns | 2.47 ns | 2.31 ns | 2.82 KB |
| Mockolate | 299.4 ns | 1.10 ns | 0.91 ns | 1.84 KB |
| Moq | 113,266.2 ns | 439.13 ns | 366.70 ns | 13.76 KB |
| NSubstitute | 3,736.1 ns | 32.94 ns | 29.20 ns | 8.41 KB |
| FakeItEasy | 4,466.9 ns | 22.19 ns | 19.67 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 135920
  bar [707.6, 425.3, 299.4, 113266.2, 3736.1, 4466.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-20T03:22:58.159Z*
