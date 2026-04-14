---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 740.47 ns | 14.398 ns | 18.722 ns | 3080 B |
| Imposter | 696.81 ns | 13.729 ns | 26.451 ns | 4688 B |
| Mockolate | 926.56 ns | 9.430 ns | 8.821 ns | 3152 B |
| Moq | 351,598.54 ns | 3,772.915 ns | 3,529.187 ns | 24325 B |
| NSubstitute | 6,038.71 ns | 63.003 ns | 52.611 ns | 10064 B |
| FakeItEasy | 7,366.95 ns | 97.965 ns | 86.844 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 421919
  bar [740.47, 696.81, 926.56, 351598.54, 6038.71, 7366.95]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.99 ns | 1.239 ns | 2.036 ns | 328 B |
| Imposter | 323.88 ns | 6.331 ns | 6.774 ns | 2400 B |
| Mockolate | 231.11 ns | 4.613 ns | 9.931 ns | 952 B |
| Moq | 89,588.18 ns | 492.269 ns | 436.383 ns | 6918 B |
| NSubstitute | 3,711.00 ns | 34.858 ns | 32.607 ns | 7088 B |
| FakeItEasy | 3,756.65 ns | 44.598 ns | 41.717 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107506
  bar [60.99, 323.88, 231.11, 89588.18, 3711, 3756.65]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,422.94 ns | 5.579 ns | 4.659 ns | 4608 B |
| Imposter | 1,965.26 ns | 23.385 ns | 21.874 ns | 11192 B |
| Mockolate | 1,971.31 ns | 11.917 ns | 10.564 ns | 5496 B |
| Moq | 489,349.29 ns | 2,436.977 ns | 2,160.316 ns | 34699 B |
| NSubstitute | 11,883.82 ns | 126.120 ns | 111.802 ns | 16763 B |
| FakeItEasy | 14,258.17 ns | 282.061 ns | 422.176 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 587220
  bar [1422.94, 1965.26, 1971.31, 489349.29, 11883.82, 14258.17]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-14T03:22:19.526Z*
