---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 264.9 ns | 87.77 ns | 4.81 ns | 120 B |
| Imposter | 309.8 ns | 30.66 ns | 1.68 ns | 168 B |
| Mockolate | 131.1 ns | 55.85 ns | 3.06 ns | 84 B |
| Moq | 874.3 ns | 54.17 ns | 2.97 ns | 376 B |
| NSubstitute | 763.9 ns | 173.27 ns | 9.50 ns | 304 B |
| FakeItEasy | 1,939.3 ns | 594.52 ns | 32.59 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2328
  bar [264.9, 309.8, 131.1, 874.3, 763.9, 1939.3]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 154.9 ns | 66.05 ns | 3.62 ns | 88 B |
| Imposter | 308.2 ns | 56.18 ns | 3.08 ns | 168 B |
| Mockolate | 110.4 ns | 42.84 ns | 2.35 ns | 60 B |
| Moq | 529.3 ns | 40.09 ns | 2.20 ns | 296 B |
| NSubstitute | 600.2 ns | 243.05 ns | 13.32 ns | 272 B |
| FakeItEasy | 1,632.0 ns | 624.39 ns | 34.22 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1959
  bar [154.9, 308.2, 110.4, 529.3, 600.2, 1632]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,592.2 ns | 9,039.60 ns | 495.49 ns | 11936 B |
| Imposter | 29,983.0 ns | 6,857.86 ns | 375.90 ns | 16800 B |
| Mockolate | 11,084.2 ns | 2,529.37 ns | 138.64 ns | 8400 B |
| Moq | 86,691.8 ns | 9,472.43 ns | 519.22 ns | 37600 B |
| NSubstitute | 81,197.3 ns | 38,403.24 ns | 2,105.01 ns | 36448 B |
| FakeItEasy | 172,276.3 ns | 115,984.01 ns | 6,357.47 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 206732
  bar [26592.2, 29983, 11084.2, 86691.8, 81197.3, 172276.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-19T03:26:57.825Z*
