---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 720.51 ns | 14.428 ns | 27.797 ns | 3008 B |
| Imposter | 722.04 ns | 8.092 ns | 7.570 ns | 4688 B |
| Mockolate | 432.71 ns | 8.676 ns | 11.282 ns | 2128 B |
| Moq | 355,515.56 ns | 2,441.062 ns | 2,283.371 ns | 24325 B |
| NSubstitute | 6,621.68 ns | 53.129 ns | 44.365 ns | 10064 B |
| FakeItEasy | 8,049.05 ns | 64.856 ns | 60.666 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 426619
  bar [720.51, 722.04, 432.71, 355515.56, 6621.68, 8049.05]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 59.66 ns | 1.160 ns | 1.139 ns | 320 B |
| Imposter | 360.64 ns | 5.020 ns | 4.450 ns | 2400 B |
| Mockolate | 269.27 ns | 3.411 ns | 3.191 ns | 1144 B |
| Moq | 90,489.67 ns | 501.143 ns | 468.770 ns | 6918 B |
| NSubstitute | 3,914.98 ns | 19.826 ns | 18.546 ns | 7088 B |
| FakeItEasy | 4,277.28 ns | 37.638 ns | 33.365 ns | 5324 B |

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
  y-axis "Time (ns)" 0 --> 108588
  bar [59.66, 360.64, 269.27, 90489.67, 3914.98, 4277.28]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,398.25 ns | 17.441 ns | 16.314 ns | 4472 B |
| Imposter | 2,037.60 ns | 28.830 ns | 26.968 ns | 11192 B |
| Mockolate | 1,220.38 ns | 24.384 ns | 23.948 ns | 5240 B |
| Moq | 481,390.63 ns | 2,372.165 ns | 2,102.863 ns | 34699 B |
| NSubstitute | 11,874.03 ns | 229.726 ns | 225.621 ns | 16763 B |
| FakeItEasy | 14,800.58 ns | 262.938 ns | 281.341 ns | 19538 B |

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
  y-axis "Time (ns)" 0 --> 577669
  bar [1398.25, 2037.6, 1220.38, 481390.63, 11874.03, 14800.58]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-23T03:26:30.646Z*
