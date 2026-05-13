---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 670.78 ns | 13.384 ns | 25.138 ns | 2864 B |
| Imposter | 660.06 ns | 4.475 ns | 4.186 ns | 4688 B |
| Mockolate | 391.26 ns | 3.383 ns | 2.641 ns | 2240 B |
| Moq | 340,931.18 ns | 3,294.622 ns | 2,920.597 ns | 24325 B |
| NSubstitute | 6,154.22 ns | 20.388 ns | 18.073 ns | 10176 B |
| FakeItEasy | 7,082.79 ns | 16.599 ns | 14.715 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 409118
  bar [670.78, 660.06, 391.26, 340931.18, 6154.22, 7082.79]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 46.52 ns | 0.459 ns | 0.430 ns | 304 B |
| Imposter | 302.21 ns | 2.174 ns | 1.927 ns | 2400 B |
| Mockolate | 227.28 ns | 0.723 ns | 0.604 ns | 1240 B |
| Moq | 87,296.53 ns | 347.520 ns | 325.071 ns | 6918 B |
| NSubstitute | 3,563.57 ns | 7.628 ns | 6.369 ns | 7088 B |
| FakeItEasy | 3,607.23 ns | 16.700 ns | 13.945 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 104756
  bar [46.52, 302.21, 227.28, 87296.53, 3563.57, 3607.23]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,096.19 ns | 1.343 ns | 1.190 ns | 4176 B |
| Imposter | 1,692.78 ns | 2.185 ns | 1.825 ns | 11192 B |
| Mockolate | 1,123.05 ns | 7.598 ns | 6.735 ns | 5376 B |
| Moq | 469,959.98 ns | 1,607.548 ns | 1,425.049 ns | 34699 B |
| NSubstitute | 10,952.86 ns | 42.127 ns | 35.178 ns | 16762 B |
| FakeItEasy | 13,670.33 ns | 111.301 ns | 92.941 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 563952
  bar [1096.19, 1692.78, 1123.05, 469959.98, 10952.86, 13670.33]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-13T03:26:48.570Z*
