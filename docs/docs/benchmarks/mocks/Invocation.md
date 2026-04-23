---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 299.7 ns | 176.28 ns | 9.66 ns | 120 B |
| Imposter | 323.5 ns | 56.89 ns | 3.12 ns | 168 B |
| Mockolate | 671.7 ns | 149.95 ns | 8.22 ns | 640 B |
| Moq | 805.6 ns | 325.32 ns | 17.83 ns | 376 B |
| NSubstitute | 756.5 ns | 199.31 ns | 10.92 ns | 304 B |
| FakeItEasy | 1,746.5 ns | 462.47 ns | 25.35 ns | 944 B |

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
  title "Invocation Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 2096
  bar [299.7, 323.5, 671.7, 805.6, 756.5, 1746.5]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 179.0 ns | 155.98 ns | 8.55 ns | 88 B |
| Imposter | 347.6 ns | 33.74 ns | 1.85 ns | 168 B |
| Mockolate | 591.6 ns | 184.08 ns | 10.09 ns | 520 B |
| Moq | 573.2 ns | 339.92 ns | 18.63 ns | 296 B |
| NSubstitute | 680.1 ns | 197.95 ns | 10.85 ns | 272 B |
| FakeItEasy | 1,638.9 ns | 314.74 ns | 17.25 ns | 776 B |

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
  title "Invocation (String) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 1967
  bar [179, 347.6, 591.6, 573.2, 680.1, 1638.9]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 29,882.5 ns | 24,666.48 ns | 1,352.05 ns | 11936 B |
| Imposter | 33,114.8 ns | 8,763.30 ns | 480.35 ns | 16800 B |
| Mockolate | 68,052.9 ns | 15,200.15 ns | 833.17 ns | 64000 B |
| Moq | 79,886.0 ns | 51,344.86 ns | 2,814.38 ns | 37600 B |
| NSubstitute | 72,527.9 ns | 30,818.18 ns | 1,689.25 ns | 30848 B |
| FakeItEasy | 183,607.0 ns | 95,656.91 ns | 5,243.28 ns | 94400 B |

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
  title "Invocation (100 calls) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 220329
  bar [29882.5, 33114.8, 68052.9, 79886, 72527.9, 183607]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-23T03:25:34.373Z*
