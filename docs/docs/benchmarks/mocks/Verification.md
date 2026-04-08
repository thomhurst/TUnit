---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 729.07 ns | 12.189 ns | 11.402 ns | 3080 B |
| Imposter | 701.94 ns | 10.373 ns | 8.662 ns | 4688 B |
| Mockolate | 921.45 ns | 17.590 ns | 17.275 ns | 3152 B |
| Moq | 343,571.12 ns | 2,384.353 ns | 2,230.325 ns | 24325 B |
| NSubstitute | 6,132.88 ns | 29.961 ns | 28.025 ns | 10064 B |
| FakeItEasy | 7,438.86 ns | 104.639 ns | 92.760 ns | 10724 B |

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
  y-axis "Time (ns)" 0 --> 412286
  bar [729.07, 701.94, 921.45, 343571.12, 6132.88, 7438.86]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.61 ns | 1.063 ns | 0.942 ns | 328 B |
| Imposter | 316.11 ns | 6.331 ns | 9.280 ns | 2400 B |
| Mockolate | 227.62 ns | 1.804 ns | 1.600 ns | 952 B |
| Moq | 86,340.24 ns | 279.275 ns | 261.234 ns | 6918 B |
| NSubstitute | 3,483.65 ns | 22.916 ns | 21.436 ns | 7088 B |
| FakeItEasy | 3,428.31 ns | 25.406 ns | 23.765 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 103609
  bar [60.61, 316.11, 227.62, 86340.24, 3483.65, 3428.31]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,404.99 ns | 27.112 ns | 35.254 ns | 4608 B |
| Imposter | 1,737.98 ns | 34.278 ns | 67.661 ns | 11192 B |
| Mockolate | 1,843.17 ns | 16.745 ns | 14.844 ns | 5496 B |
| Moq | 470,425.90 ns | 2,068.305 ns | 1,727.128 ns | 34699 B |
| NSubstitute | 11,217.46 ns | 126.871 ns | 118.675 ns | 16763 B |
| FakeItEasy | 13,106.85 ns | 243.766 ns | 203.556 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 564512
  bar [1404.99, 1737.98, 1843.17, 470425.9, 11217.46, 13106.85]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-08T03:21:46.624Z*
