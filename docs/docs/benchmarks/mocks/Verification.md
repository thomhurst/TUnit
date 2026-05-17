---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 640.40 ns | 8.433 ns | 7.888 ns | 2864 B |
| Imposter | 703.03 ns | 8.966 ns | 7.487 ns | 4688 B |
| Mockolate | 400.64 ns | 3.439 ns | 3.217 ns | 2240 B |
| Moq | 342,915.55 ns | 2,260.478 ns | 2,114.453 ns | 24325 B |
| NSubstitute | 6,221.95 ns | 52.695 ns | 44.003 ns | 10064 B |
| FakeItEasy | 7,464.23 ns | 72.881 ns | 68.173 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 411499
  bar [640.4, 703.03, 400.64, 342915.55, 6221.95, 7464.23]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 47.81 ns | 0.508 ns | 0.475 ns | 304 B |
| Imposter | 318.14 ns | 4.464 ns | 3.728 ns | 2400 B |
| Mockolate | 235.19 ns | 1.780 ns | 1.665 ns | 1240 B |
| Moq | 87,046.63 ns | 335.795 ns | 297.674 ns | 6918 B |
| NSubstitute | 3,500.66 ns | 20.931 ns | 18.555 ns | 7088 B |
| FakeItEasy | 3,473.93 ns | 38.775 ns | 34.373 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 104456
  bar [47.81, 318.14, 235.19, 87046.63, 3500.66, 3473.93]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,107.19 ns | 10.732 ns | 10.038 ns | 4176 B |
| Imposter | 1,727.42 ns | 15.739 ns | 13.143 ns | 11192 B |
| Mockolate | 1,104.12 ns | 5.005 ns | 4.437 ns | 5376 B |
| Moq | 466,678.09 ns | 3,637.016 ns | 3,402.067 ns | 34699 B |
| NSubstitute | 11,481.11 ns | 56.990 ns | 53.309 ns | 16763 B |
| FakeItEasy | 13,513.55 ns | 200.466 ns | 167.399 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 560014
  bar [1107.19, 1727.42, 1104.12, 466678.09, 11481.11, 13513.55]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-17T03:31:33.295Z*
