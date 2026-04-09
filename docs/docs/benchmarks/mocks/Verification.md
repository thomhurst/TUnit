---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 746.63 ns | 8.631 ns | 8.073 ns | 3080 B |
| Imposter | 703.66 ns | 14.085 ns | 19.280 ns | 4688 B |
| Mockolate | 920.04 ns | 8.875 ns | 7.867 ns | 3152 B |
| Moq | 337,002.91 ns | 1,828.250 ns | 1,710.146 ns | 24325 B |
| NSubstitute | 6,176.36 ns | 28.778 ns | 26.919 ns | 10064 B |
| FakeItEasy | 7,196.42 ns | 28.265 ns | 25.056 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 404404
  bar [746.63, 703.66, 920.04, 337002.91, 6176.36, 7196.42]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.93 ns | 0.647 ns | 0.606 ns | 328 B |
| Imposter | 313.05 ns | 2.122 ns | 1.881 ns | 2400 B |
| Mockolate | 213.06 ns | 1.064 ns | 0.943 ns | 952 B |
| Moq | 86,942.44 ns | 348.243 ns | 325.746 ns | 6918 B |
| NSubstitute | 3,676.85 ns | 13.320 ns | 11.123 ns | 7088 B |
| FakeItEasy | 3,563.31 ns | 55.100 ns | 48.844 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 104331
  bar [60.93, 313.05, 213.06, 86942.44, 3676.85, 3563.31]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,266.34 ns | 11.516 ns | 10.772 ns | 4608 B |
| Imposter | 1,699.14 ns | 12.153 ns | 11.368 ns | 11192 B |
| Mockolate | 1,745.51 ns | 10.336 ns | 9.668 ns | 5496 B |
| Moq | 474,966.08 ns | 2,439.490 ns | 2,162.545 ns | 34779 B |
| NSubstitute | 11,332.58 ns | 152.948 ns | 143.067 ns | 16929 B |
| FakeItEasy | 12,907.29 ns | 93.733 ns | 78.271 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 569960
  bar [1266.34, 1699.14, 1745.51, 474966.08, 11332.58, 12907.29]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-09T03:21:47.332Z*
