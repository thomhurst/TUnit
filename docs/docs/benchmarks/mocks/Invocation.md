---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 261.2 ns | 57.00 ns | 3.12 ns | 120 B |
| Imposter | 296.5 ns | 79.74 ns | 4.37 ns | 168 B |
| Mockolate | 106.1 ns | 17.88 ns | 0.98 ns | 84 B |
| Moq | 780.3 ns | 174.26 ns | 9.55 ns | 376 B |
| NSubstitute | 751.2 ns | 104.34 ns | 5.72 ns | 360 B |
| FakeItEasy | 1,746.5 ns | 293.26 ns | 16.07 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2096
  bar [261.2, 296.5, 106.1, 780.3, 751.2, 1746.5]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.4 ns | 83.95 ns | 4.60 ns | 88 B |
| Imposter | 297.8 ns | 80.18 ns | 4.39 ns | 168 B |
| Mockolate | 107.2 ns | 36.93 ns | 2.02 ns | 60 B |
| Moq | 529.4 ns | 23.26 ns | 1.28 ns | 296 B |
| NSubstitute | 594.5 ns | 241.41 ns | 13.23 ns | 272 B |
| FakeItEasy | 1,558.4 ns | 168.43 ns | 9.23 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1871
  bar [165.4, 297.8, 107.2, 529.4, 594.5, 1558.4]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,440.1 ns | 12,850.80 ns | 704.40 ns | 11936 B |
| Imposter | 29,534.5 ns | 7,083.40 ns | 388.26 ns | 16800 B |
| Mockolate | 10,847.1 ns | 2,485.30 ns | 136.23 ns | 8400 B |
| Moq | 79,574.6 ns | 26,406.68 ns | 1,447.44 ns | 37600 B |
| NSubstitute | 72,639.0 ns | 30,873.24 ns | 1,692.27 ns | 30848 B |
| FakeItEasy | 175,627.2 ns | 75,016.76 ns | 4,111.92 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 210753
  bar [26440.1, 29534.5, 10847.1, 79574.6, 72639, 175627.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-22T03:28:55.311Z*
