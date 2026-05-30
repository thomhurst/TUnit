---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 264.97 ns | 139.64 ns | 7.654 ns | 120 B |
| Imposter | 291.89 ns | 61.66 ns | 3.380 ns | 168 B |
| Mockolate | 104.91 ns | 25.36 ns | 1.390 ns | 84 B |
| Moq | 837.80 ns | 186.33 ns | 10.213 ns | 376 B |
| NSubstitute | 746.03 ns | 338.50 ns | 18.555 ns | 304 B |
| FakeItEasy | 1,729.42 ns | 1,311.15 ns | 71.868 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2076
  bar [264.97, 291.89, 104.91, 837.8, 746.03, 1729.42]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 161.43 ns | 52.21 ns | 2.862 ns | 88 B |
| Imposter | 292.30 ns | 80.24 ns | 4.398 ns | 168 B |
| Mockolate | 98.52 ns | 48.95 ns | 2.683 ns | 60 B |
| Moq | 579.18 ns | 434.82 ns | 23.834 ns | 296 B |
| NSubstitute | 617.90 ns | 42.50 ns | 2.329 ns | 272 B |
| FakeItEasy | 1,550.73 ns | 397.55 ns | 21.791 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1861
  bar [161.43, 292.3, 98.52, 579.18, 617.9, 1550.73]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,290.97 ns | 8,952.56 ns | 490.720 ns | 11936 B |
| Imposter | 28,718.82 ns | 8,191.32 ns | 448.994 ns | 16800 B |
| Mockolate | 10,815.48 ns | 3,060.86 ns | 167.776 ns | 8400 B |
| Moq | 82,620.36 ns | 30,680.75 ns | 1,681.715 ns | 37600 B |
| NSubstitute | 77,829.77 ns | 9,837.85 ns | 539.246 ns | 36448 B |
| FakeItEasy | 178,618.18 ns | 26,804.30 ns | 1,469.234 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 214342
  bar [26290.97, 28718.82, 10815.48, 82620.36, 77829.77, 178618.18]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-30T03:25:40.021Z*
