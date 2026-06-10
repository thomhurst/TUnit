---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 731.10 ns | 5.355 ns | 5.009 ns | 3000 B |
| Imposter | 787.30 ns | 15.514 ns | 17.866 ns | 4688 B |
| Mockolate | 468.03 ns | 8.804 ns | 9.421 ns | 2240 B |
| Moq | 355,422.97 ns | 3,033.269 ns | 2,688.914 ns | 24325 B |
| NSubstitute | 6,626.39 ns | 27.622 ns | 25.837 ns | 10064 B |
| FakeItEasy | 8,104.45 ns | 56.104 ns | 49.735 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 426508
  bar [731.1, 787.3, 468.03, 355422.97, 6626.39, 8104.45]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 59.22 ns | 1.211 ns | 2.502 ns | 312 B |
| Imposter | 385.28 ns | 7.672 ns | 15.144 ns | 2400 B |
| Mockolate | 277.83 ns | 3.857 ns | 3.608 ns | 1240 B |
| Moq | 90,031.45 ns | 986.633 ns | 922.897 ns | 6918 B |
| NSubstitute | 3,746.73 ns | 13.887 ns | 12.311 ns | 7088 B |
| FakeItEasy | 3,765.54 ns | 40.832 ns | 38.195 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 108038
  bar [59.22, 385.28, 277.83, 90031.45, 3746.73, 3765.54]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,428.06 ns | 27.946 ns | 36.337 ns | 4464 B |
| Imposter | 1,930.48 ns | 37.884 ns | 62.244 ns | 11192 B |
| Mockolate | 1,257.34 ns | 24.734 ns | 51.081 ns | 5376 B |
| Moq | 482,633.15 ns | 2,696.414 ns | 2,522.228 ns | 34699 B |
| NSubstitute | 11,866.06 ns | 129.624 ns | 121.251 ns | 16763 B |
| FakeItEasy | 14,668.11 ns | 209.845 ns | 196.289 ns | 19457 B |

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
  y-axis "Time (ns)" 0 --> 579160
  bar [1428.06, 1930.48, 1257.34, 482633.15, 11866.06, 14668.11]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-10T03:28:13.506Z*
