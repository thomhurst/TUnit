---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 316.8 ns | 167.39 ns | 9.18 ns | 208 B |
| Imposter | 288.1 ns | 62.75 ns | 3.44 ns | 168 B |
| Mockolate | 633.1 ns | 89.16 ns | 4.89 ns | 640 B |
| Moq | 798.1 ns | 253.28 ns | 13.88 ns | 376 B |
| NSubstitute | 758.5 ns | 233.20 ns | 12.78 ns | 360 B |
| FakeItEasy | 1,708.6 ns | 443.53 ns | 24.31 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2051
  bar [316.8, 288.1, 633.1, 798.1, 758.5, 1708.6]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 270.7 ns | 124.78 ns | 6.84 ns | 144 B |
| Imposter | 292.3 ns | 86.39 ns | 4.74 ns | 168 B |
| Mockolate | 525.3 ns | 174.71 ns | 9.58 ns | 520 B |
| Moq | 517.8 ns | 335.71 ns | 18.40 ns | 296 B |
| NSubstitute | 592.5 ns | 212.16 ns | 11.63 ns | 272 B |
| FakeItEasy | 1,522.2 ns | 536.69 ns | 29.42 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1827
  bar [270.7, 292.3, 525.3, 517.8, 592.5, 1522.2]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 31,365.4 ns | 17,153.66 ns | 940.25 ns | 21248 B |
| Imposter | 28,902.8 ns | 10,632.48 ns | 582.80 ns | 16800 B |
| Mockolate | 65,972.9 ns | 18,196.73 ns | 997.42 ns | 64000 B |
| Moq | 80,423.8 ns | 4,121.47 ns | 225.91 ns | 37600 B |
| NSubstitute | 73,410.9 ns | 11,495.85 ns | 630.13 ns | 30848 B |
| FakeItEasy | 181,158.0 ns | 73,020.55 ns | 4,002.50 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 217390
  bar [31365.4, 28902.8, 65972.9, 80423.8, 73410.9, 181158]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-05T11:44:06.333Z*
