---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 697.71 ns | 5.722 ns | 5.072 ns | 3008 B |
| Imposter | 671.73 ns | 6.382 ns | 5.970 ns | 4688 B |
| Mockolate | 401.61 ns | 3.838 ns | 3.590 ns | 2128 B |
| Moq | 344,736.57 ns | 3,057.561 ns | 2,860.044 ns | 24325 B |
| NSubstitute | 6,315.60 ns | 44.572 ns | 39.512 ns | 10064 B |
| FakeItEasy | 7,604.91 ns | 82.011 ns | 76.713 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 413684
  bar [697.71, 671.73, 401.61, 344736.57, 6315.6, 7604.91]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.09 ns | 0.418 ns | 0.391 ns | 320 B |
| Imposter | 319.97 ns | 2.992 ns | 2.799 ns | 2400 B |
| Mockolate | 231.98 ns | 1.797 ns | 1.593 ns | 1144 B |
| Moq | 88,727.49 ns | 537.612 ns | 448.931 ns | 6918 B |
| NSubstitute | 3,574.40 ns | 24.975 ns | 23.362 ns | 7088 B |
| FakeItEasy | 3,595.24 ns | 24.341 ns | 22.768 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 106473
  bar [52.09, 319.97, 231.98, 88727.49, 3574.4, 3595.24]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,207.34 ns | 5.019 ns | 4.695 ns | 4472 B |
| Imposter | 1,721.34 ns | 17.309 ns | 16.191 ns | 11192 B |
| Mockolate | 1,070.27 ns | 4.221 ns | 3.295 ns | 5240 B |
| Moq | 474,679.51 ns | 2,666.443 ns | 2,363.732 ns | 34699 B |
| NSubstitute | 11,404.95 ns | 59.300 ns | 55.469 ns | 16762 B |
| FakeItEasy | 13,949.69 ns | 265.855 ns | 273.014 ns | 19313 B |

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
  y-axis "Time (ns)" 0 --> 569616
  bar [1207.34, 1721.34, 1070.27, 474679.51, 11404.95, 13949.69]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-05T03:32:29.901Z*
