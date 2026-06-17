---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 810.19 ns | 8.457 ns | 7.911 ns | 3008 B |
| Imposter | 717.06 ns | 8.929 ns | 8.352 ns | 4688 B |
| Mockolate | 453.84 ns | 3.908 ns | 3.464 ns | 2240 B |
| Moq | 251,775.55 ns | 1,166.818 ns | 974.346 ns | 24306 B |
| NSubstitute | 6,194.00 ns | 33.137 ns | 30.996 ns | 10064 B |
| FakeItEasy | 7,176.65 ns | 57.675 ns | 51.127 ns | 10731 B |

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
  y-axis "Time (ns)" 0 --> 302131
  bar [810.19, 717.06, 453.84, 251775.55, 6194, 7176.65]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.49 ns | 0.534 ns | 0.500 ns | 320 B |
| Imposter | 325.05 ns | 2.492 ns | 2.209 ns | 2400 B |
| Mockolate | 253.72 ns | 3.304 ns | 3.091 ns | 1240 B |
| Moq | 65,773.42 ns | 662.859 ns | 620.039 ns | 7037 B |
| NSubstitute | 3,339.29 ns | 20.893 ns | 18.521 ns | 7088 B |
| FakeItEasy | 3,223.40 ns | 30.028 ns | 28.088 ns | 5217 B |

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
  y-axis "Time (ns)" 0 --> 78929
  bar [52.49, 325.05, 253.72, 65773.42, 3339.29, 3223.4]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,381.22 ns | 6.626 ns | 6.198 ns | 4472 B |
| Imposter | 1,744.80 ns | 28.617 ns | 26.769 ns | 11192 B |
| Mockolate | 1,188.13 ns | 13.426 ns | 11.901 ns | 5376 B |
| Moq | 350,594.98 ns | 3,040.700 ns | 2,844.273 ns | 35181 B |
| NSubstitute | 10,660.99 ns | 93.903 ns | 87.837 ns | 16762 B |
| FakeItEasy | 12,630.28 ns | 46.944 ns | 43.911 ns | 19239 B |

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
  y-axis "Time (ns)" 0 --> 420714
  bar [1381.22, 1744.8, 1188.13, 350594.98, 10660.99, 12630.28]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-17T03:28:53.706Z*
