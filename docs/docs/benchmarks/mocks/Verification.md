---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 688.72 ns | 2.025 ns | 1.795 ns | 3008 B |
| Imposter | 675.27 ns | 4.531 ns | 4.016 ns | 4688 B |
| Mockolate | 440.48 ns | 1.777 ns | 1.484 ns | 2128 B |
| Moq | 350,646.81 ns | 805.187 ns | 672.367 ns | 24325 B |
| NSubstitute | 6,898.23 ns | 20.072 ns | 15.671 ns | 10176 B |
| FakeItEasy | 7,431.86 ns | 23.269 ns | 19.431 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 420777
  bar [688.72, 675.27, 440.48, 350646.81, 6898.23, 7431.86]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 50.51 ns | 0.704 ns | 0.659 ns | 320 B |
| Imposter | 310.62 ns | 2.167 ns | 2.027 ns | 2400 B |
| Mockolate | 216.70 ns | 0.555 ns | 0.520 ns | 1144 B |
| Moq | 89,735.83 ns | 243.859 ns | 190.389 ns | 6918 B |
| NSubstitute | 3,891.54 ns | 14.568 ns | 12.914 ns | 7088 B |
| FakeItEasy | 3,647.10 ns | 12.642 ns | 11.826 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107683
  bar [50.51, 310.62, 216.7, 89735.83, 3891.54, 3647.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,289.06 ns | 4.317 ns | 3.605 ns | 4472 B |
| Imposter | 1,683.02 ns | 5.554 ns | 5.195 ns | 11192 B |
| Mockolate | 1,046.61 ns | 3.099 ns | 2.747 ns | 5240 B |
| Moq | 478,707.60 ns | 2,483.719 ns | 2,074.017 ns | 34699 B |
| NSubstitute | 12,293.73 ns | 129.821 ns | 121.435 ns | 16929 B |
| FakeItEasy | 12,915.06 ns | 158.250 ns | 140.285 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 574450
  bar [1289.06, 1683.02, 1046.61, 478707.6, 12293.73, 12915.06]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-14T02:37:23.172Z*
