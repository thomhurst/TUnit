---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 706.76 ns | 6.314 ns | 5.906 ns | 3008 B |
| Imposter | 833.52 ns | 5.511 ns | 4.885 ns | 4688 B |
| Mockolate | 411.06 ns | 4.345 ns | 4.064 ns | 2128 B |
| Moq | 341,643.63 ns | 2,083.005 ns | 1,948.444 ns | 24325 B |
| NSubstitute | 6,228.87 ns | 40.997 ns | 36.343 ns | 10064 B |
| FakeItEasy | 7,433.54 ns | 60.783 ns | 56.857 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 409973
  bar [706.76, 833.52, 411.06, 341643.63, 6228.87, 7433.54]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.69 ns | 0.314 ns | 0.294 ns | 320 B |
| Imposter | 322.19 ns | 3.978 ns | 3.526 ns | 2400 B |
| Mockolate | 233.95 ns | 1.761 ns | 1.647 ns | 1144 B |
| Moq | 87,729.42 ns | 240.241 ns | 200.612 ns | 6918 B |
| NSubstitute | 3,517.28 ns | 19.945 ns | 17.681 ns | 7088 B |
| FakeItEasy | 3,719.39 ns | 34.515 ns | 32.286 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 105276
  bar [52.69, 322.19, 233.95, 87729.42, 3517.28, 3719.39]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,328.36 ns | 10.332 ns | 9.664 ns | 4472 B |
| Imposter | 1,918.65 ns | 19.593 ns | 17.368 ns | 11192 B |
| Mockolate | 1,158.59 ns | 9.879 ns | 8.757 ns | 5240 B |
| Moq | 472,999.18 ns | 3,567.529 ns | 3,337.069 ns | 34699 B |
| NSubstitute | 11,407.07 ns | 117.094 ns | 103.801 ns | 16763 B |
| FakeItEasy | 13,469.51 ns | 170.053 ns | 159.068 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 567600
  bar [1328.36, 1918.65, 1158.59, 472999.18, 11407.07, 13469.51]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-11T03:21:26.661Z*
