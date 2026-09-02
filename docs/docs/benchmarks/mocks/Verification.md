---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 702.47 ns | 3.495 ns | 2.918 ns | 3008 B |
| Imposter | 688.05 ns | 4.026 ns | 3.766 ns | 4688 B |
| Mockolate | 406.50 ns | 1.384 ns | 1.227 ns | 2128 B |
| Moq | 347,190.73 ns | 1,895.850 ns | 1,680.621 ns | 24325 B |
| NSubstitute | 6,816.33 ns | 64.578 ns | 60.407 ns | 10064 B |
| FakeItEasy | 7,518.42 ns | 124.317 ns | 110.204 ns | 10724 B |

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
  y-axis "Time (ns)" 0 --> 416629
  bar [702.47, 688.05, 406.5, 347190.73, 6816.33, 7518.42]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 50.43 ns | 0.108 ns | 0.096 ns | 320 B |
| Imposter | 323.83 ns | 1.545 ns | 1.369 ns | 2400 B |
| Mockolate | 224.77 ns | 1.296 ns | 1.149 ns | 1144 B |
| Moq | 90,334.21 ns | 293.895 ns | 245.416 ns | 7030 B |
| NSubstitute | 3,811.39 ns | 53.261 ns | 49.820 ns | 7088 B |
| FakeItEasy | 3,734.17 ns | 22.847 ns | 20.253 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 108402
  bar [50.43, 323.83, 224.77, 90334.21, 3811.39, 3734.17]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,227.53 ns | 7.414 ns | 6.573 ns | 4472 B |
| Imposter | 1,709.39 ns | 3.703 ns | 3.283 ns | 11192 B |
| Mockolate | 1,096.44 ns | 15.810 ns | 14.789 ns | 5240 B |
| Moq | 480,236.40 ns | 2,351.721 ns | 2,199.801 ns | 34699 B |
| NSubstitute | 12,563.78 ns | 58.588 ns | 51.937 ns | 16763 B |
| FakeItEasy | 13,513.18 ns | 118.415 ns | 110.766 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 576284
  bar [1227.53, 1709.39, 1096.44, 480236.4, 12563.78, 13513.18]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-02T02:49:53.672Z*
