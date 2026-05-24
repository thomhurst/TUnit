---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 733.68 ns | 13.840 ns | 12.946 ns | 2968 B |
| Imposter | 693.60 ns | 9.312 ns | 8.710 ns | 4688 B |
| Mockolate | 409.47 ns | 5.374 ns | 5.027 ns | 2240 B |
| Moq | 346,315.18 ns | 1,744.817 ns | 1,457.001 ns | 24325 B |
| NSubstitute | 6,314.81 ns | 124.442 ns | 116.403 ns | 10064 B |
| FakeItEasy | 7,314.49 ns | 126.101 ns | 111.785 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 415579
  bar [733.68, 693.6, 409.47, 346315.18, 6314.81, 7314.49]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 48.95 ns | 0.713 ns | 0.667 ns | 304 B |
| Imposter | 333.70 ns | 6.365 ns | 6.537 ns | 2400 B |
| Mockolate | 255.13 ns | 5.104 ns | 7.155 ns | 1240 B |
| Moq | 89,610.75 ns | 618.847 ns | 548.592 ns | 7030 B |
| NSubstitute | 3,618.16 ns | 47.208 ns | 44.158 ns | 7088 B |
| FakeItEasy | 3,589.47 ns | 71.324 ns | 84.906 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107533
  bar [48.95, 333.7, 255.13, 89610.75, 3618.16, 3589.47]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,284.07 ns | 25.338 ns | 26.021 ns | 4384 B |
| Imposter | 1,765.97 ns | 35.057 ns | 36.001 ns | 11192 B |
| Mockolate | 1,169.10 ns | 15.594 ns | 14.587 ns | 5376 B |
| Moq | 477,079.81 ns | 2,965.717 ns | 2,476.508 ns | 34699 B |
| NSubstitute | 11,914.00 ns | 155.887 ns | 145.816 ns | 16929 B |
| FakeItEasy | 13,949.65 ns | 231.537 ns | 205.251 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 572496
  bar [1284.07, 1765.97, 1169.1, 477079.81, 11914, 13949.65]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-24T03:32:03.972Z*
