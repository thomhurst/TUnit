---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 689.28 ns | 2.495 ns | 2.083 ns | 3008 B |
| Imposter | 681.81 ns | 2.881 ns | 2.406 ns | 4688 B |
| Mockolate | 407.72 ns | 2.716 ns | 2.407 ns | 2128 B |
| Moq | 345,204.26 ns | 1,900.171 ns | 1,586.729 ns | 24325 B |
| NSubstitute | 6,325.54 ns | 81.916 ns | 72.616 ns | 10064 B |
| FakeItEasy | 7,525.40 ns | 128.378 ns | 113.804 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 414246
  bar [689.28, 681.81, 407.72, 345204.26, 6325.54, 7525.4]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.82 ns | 1.100 ns | 1.867 ns | 320 B |
| Imposter | 319.92 ns | 6.436 ns | 10.209 ns | 2400 B |
| Mockolate | 243.04 ns | 4.749 ns | 5.654 ns | 1144 B |
| Moq | 89,934.97 ns | 805.777 ns | 753.724 ns | 6918 B |
| NSubstitute | 3,657.29 ns | 70.798 ns | 75.753 ns | 7088 B |
| FakeItEasy | 3,783.16 ns | 73.786 ns | 95.942 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107922
  bar [54.82, 319.92, 243.04, 89934.97, 3657.29, 3783.16]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,217.75 ns | 24.149 ns | 42.925 ns | 4472 B |
| Imposter | 1,856.88 ns | 35.844 ns | 35.204 ns | 11192 B |
| Mockolate | 1,106.03 ns | 19.644 ns | 17.414 ns | 5240 B |
| Moq | 483,417.42 ns | 3,419.433 ns | 3,198.540 ns | 34699 B |
| NSubstitute | 11,742.67 ns | 113.008 ns | 100.178 ns | 16762 B |
| FakeItEasy | 14,338.99 ns | 158.480 ns | 140.489 ns | 19345 B |

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
  y-axis "Time (ns)" 0 --> 580101
  bar [1217.75, 1856.88, 1106.03, 483417.42, 11742.67, 14338.99]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-20T03:22:58.159Z*
