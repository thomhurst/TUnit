---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 264.1 ns | 89.12 ns | 4.88 ns | 120 B |
| Imposter | 303.8 ns | 62.21 ns | 3.41 ns | 168 B |
| Mockolate | 125.6 ns | 59.23 ns | 3.25 ns | 84 B |
| Moq | 863.7 ns | 261.60 ns | 14.34 ns | 376 B |
| NSubstitute | 728.7 ns | 261.06 ns | 14.31 ns | 304 B |
| FakeItEasy | 1,830.8 ns | 918.34 ns | 50.34 ns | 944 B |

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
  title "Invocation Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 2197
  bar [264.1, 303.8, 125.6, 863.7, 728.7, 1830.8]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 153.1 ns | 74.06 ns | 4.06 ns | 88 B |
| Imposter | 297.2 ns | 60.74 ns | 3.33 ns | 168 B |
| Mockolate | 100.4 ns | 16.53 ns | 0.91 ns | 60 B |
| Moq | 549.4 ns | 81.25 ns | 4.45 ns | 296 B |
| NSubstitute | 626.9 ns | 190.77 ns | 10.46 ns | 272 B |
| FakeItEasy | 1,632.4 ns | 313.40 ns | 17.18 ns | 776 B |

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
  title "Invocation (String) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 1959
  bar [153.1, 297.2, 100.4, 549.4, 626.9, 1632.4]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,008.5 ns | 11,394.55 ns | 624.57 ns | 11936 B |
| Imposter | 29,371.5 ns | 5,520.22 ns | 302.58 ns | 16800 B |
| Mockolate | 11,587.0 ns | 10,066.29 ns | 551.77 ns | 8400 B |
| Moq | 82,933.5 ns | 19,464.28 ns | 1,066.90 ns | 37600 B |
| NSubstitute | 71,795.2 ns | 15,344.93 ns | 841.11 ns | 30848 B |
| FakeItEasy | 184,815.4 ns | 99,984.84 ns | 5,480.51 ns | 94400 B |

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
  title "Invocation (100 calls) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 221779
  bar [26008.5, 29371.5, 11587, 82933.5, 71795.2, 184815.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-04T03:27:14.154Z*
