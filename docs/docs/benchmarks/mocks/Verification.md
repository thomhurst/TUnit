---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 687.26 ns | 1.686 ns | 1.577 ns | 3008 B |
| Imposter | 667.16 ns | 4.854 ns | 4.541 ns | 4688 B |
| Mockolate | 400.01 ns | 2.143 ns | 1.673 ns | 2128 B |
| Moq | 339,630.15 ns | 1,888.161 ns | 1,766.187 ns | 24325 B |
| NSubstitute | 6,334.52 ns | 60.991 ns | 50.930 ns | 10176 B |
| FakeItEasy | 7,308.63 ns | 76.131 ns | 67.488 ns | 10724 B |

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
  y-axis "Time (ns)" 0 --> 407557
  bar [687.26, 667.16, 400.01, 339630.15, 6334.52, 7308.63]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 50.05 ns | 0.180 ns | 0.159 ns | 320 B |
| Imposter | 311.30 ns | 1.319 ns | 1.170 ns | 2400 B |
| Mockolate | 222.92 ns | 0.848 ns | 0.708 ns | 1144 B |
| Moq | 85,450.63 ns | 245.049 ns | 204.627 ns | 6918 B |
| NSubstitute | 3,613.08 ns | 12.276 ns | 10.251 ns | 7088 B |
| FakeItEasy | 3,589.99 ns | 21.357 ns | 19.978 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 102541
  bar [50.05, 311.3, 222.92, 85450.63, 3613.08, 3589.99]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,228.73 ns | 4.736 ns | 3.955 ns | 4472 B |
| Imposter | 1,673.95 ns | 17.647 ns | 16.507 ns | 11192 B |
| Mockolate | 1,061.56 ns | 5.991 ns | 5.604 ns | 5240 B |
| Moq | 457,746.83 ns | 2,582.141 ns | 2,415.336 ns | 34699 B |
| NSubstitute | 11,350.13 ns | 54.116 ns | 45.189 ns | 16763 B |
| FakeItEasy | 13,285.50 ns | 114.741 ns | 101.715 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 549297
  bar [1228.73, 1673.95, 1061.56, 457746.83, 11350.13, 13285.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-29T03:20:13.661Z*
