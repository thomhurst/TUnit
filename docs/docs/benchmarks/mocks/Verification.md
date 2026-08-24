---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 757.38 ns | 13.431 ns | 12.563 ns | 3008 B |
| Imposter | 730.08 ns | 12.371 ns | 10.966 ns | 4688 B |
| Mockolate | 404.55 ns | 1.474 ns | 1.231 ns | 2128 B |
| Moq | 347,942.95 ns | 2,038.898 ns | 1,807.430 ns | 24325 B |
| NSubstitute | 7,076.23 ns | 77.488 ns | 72.482 ns | 10064 B |
| FakeItEasy | 7,606.75 ns | 33.969 ns | 30.113 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 417532
  bar [757.38, 730.08, 404.55, 347942.95, 7076.23, 7606.75]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.26 ns | 0.591 ns | 0.524 ns | 320 B |
| Imposter | 337.80 ns | 2.597 ns | 2.429 ns | 2400 B |
| Mockolate | 244.92 ns | 4.931 ns | 4.843 ns | 1144 B |
| Moq | 89,034.99 ns | 249.542 ns | 208.379 ns | 6918 B |
| NSubstitute | 3,984.18 ns | 48.944 ns | 45.782 ns | 7088 B |
| FakeItEasy | 3,722.70 ns | 71.592 ns | 70.313 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106842
  bar [53.26, 337.8, 244.92, 89034.99, 3984.18, 3722.7]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,280.87 ns | 10.870 ns | 10.168 ns | 4472 B |
| Imposter | 1,849.21 ns | 27.266 ns | 25.505 ns | 11192 B |
| Mockolate | 1,129.89 ns | 13.561 ns | 12.685 ns | 5240 B |
| Moq | 484,328.25 ns | 3,701.225 ns | 3,462.128 ns | 34922 B |
| NSubstitute | 13,146.88 ns | 106.801 ns | 99.902 ns | 16763 B |
| FakeItEasy | 14,006.14 ns | 218.499 ns | 182.456 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 581194
  bar [1280.87, 1849.21, 1129.89, 484328.25, 13146.88, 14006.14]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-24T02:46:06.016Z*
