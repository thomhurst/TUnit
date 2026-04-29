---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 673.83 ns | 13.202 ns | 15.716 ns | 2864 B |
| Imposter | 717.63 ns | 14.091 ns | 13.181 ns | 4688 B |
| Mockolate | 940.07 ns | 7.393 ns | 6.916 ns | 3152 B |
| Moq | 343,578.97 ns | 2,304.589 ns | 2,042.958 ns | 24325 B |
| NSubstitute | 6,301.30 ns | 97.777 ns | 91.460 ns | 10064 B |
| FakeItEasy | 8,096.34 ns | 63.176 ns | 52.755 ns | 10964 B |

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
  y-axis "Time (ns)" 0 --> 412295
  bar [673.83, 717.63, 940.07, 343578.97, 6301.3, 8096.34]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.80 ns | 1.077 ns | 1.475 ns | 304 B |
| Imposter | 349.28 ns | 3.190 ns | 2.664 ns | 2400 B |
| Mockolate | 218.69 ns | 2.554 ns | 2.389 ns | 952 B |
| Moq | 89,567.20 ns | 531.002 ns | 496.699 ns | 6918 B |
| NSubstitute | 3,603.54 ns | 9.071 ns | 8.042 ns | 7088 B |
| FakeItEasy | 3,619.33 ns | 11.860 ns | 10.513 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107481
  bar [52.8, 349.28, 218.69, 89567.2, 3603.54, 3619.33]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,133.94 ns | 10.321 ns | 9.149 ns | 4176 B |
| Imposter | 1,888.49 ns | 29.616 ns | 26.254 ns | 11192 B |
| Mockolate | 1,846.36 ns | 17.469 ns | 16.341 ns | 5496 B |
| Moq | 471,586.64 ns | 1,560.490 ns | 1,459.684 ns | 34699 B |
| NSubstitute | 11,546.98 ns | 65.861 ns | 58.384 ns | 16762 B |
| FakeItEasy | 13,260.04 ns | 93.851 ns | 78.370 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 565904
  bar [1133.94, 1888.49, 1846.36, 471586.64, 11546.98, 13260.04]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-29T03:24:49.990Z*
