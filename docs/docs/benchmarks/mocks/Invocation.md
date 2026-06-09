---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 270.65 ns | 88.48 ns | 4.850 ns | 128 B |
| Imposter | 296.41 ns | 86.97 ns | 4.767 ns | 168 B |
| Mockolate | 105.51 ns | 19.26 ns | 1.056 ns | 84 B |
| Moq | 783.44 ns | 87.07 ns | 4.772 ns | 376 B |
| NSubstitute | 744.91 ns | 406.55 ns | 22.285 ns | 304 B |
| FakeItEasy | 1,753.45 ns | 120.34 ns | 6.596 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2105
  bar [270.65, 296.41, 105.51, 783.44, 744.91, 1753.45]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 172.98 ns | 57.66 ns | 3.161 ns | 96 B |
| Imposter | 297.06 ns | 73.57 ns | 4.032 ns | 168 B |
| Mockolate | 99.47 ns | 50.50 ns | 2.768 ns | 60 B |
| Moq | 543.52 ns | 291.05 ns | 15.954 ns | 296 B |
| NSubstitute | 614.20 ns | 440.45 ns | 24.143 ns | 272 B |
| FakeItEasy | 1,561.06 ns | 260.49 ns | 14.278 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1874
  bar [172.98, 297.06, 99.47, 543.52, 614.2, 1561.06]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,483.25 ns | 10,968.83 ns | 601.238 ns | 12736 B |
| Imposter | 29,486.85 ns | 9,488.24 ns | 520.082 ns | 16800 B |
| Mockolate | 10,781.25 ns | 2,024.28 ns | 110.957 ns | 8400 B |
| Moq | 80,958.08 ns | 61,252.80 ns | 3,357.472 ns | 37600 B |
| NSubstitute | 71,919.88 ns | 19,475.72 ns | 1,067.530 ns | 30848 B |
| FakeItEasy | 182,805.97 ns | 119,145.17 ns | 6,530.749 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 219368
  bar [27483.25, 29486.85, 10781.25, 80958.08, 71919.88, 182805.97]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-09T03:29:02.106Z*
