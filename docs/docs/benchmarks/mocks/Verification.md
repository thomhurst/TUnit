---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 667.60 ns | 7.860 ns | 7.353 ns | 2864 B |
| Imposter | 734.21 ns | 12.299 ns | 11.504 ns | 4688 B |
| Mockolate | 409.25 ns | 5.508 ns | 5.152 ns | 2240 B |
| Moq | 342,992.68 ns | 1,770.412 ns | 1,569.424 ns | 24325 B |
| NSubstitute | 6,305.33 ns | 52.295 ns | 48.917 ns | 10064 B |
| FakeItEasy | 7,567.78 ns | 78.235 ns | 73.181 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 411592
  bar [667.6, 734.21, 409.25, 342992.68, 6305.33, 7567.78]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 48.66 ns | 0.938 ns | 0.964 ns | 304 B |
| Imposter | 335.27 ns | 6.472 ns | 7.705 ns | 2400 B |
| Mockolate | 237.73 ns | 3.935 ns | 3.681 ns | 1240 B |
| Moq | 86,850.85 ns | 391.860 ns | 347.373 ns | 6918 B |
| NSubstitute | 3,533.56 ns | 33.560 ns | 31.392 ns | 7088 B |
| FakeItEasy | 3,472.08 ns | 45.221 ns | 37.761 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 104222
  bar [48.66, 335.27, 237.73, 86850.85, 3533.56, 3472.08]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,154.55 ns | 18.930 ns | 17.707 ns | 4176 B |
| Imposter | 1,726.96 ns | 32.893 ns | 30.768 ns | 11192 B |
| Mockolate | 1,162.15 ns | 20.243 ns | 18.935 ns | 5376 B |
| Moq | 478,139.85 ns | 2,791.730 ns | 2,331.221 ns | 34922 B |
| NSubstitute | 11,571.75 ns | 62.277 ns | 55.207 ns | 16763 B |
| FakeItEasy | 13,555.40 ns | 233.006 ns | 194.571 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 573768
  bar [1154.55, 1726.96, 1162.15, 478139.85, 11571.75, 13555.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-15T03:27:25.234Z*
