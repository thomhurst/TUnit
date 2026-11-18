---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 567.03 ns | 11.142 ns | 10.422 ns | 3040 B |
| Imposter | 214.73 ns | 2.090 ns | 1.853 ns | 1096 B |
| Mockolate | 320.95 ns | 4.640 ns | 4.340 ns | 1824 B |
| Moq | 135,140.36 ns | 631.220 ns | 590.444 ns | 24314 B |
| NSubstitute | 4,999.91 ns | 95.744 ns | 106.419 ns | 10024 B |
| FakeItEasy | 3,966.32 ns | 73.074 ns | 68.353 ns | 10501 B |

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
  title "Verification Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 162169
  bar [567.03, 214.73, 320.95, 135140.36, 4999.91, 3966.32]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 42.60 ns | 0.841 ns | 1.719 ns | 336 B |
| Imposter | 109.34 ns | 2.176 ns | 2.673 ns | 616 B |
| Mockolate | 181.87 ns | 2.742 ns | 2.564 ns | 1040 B |
| Moq | 33,501.78 ns | 264.746 ns | 247.643 ns | 6914 B |
| NSubstitute | 2,319.19 ns | 15.077 ns | 14.103 ns | 7056 B |
| FakeItEasy | 1,856.54 ns | 6.237 ns | 5.529 ns | 5119 B |

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
  title "Verification (Never) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 40203
  bar [42.6, 109.34, 181.87, 33501.78, 2319.19, 1856.54]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 939.21 ns | 2.763 ns | 2.584 ns | 4528 B |
| Imposter | 426.38 ns | 3.664 ns | 3.427 ns | 2288 B |
| Mockolate | 627.20 ns | 4.479 ns | 3.970 ns | 3264 B |
| Moq | 176,049.31 ns | 1,440.891 ns | 1,347.810 ns | 34847 B |
| NSubstitute | 8,230.25 ns | 76.971 ns | 71.998 ns | 16633 B |
| FakeItEasy | 7,224.92 ns | 142.350 ns | 194.850 ns | 18716 B |

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
  title "Verification (Multiple) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 211260
  bar [939.21, 426.38, 627.2, 176049.31, 8230.25, 7224.92]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-18T02:25:26.946Z*
