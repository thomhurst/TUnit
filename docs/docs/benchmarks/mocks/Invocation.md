---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 270.4 ns | 87.16 ns | 4.78 ns | 120 B |
| Imposter | 312.3 ns | 105.21 ns | 5.77 ns | 168 B |
| Mockolate | 118.1 ns | 92.05 ns | 5.05 ns | 84 B |
| Moq | 868.5 ns | 221.16 ns | 12.12 ns | 376 B |
| NSubstitute | 774.5 ns | 142.72 ns | 7.82 ns | 304 B |
| FakeItEasy | 1,983.0 ns | 338.98 ns | 18.58 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2380
  bar [270.4, 312.3, 118.1, 868.5, 774.5, 1983]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 161.5 ns | 80.59 ns | 4.42 ns | 88 B |
| Imposter | 312.3 ns | 99.51 ns | 5.45 ns | 168 B |
| Mockolate | 124.4 ns | 58.79 ns | 3.22 ns | 60 B |
| Moq | 630.9 ns | 297.78 ns | 16.32 ns | 296 B |
| NSubstitute | 734.8 ns | 103.10 ns | 5.65 ns | 328 B |
| FakeItEasy | 1,806.2 ns | 195.11 ns | 10.69 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2168
  bar [161.5, 312.3, 124.4, 630.9, 734.8, 1806.2]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,090.1 ns | 10,775.05 ns | 590.62 ns | 11936 B |
| Imposter | 30,628.9 ns | 6,499.86 ns | 356.28 ns | 16800 B |
| Mockolate | 13,117.9 ns | 4,809.80 ns | 263.64 ns | 8400 B |
| Moq | 88,139.7 ns | 19,796.37 ns | 1,085.11 ns | 37600 B |
| NSubstitute | 79,799.2 ns | 27,093.46 ns | 1,485.08 ns | 30848 B |
| FakeItEasy | 189,034.3 ns | 100,257.58 ns | 5,495.46 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 226842
  bar [27090.1, 30628.9, 13117.9, 88139.7, 79799.2, 189034.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-27T03:29:35.677Z*
