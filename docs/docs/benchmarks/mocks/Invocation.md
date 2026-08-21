---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 267.12 ns | 83.38 ns | 4.570 ns | 128 B |
| Imposter | 299.06 ns | 66.41 ns | 3.640 ns | 168 B |
| Mockolate | 108.81 ns | 43.87 ns | 2.405 ns | 84 B |
| Moq | 772.43 ns | 40.84 ns | 2.239 ns | 376 B |
| NSubstitute | 716.91 ns | 268.42 ns | 14.713 ns | 304 B |
| FakeItEasy | 1,722.73 ns | 191.77 ns | 10.512 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2068
  bar [267.12, 299.06, 108.81, 772.43, 716.91, 1722.73]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 173.02 ns | 62.46 ns | 3.423 ns | 96 B |
| Imposter | 302.09 ns | 71.47 ns | 3.917 ns | 168 B |
| Mockolate | 96.40 ns | 10.24 ns | 0.562 ns | 60 B |
| Moq | 529.75 ns | 119.79 ns | 6.566 ns | 296 B |
| NSubstitute | 590.63 ns | 177.13 ns | 9.709 ns | 272 B |
| FakeItEasy | 1,547.39 ns | 139.64 ns | 7.654 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1857
  bar [173.02, 302.09, 96.4, 529.75, 590.63, 1547.39]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,926.50 ns | 8,667.69 ns | 475.105 ns | 12736 B |
| Imposter | 29,441.08 ns | 9,531.61 ns | 522.460 ns | 16800 B |
| Mockolate | 10,511.18 ns | 2,763.19 ns | 151.460 ns | 8400 B |
| Moq | 81,016.02 ns | 88,793.47 ns | 4,867.070 ns | 37600 B |
| NSubstitute | 72,247.48 ns | 25,418.10 ns | 1,393.252 ns | 30848 B |
| FakeItEasy | 173,821.18 ns | 61,053.71 ns | 3,346.560 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 208586
  bar [26926.5, 29441.08, 10511.18, 81016.02, 72247.48, 173821.18]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-21T02:46:27.792Z*
