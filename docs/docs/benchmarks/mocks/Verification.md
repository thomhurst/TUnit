---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 791.81 ns | 3.722 ns | 3.108 ns | 3008 B |
| Imposter | 822.29 ns | 3.858 ns | 3.609 ns | 4688 B |
| Mockolate | 463.91 ns | 2.275 ns | 2.128 ns | 2128 B |
| Moq | 359,901.64 ns | 2,253.535 ns | 1,881.804 ns | 24325 B |
| NSubstitute | 6,760.93 ns | 19.511 ns | 17.296 ns | 10064 B |
| FakeItEasy | 7,902.17 ns | 30.847 ns | 28.854 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 431882
  bar [791.81, 822.29, 463.91, 359901.64, 6760.93, 7902.17]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.06 ns | 0.717 ns | 0.636 ns | 320 B |
| Imposter | 386.74 ns | 2.179 ns | 1.820 ns | 2400 B |
| Mockolate | 271.03 ns | 2.167 ns | 1.921 ns | 1144 B |
| Moq | 91,572.41 ns | 422.239 ns | 374.304 ns | 6918 B |
| NSubstitute | 3,934.84 ns | 14.863 ns | 12.411 ns | 7088 B |
| FakeItEasy | 4,051.93 ns | 76.489 ns | 71.548 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 109887
  bar [60.06, 386.74, 271.03, 91572.41, 3934.84, 4051.93]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,400.46 ns | 12.227 ns | 11.437 ns | 4472 B |
| Imposter | 2,061.75 ns | 19.275 ns | 17.087 ns | 11192 B |
| Mockolate | 1,281.35 ns | 22.858 ns | 21.381 ns | 5240 B |
| Moq | 493,310.44 ns | 4,752.532 ns | 4,445.522 ns | 34699 B |
| NSubstitute | 12,357.22 ns | 83.231 ns | 69.502 ns | 16763 B |
| FakeItEasy | 14,597.11 ns | 130.826 ns | 109.246 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 591973
  bar [1400.46, 2061.75, 1281.35, 493310.44, 12357.22, 14597.11]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-27T03:23:36.716Z*
