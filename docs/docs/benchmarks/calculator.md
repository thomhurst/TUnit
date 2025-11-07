---
title: Benchmark Calculator
description: Calculate potential time savings for your test suite when migrating to TUnit
sidebar_position: 2
---

# Benchmark Calculator

Calculate how much time you could save by migrating your test suite to TUnit.

import BenchmarkCalculator from '@site/src/components/BenchmarkCalculator';

<BenchmarkCalculator />

---

## How It Works

This calculator uses real benchmark data from our daily automated tests to estimate:

1. **Your current test execution time** based on test count and framework
2. **Estimated TUnit execution time** with standard JIT compilation
3. **Estimated TUnit AOT execution time** with Native AOT compilation
4. **Time saved per run** and projected annual savings

## Assumptions

- Calculations are based on average execution times from our benchmark suite
- Real-world results may vary based on test complexity and infrastructure
- Native AOT provides additional benefits like faster cold starts and lower memory usage

## Factors That Affect Performance

### Test Characteristics
- **Parallelizable tests** benefit most from TUnit's parallel-by-default execution
- **CPU-bound tests** see the largest speedups
- **I/O-bound tests** may see smaller relative improvements

### Infrastructure
- **CI/CD pipelines** benefit from faster test execution and reduced build times
- **Developer machines** benefit from faster local test runs during development
- **Native AOT** provides the biggest wins on cold starts (CI, containers)

## Real-World Impact

Based on our benchmark data:

- **Small projects** (50-200 tests): Save 2-10 seconds per run
- **Medium projects** (200-1000 tests): Save 10-60 seconds per run
- **Large projects** (1000+ tests): Save 1-5 minutes per run

For a development team running tests:
- **10 times per day** = 5-30 minutes saved daily
- **Per developer per year** = 20-120 hours saved annually

## Try It In Action

Want to see real benchmark data? Check out our [detailed benchmarks](/docs/benchmarks/) with actual execution times across different test scenarios.

Ready to migrate? See our [Migration Guides](/docs/migration/xunit) for step-by-step instructions.
