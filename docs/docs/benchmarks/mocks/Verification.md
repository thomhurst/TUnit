---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 805.88 ns | 13.506 ns | 12.634 ns | 3008 B |
| Imposter | 764.85 ns | 14.802 ns | 21.228 ns | 4688 B |
| Mockolate | 405.88 ns | 5.188 ns | 4.853 ns | 2128 B |
| Moq | 248,889.64 ns | 3,273.883 ns | 2,902.212 ns | 24324 B |
| NSubstitute | 6,641.62 ns | 126.665 ns | 160.191 ns | 10064 B |
| FakeItEasy | 6,826.65 ns | 134.862 ns | 189.058 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 298668
  bar [805.88, 764.85, 405.88, 248889.64, 6641.62, 6826.65]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 57.39 ns | 0.762 ns | 0.713 ns | 320 B |
| Imposter | 351.91 ns | 6.950 ns | 9.277 ns | 2400 B |
| Mockolate | 244.27 ns | 2.518 ns | 2.103 ns | 1144 B |
| Moq | 64,360.35 ns | 619.472 ns | 483.643 ns | 7037 B |
| NSubstitute | 3,690.99 ns | 73.790 ns | 82.017 ns | 7088 B |
| FakeItEasy | 3,403.23 ns | 58.478 ns | 54.701 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 77233
  bar [57.39, 351.91, 244.27, 64360.35, 3690.99, 3403.23]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,337.05 ns | 24.017 ns | 22.465 ns | 4472 B |
| Imposter | 1,822.91 ns | 35.972 ns | 80.457 ns | 11192 B |
| Mockolate | 1,162.26 ns | 23.046 ns | 40.362 ns | 5240 B |
| Moq | 345,620.84 ns | 1,077.065 ns | 840.901 ns | 34699 B |
| NSubstitute | 11,711.81 ns | 161.307 ns | 150.887 ns | 16762 B |
| FakeItEasy | 12,091.56 ns | 240.054 ns | 266.819 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 414746
  bar [1337.05, 1822.91, 1162.26, 345620.84, 11711.81, 12091.56]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-27T04:05:27.840Z*
