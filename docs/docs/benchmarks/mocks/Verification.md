---
title: "Mock Benchmark: Verification"
description: "Mock verification workflows, including creation, setup and invocation where applicable — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Mock verification workflows, including creation, setup and invocation where applicable — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Mock verification workflows, including creation, setup and invocation where applicable:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 488.54 ns | 8.316 ns | 7.779 ns | 1512 B |
| Imposter | 291.85 ns | 5.719 ns | 5.350 ns | 1096 B |
| Mockolate | 213.71 ns | 1.777 ns | 1.575 ns | 712 B |
| Moq | 378,116.74 ns | 2,857.948 ns | 2,386.516 ns | 24300 B |
| NSubstitute | 7,042.47 ns | 69.052 ns | 64.592 ns | 10024 B |
| FakeItEasy | 7,301.30 ns | 19.053 ns | 14.875 ns | 10506 B |

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
  y-axis "Time (ns)" 0 --> 453741
  bar [488.54, 291.85, 213.71, 378116.74, 7042.47, 7301.3]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 63.48 ns | 1.206 ns | 1.389 ns | 360 B |
| Imposter | 149.67 ns | 2.330 ns | 2.180 ns | 616 B |
| Mockolate | 80.21 ns | 1.090 ns | 1.019 ns | 368 B |
| Moq | 97,479.97 ns | 650.658 ns | 608.626 ns | 6917 B |
| NSubstitute | 4,037.50 ns | 31.154 ns | 29.142 ns | 7056 B |
| FakeItEasy | 3,772.03 ns | 27.572 ns | 25.790 ns | 5105 B |

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
  y-axis "Time (ns)" 0 --> 116976
  bar [63.48, 149.67, 80.21, 97479.97, 4037.5, 3772.03]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 915.98 ns | 7.638 ns | 7.144 ns | 2528 B |
| Imposter | 692.12 ns | 11.989 ns | 11.214 ns | 2288 B |
| Mockolate | 471.85 ns | 1.964 ns | 1.837 ns | 1360 B |
| Moq | 518,426.45 ns | 3,780.528 ns | 3,351.339 ns | 35082 B |
| NSubstitute | 12,776.72 ns | 41.229 ns | 34.428 ns | 16633 B |
| FakeItEasy | 13,856.07 ns | 85.526 ns | 75.817 ns | 18703 B |

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
  y-axis "Time (ns)" 0 --> 622112
  bar [915.98, 692.12, 471.85, 518426.45, 12776.72, 13856.07]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for mock verification workflows, including creation, setup and invocation where applicable.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-26T02:21:09.958Z*
