---
sidebar_position: 11
---

# Aggregated Reports (Multiple Test Projects)

When you run several test projects with a single command — `dotnet test` on a solution, or one `dotnet run` per microservice test suite — each project runs in its own process, and each process produces its own HTML report and its own GitHub Actions summary section. With many projects that gets noisy fast: N summary blocks to scroll through, N report artifacts to click into.

Report aggregation merges all of that into **one combined HTML report and one GitHub step summary block**, without an orchestrator project and without restructuring your solution.

**It is on by default on GitHub Actions** (and anywhere `TUNIT_AGGREGATE_DIR` is set). Disable it with `TUNIT_AGGREGATE_REPORTS=off`.

## How It Works

1. Alongside every HTML report, TUnit writes a machine-readable sidecar: `{AssemblyName}-{os}-{tfm}.tunit-report.json`. This is on by default (disable with `TUNIT_DISABLE_JSON_REPORT=true`).
2. With aggregation enabled, each test process also copies its sidecar into a directory shared by all sibling processes.
3. As each process finishes, it takes a cross-process lock, reads *all* sidecars present so far, and regenerates the merged HTML report and the summary block. The last process to finish naturally leaves the complete aggregate — no process ever needs to know whether it is the last one.

The summary block is bracketed by invisible HTML comment markers, and only the content between TUnit's own markers is ever rewritten. Anything else in your step summary — other tools' sections, your own `echo >> $GITHUB_STEP_SUMMARY` lines — is never touched.

## One Step, Many Projects (GitHub Actions) — Zero Config

If all your test projects run within a single workflow step (the common `dotnet test` on a solution), there is nothing to configure:

```yaml
- name: Run tests
  run: dotnet test MySolution.sln
```

You get:

- **One summary block** in the job summary — total counts across all suites, a per-suite rollup table (with links to each suite's report artifact when in-process upload is enabled), flaky tests, and failures grouped by cause.
- **One merged HTML report** with every suite's tests, written to the shared directory (the summary block prints its path). The path is inside `$RUNNER_TEMP`, so add an upload step if you want to keep it:

```yaml
- name: Upload merged report
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: merged-test-report
    path: ${{ runner.temp }}/tunit-aggregate/**/merged-report.html
```

On GitHub Actions the shared directory is derived automatically (scoped to run + attempt + job). On other CI systems or locally, aggregation activates once you point the processes at a shared directory:

```bash
TUNIT_AGGREGATE_DIR=/path/shared/by/all/test/processes
```

Without a resolvable shared directory (e.g. a plain local `dotnet run`), aggregation silently does nothing.

## Multiple Steps or Matrix Jobs: the `tunit-report` Tool

GitHub gives every workflow step its **own** summary file, uploaded when the step ends — a later step cannot rewrite an earlier step's block. So when test projects run as *separate steps* (or separate matrix jobs), cooperative merging can't produce a single block: with the default behaviour each step would emit its own cumulative block. Set defer mode on the test steps and add a final merge step instead:

```yaml
- name: Test service A
  run: dotnet run --project ServiceA.Tests
  env:
    TUNIT_AGGREGATE_REPORTS: defer
    TUNIT_AGGREGATE_DIR: ${{ runner.temp }}/tunit

- name: Test service B
  run: dotnet run --project ServiceB.Tests
  env:
    TUNIT_AGGREGATE_REPORTS: defer
    TUNIT_AGGREGATE_DIR: ${{ runner.temp }}/tunit

- name: Merge test reports
  if: always()
  run: |
    dotnet tool install --global TUnit.Reporting.Tool
    tunit-report merge --directory ${{ runner.temp }}/tunit --github-summary
```

In defer mode each process persists its sidecar and keeps the merged HTML fresh, but writes **no** per-step summary at all — the final `tunit-report merge --github-summary` step emits the single block.

For matrix jobs on different machines, upload each job's sidecars (or `TestResults` directories) as artifacts, download them all in a final job, and point `tunit-report merge` at the download directory — it scans recursively.

### Tool Reference

```
tunit-report merge --directory <dir> [options]

  -d, --directory <dir>    Directory scanned recursively for *.tunit-report.json sidecars
  -o, --output <file>      Merged HTML report path (default: <dir>/merged-report.html)
  --github-summary         Write the merged summary to $GITHUB_STEP_SUMMARY (stdout if unset)
  --style <style>          collapsible (default) or full
  --fail-on-failures       Exit 1 when any merged test failed
```

## Configuration Reference

| Environment Variable | Effect |
| --- | --- |
| `TUNIT_AGGREGATE_REPORTS` | Unset (default) — cooperative merge wherever a shared directory is resolvable (GitHub Actions, or explicit `TUNIT_AGGREGATE_DIR`); silently off otherwise. `defer` — persist sidecars + merged HTML only; no summary blocks (multi-step scenarios). `off` (also `false`/`0`/`no`/`disabled`/`none`) — no aggregation. |
| `TUNIT_AGGREGATE_DIR` | Shared directory for sidecars and the merged report. Required outside GitHub Actions; optional override on GitHub Actions. |
| `TUNIT_DISABLE_JSON_REPORT` | Disables the JSON sidecar written next to the HTML report. Note: sidecars are what aggregation and `tunit-report` consume. |

## Notes & Limitations

- Aggregation is driven by the HTML reporter's data pipeline — if you set `TUNIT_DISABLE_HTML_REPORTER`, no sidecars are produced and there is nothing to merge.
- With cooperative mode (the default) across *multiple steps in the same job*, each step appends its own progressively-larger block (earlier steps' blocks can't be rewritten). Use `defer` + the tool for that layout, or `off` to restore per-suite blocks.
- Suites are identified per assembly + OS + TFM, so multi-targeted projects appear as separate rows (e.g. `MyTests (.NET 8.0.x)` / `MyTests (.NET 9.0.x)`).
- The GitHub step summary is capped at 1 MB by GitHub; the aggregated block replaces N per-suite blocks, so it usually *reduces* summary size.

## See Also

- [HTML Test Report](/docs/guides/html-report) — the per-suite report and artifact upload
- [CI/CD Reporting](/docs/execution/ci-cd-reporting) — the GitHub Actions reporter
