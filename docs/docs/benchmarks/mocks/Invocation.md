---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 253.9 ns | 71.72 ns | 3.93 ns | 120 B |
| Imposter | 294.7 ns | 96.52 ns | 5.29 ns | 168 B |
| Mockolate | 636.9 ns | 51.45 ns | 2.82 ns | 640 B |
| Moq | 801.3 ns | 119.54 ns | 6.55 ns | 376 B |
| NSubstitute | 786.6 ns | 70.03 ns | 3.84 ns | 360 B |
| FakeItEasy | 1,831.1 ns | 1,410.44 ns | 77.31 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2198
  bar [253.9, 294.7, 636.9, 801.3, 786.6, 1831.1]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 152.1 ns | 71.37 ns | 3.91 ns | 88 B |
| Imposter | 297.7 ns | 18.57 ns | 1.02 ns | 168 B |
| Mockolate | 565.5 ns | 367.97 ns | 20.17 ns | 520 B |
| Moq | 546.0 ns | 102.03 ns | 5.59 ns | 296 B |
| NSubstitute | 609.8 ns | 312.03 ns | 17.10 ns | 272 B |
| FakeItEasy | 1,577.0 ns | 75.19 ns | 4.12 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1893
  bar [152.1, 297.7, 565.5, 546, 609.8, 1577]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 25,888.9 ns | 9,978.49 ns | 546.95 ns | 11936 B |
| Imposter | 29,197.1 ns | 12,265.18 ns | 672.30 ns | 16800 B |
| Mockolate | 67,594.1 ns | 33,440.75 ns | 1,833.00 ns | 64000 B |
| Moq | 83,544.0 ns | 24,502.47 ns | 1,343.06 ns | 37600 B |
| NSubstitute | 71,649.6 ns | 40,465.69 ns | 2,218.06 ns | 30848 B |
| FakeItEasy | 187,899.2 ns | 42,491.61 ns | 2,329.11 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 225480
  bar [25888.9, 29197.1, 67594.1, 83544, 71649.6, 187899.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-29T03:24:49.990Z*
