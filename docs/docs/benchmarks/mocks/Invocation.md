---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 274.12 ns | 74.72 ns | 4.095 ns | 128 B |
| Imposter | 294.47 ns | 53.69 ns | 2.943 ns | 168 B |
| Mockolate | 103.93 ns | 29.40 ns | 1.611 ns | 84 B |
| Moq | 808.49 ns | 131.81 ns | 7.225 ns | 376 B |
| NSubstitute | 721.88 ns | 409.11 ns | 22.425 ns | 304 B |
| FakeItEasy | 1,766.24 ns | 676.67 ns | 37.091 ns | 944 B |

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
  bar [274.12, 294.47, 103.93, 808.49, 721.88, 1766.24]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.25 ns | 72.02 ns | 3.948 ns | 96 B |
| Imposter | 300.99 ns | 86.78 ns | 4.757 ns | 168 B |
| Mockolate | 93.11 ns | 55.11 ns | 3.021 ns | 60 B |
| Moq | 540.08 ns | 143.43 ns | 7.862 ns | 296 B |
| NSubstitute | 613.95 ns | 199.71 ns | 10.947 ns | 272 B |
| FakeItEasy | 1,624.81 ns | 264.07 ns | 14.475 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1950
  bar [165.25, 300.99, 93.11, 540.08, 613.95, 1624.81]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,235.53 ns | 12,804.65 ns | 701.866 ns | 12736 B |
| Imposter | 28,992.09 ns | 12,746.35 ns | 698.670 ns | 16800 B |
| Mockolate | 10,404.25 ns | 2,075.34 ns | 113.757 ns | 8400 B |
| Moq | 80,259.51 ns | 6,513.30 ns | 357.016 ns | 37600 B |
| NSubstitute | 69,990.04 ns | 11,556.93 ns | 633.474 ns | 30848 B |
| FakeItEasy | 175,327.82 ns | 80,333.70 ns | 4,403.361 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 210394
  bar [27235.53, 28992.09, 10404.25, 80259.51, 69990.04, 175327.82]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-04T03:21:55.003Z*
