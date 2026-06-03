---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 269.4 ns | 69.64 ns | 3.82 ns | 120 B |
| Imposter | 301.1 ns | 64.82 ns | 3.55 ns | 168 B |
| Mockolate | 104.4 ns | 45.03 ns | 2.47 ns | 84 B |
| Moq | 826.3 ns | 446.43 ns | 24.47 ns | 376 B |
| NSubstitute | 753.0 ns | 69.80 ns | 3.83 ns | 304 B |
| FakeItEasy | 1,977.5 ns | 1,199.87 ns | 65.77 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2373
  bar [269.4, 301.1, 104.4, 826.3, 753, 1977.5]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 160.7 ns | 82.30 ns | 4.51 ns | 88 B |
| Imposter | 302.4 ns | 78.44 ns | 4.30 ns | 168 B |
| Mockolate | 103.3 ns | 22.31 ns | 1.22 ns | 60 B |
| Moq | 556.3 ns | 151.96 ns | 8.33 ns | 296 B |
| NSubstitute | 688.3 ns | 79.80 ns | 4.37 ns | 328 B |
| FakeItEasy | 1,598.0 ns | 652.11 ns | 35.74 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1918
  bar [160.7, 302.4, 103.3, 556.3, 688.3, 1598]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,748.3 ns | 9,139.38 ns | 500.96 ns | 11936 B |
| Imposter | 30,135.1 ns | 5,668.88 ns | 310.73 ns | 16800 B |
| Mockolate | 12,760.5 ns | 2,436.35 ns | 133.54 ns | 8400 B |
| Moq | 89,445.5 ns | 52,932.26 ns | 2,901.40 ns | 37600 B |
| NSubstitute | 75,062.2 ns | 7,200.93 ns | 394.71 ns | 30848 B |
| FakeItEasy | 189,383.5 ns | 91,551.97 ns | 5,018.27 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 227261
  bar [26748.3, 30135.1, 12760.5, 89445.5, 75062.2, 189383.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-03T03:30:19.511Z*
