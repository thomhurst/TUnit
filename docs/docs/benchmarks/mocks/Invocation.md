---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 277.91 ns | 168.22 ns | 9.221 ns | 128 B |
| Imposter | 305.66 ns | 215.84 ns | 11.831 ns | 168 B |
| Mockolate | 106.57 ns | 20.53 ns | 1.125 ns | 84 B |
| Moq | 836.41 ns | 418.43 ns | 22.935 ns | 376 B |
| NSubstitute | 743.31 ns | 675.53 ns | 37.028 ns | 304 B |
| FakeItEasy | 1,766.19 ns | 97.51 ns | 5.345 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2120
  bar [277.91, 305.66, 106.57, 836.41, 743.31, 1766.19]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 173.91 ns | 68.45 ns | 3.752 ns | 96 B |
| Imposter | 298.33 ns | 101.31 ns | 5.553 ns | 168 B |
| Mockolate | 98.90 ns | 56.78 ns | 3.112 ns | 60 B |
| Moq | 525.99 ns | 63.37 ns | 3.474 ns | 296 B |
| NSubstitute | 610.15 ns | 231.29 ns | 12.678 ns | 272 B |
| FakeItEasy | 1,637.09 ns | 505.21 ns | 27.692 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1965
  bar [173.91, 298.33, 98.9, 525.99, 610.15, 1637.09]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,230.90 ns | 10,653.15 ns | 583.935 ns | 12736 B |
| Imposter | 30,655.85 ns | 37,018.92 ns | 2,029.132 ns | 16800 B |
| Mockolate | 11,547.82 ns | 14,291.35 ns | 783.357 ns | 8400 B |
| Moq | 78,291.98 ns | 12,310.35 ns | 674.772 ns | 37600 B |
| NSubstitute | 78,791.90 ns | 12,109.72 ns | 663.774 ns | 36448 B |
| FakeItEasy | 174,269.11 ns | 40,974.48 ns | 2,245.949 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 209123
  bar [27230.9, 30655.85, 11547.82, 78291.98, 78791.9, 174269.11]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-04T03:31:56.363Z*
