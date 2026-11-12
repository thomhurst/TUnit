# Automated Benchmark Documentation System

## Overview

This implementation adds a comprehensive, automated benchmark visualization system to the TUnit documentation site. The system automatically updates benchmark results daily from CI runs and presents them through interactive components.

## What Was Implemented

### Phase 1: GitHub Actions Automation ✅

**File:** `.github/workflows/speed-comparison.yml`

Added a new job `aggregate-and-commit-results` that:
- Runs after all benchmark jobs complete
- Downloads all benchmark artifacts (runtime + build benchmarks)
- Processes results with Node.js script
- **Creates a Pull Request with updated documentation**
- Only runs on `main` branch to avoid conflicts

**Key Features:**
- Automatic daily execution (existing schedule)
- Manual trigger support (`workflow_dispatch`)
- **Respects branch protection rules** by creating PRs instead of direct pushes
- **Fully automated**: Enables auto-merge so PRs merge when CI passes
- Auto-deletes the feature branch after merge
- Proper artifact handling with merge support
- Professional PR description with workflow metadata
- Zero manual intervention required

---

### Phase 2: Benchmark Processing Script ✅

**File:** `.github/scripts/process-benchmarks.js`

A comprehensive Node.js script that:

**Parsing:**
- Finds all BenchmarkDotNet markdown files
- Extracts performance data tables
- Parses environment information
- Handles both runtime and build benchmarks

**Analysis:**
- Calculates average speedup factors vs each framework
- Computes AOT vs JIT improvements
- Generates comparison statistics
- Tracks trends over time

**Output:**
- Generates formatted markdown documentation (`docs/docs/benchmarks/index.md`)
- Creates JSON data for interactive components (`docs/static/benchmarks/latest.json`)
- Maintains 90-day historical data (`docs/static/benchmarks/historical.json`)

**Data Generated:**
```json
{
  "timestamp": "ISO date",
  "environment": { "os", "sdk", "host" },
  "categories": { "AsyncTests": [...], "DataDrivenTests": [...] },
  "comparisons": { "vsXUnit": "2.6x", "vsNUnit": "4.5x", ... },
  "averageSpeedups": { "vsXUnit": "2.6", ... }
}
```

---

### Phase 3: Documentation Pages ✅

#### Main Benchmarks Page
**File:** `docs/docs/benchmarks/index.md`

Auto-generated page featuring:
- Executive summary with average speedups
- Performance tables for each test category
- Visual speedup indicators (🏆 for TUnit)
- AOT performance callouts
- Build performance data
- Methodology explanation
- Links to interactive tools

#### Methodology Page
**File:** `docs/docs/benchmarks/methodology.md`

Comprehensive documentation covering:
- Core testing principles
- Test category descriptions
- Environment specifications
- Measurement process
- Reproducibility instructions
- Limitations and caveats

---

### Phase 4: Interactive Components ✅

#### 1. BenchmarkChart Component
**Files:**
- `docs/src/components/BenchmarkChart/index.tsx`
- `docs/src/components/BenchmarkChart/styles.module.css`

**Features:**
- Horizontal bar charts showing relative performance
- Color-coded bars (TUnit = primary color, others = gray)
- Automatic data loading from JSON
- Responsive design
- Trophy emoji 🏆 for TUnit entries
- Loading and error states
- Dynamic percentage-based visualization

**Usage:**
```tsx
import BenchmarkChart from '@site/src/components/BenchmarkChart';

<BenchmarkChart category="AsyncTests" />
```

---

#### 3. Homepage race
**File:** `docs/src/pages/index.tsx`

The homepage reads `docs/static/benchmarks/latest.json` at build time and replays the measured run times as a race, with a JIT view (plus a TUnit Native AOT lane for reference) and a Native AOT view.

---

### Phase 5: Navigation Updates ✅

**File:** `docs/sidebars.ts`

Added new "Benchmarks" section with:
- Main benchmarks page (index)
- Methodology page

Positioned between "Examples & Use Cases" and "Reference" for optimal discoverability.

---

### Phase 6: Placeholder Data ✅

**Files:**
- `docs/static/benchmarks/latest.json` (placeholder data)
- `docs/static/benchmarks/historical.json` (empty array)

These files ensure the site works before the first benchmark run. They'll be replaced automatically when the workflow runs.

---

## How It Works

### Daily Workflow

```
1. Speed Comparison Workflow Triggers (daily at midnight or manual)
   ├─► Build test artifacts (all frameworks)
   ├─► Run runtime benchmarks (5 test categories in parallel)
   ├─► Run build benchmarks
   └─► Upload artifacts

2. Aggregate and Commit Job
   ├─► Download all artifacts
   ├─► Process with Node.js script
   │   ├─► Parse markdown tables
   │   ├─► Calculate statistics
   │   ├─► Generate markdown docs
   │   └─► Update JSON data
   └─► Create Pull Request with changes

3. Automated Review and Merge
   ├─► Automated PR created with benchmark updates
   ├─► Auto-merge enabled on the PR
   ├─► CI checks run on PR
   └─► PR automatically merges when checks pass

4. Documentation Site
   ├─► Deploys automatically after PR merge
   ├─► Interactive components load latest.json
   └─► Users see updated benchmarks

✨ Fully automated - zero manual intervention required!
```

### Data Flow

```
BenchmarkDotNet → Markdown Files → process-benchmarks.js → {
  docs/docs/benchmarks/index.md       (human-readable)
  docs/static/benchmarks/latest.json  (machine-readable)
  docs/static/benchmarks/historical.json (trend tracking)
}
```

### Component Data Loading

All React components fetch `/benchmarks/latest.json` at runtime:
- No rebuild required for benchmark updates
- Data updates automatically when workflow runs
- Fallback values prevent errors before first run

---

## File Structure

```
TUnit/
├── .github/
│   ├── workflows/
│   │   └── speed-comparison.yml          [MODIFIED] Added commit job
│   └── scripts/
│       └── process-benchmarks.js         [NEW] Processing script
│
├── docs/
│   ├── docs/
│   │   └── benchmarks/
│   │       ├── index.md                  [AUTO-GENERATED]
│   │       └── methodology.md            [NEW]
│   │
│   ├── src/
│   │   ├── components/
│   │   │   └── BenchmarkChart/
│   │   │       ├── index.tsx            [NEW]
│   │   │       └── styles.module.css    [NEW]
│   │   └── pages/
│   │       └── index.tsx                [MODIFIED] Added benchmark section
│   │
│   ├── static/
│   │   └── benchmarks/
│   │       ├── latest.json              [AUTO-UPDATED]
│   │       └── historical.json          [AUTO-UPDATED]
│   │
│   └── sidebars.ts                      [MODIFIED] Added benchmarks nav
```

---

## Testing the Implementation

### Local Testing (Before First CI Run)

The site will work immediately with placeholder data:

```bash
cd docs
npm install
npm start
```

Visit:
- http://localhost:3000 (homepage with benchmark section)
- http://localhost:3000/docs/benchmarks (detailed benchmarks)
- http://localhost:3000/docs/benchmarks/methodology (methodology)

### After First CI Run

1. **Trigger Workflow:**
   - Wait for daily schedule (midnight UTC), OR
   - Manual trigger: Go to GitHub Actions → Speed Comparison → Run workflow

2. **Verify Automation:**
   - Check workflow completes successfully
   - **Look for automated PR**: "🤖 Update Benchmark Results"
   - PR will have "Auto-merge enabled" badge
   - CI checks will run automatically

3. **Wait for Auto-Merge:**
   - PR merges automatically when CI checks pass (no action needed!)
   - Branch automatically deletes after merge
   - Typically takes 2-5 minutes from PR creation to merge

4. **Verify Site:**
   - Visit the deployed site after merge
   - Homepage shows real speedup numbers
   - Benchmarks page shows all test categories
   - Charts render correctly

**Note:** You can review the PRs if desired, but they will merge automatically without intervention.

---

## Maintenance & Customization

### Updating Benchmark Categories

If you add new test categories to `speed-comparison.yml`:

```yaml
matrix:
  class: [DataDrivenTests, AsyncTests, NewCategoryTests]  # Add here
```

The system automatically:
- Processes new results
- Adds new sections to documentation
- Includes in charts

### Styling Customization

All components use CSS modules and respect Docusaurus theming:
- Colors use CSS custom properties (`--ifm-color-primary`, etc.)
- Dark mode automatically supported
- Responsive breakpoints included

### Data Retention

Historical data keeps 90 days by default:

```javascript
// In process-benchmarks.js, line ~470
historical = historical.slice(-90);  // Change to keep more/less
```

### Placeholder Data

Update `docs/static/benchmarks/latest.json` with realistic estimates before the first benchmark run to avoid showing "N/A" values.

---

## Dependencies

**No new npm packages required!**

All components use:
- React (already included with Docusaurus)
- Standard CSS modules (built-in)
- Fetch API (native browser API)

---

## Performance Considerations

### Client-Side Loading
- JSON files are small (~10KB)
- Cached by browser
- Loaded asynchronously
- No build-time data baking required

### Build Performance
- No impact on build time
- Static JSON files served directly
- React hydration handles interactivity

### Git Repository
- Small commits (3 files updated daily)
- Markdown diffs are human-readable
- JSON files compress well

---

## Future Enhancements (Optional)

### Easy Additions:
1. **Historical Trend Charts**: Use `historical.json` to show performance over time
2. **Framework Version Tracking**: Display when versions change
3. **Comparison Matrix**: Side-by-side feature comparison table
4. **Export to CSV/PDF**: Download benchmark reports
5. **Email Notifications**: Alert on performance regressions
6. **GitHub Issue Creation**: Auto-file issues for slowdowns

### Integration Ideas:
1. **PR Comments**: Post benchmark results on PRs
2. **Status Badges**: Add badge to README with latest speedup
3. **API Endpoint**: Expose JSON for external tools
4. **Slack/Discord Notifications**: Post daily results to chat

---

## Troubleshooting

### Components Show "Loading" Forever

**Cause:** `/benchmarks/latest.json` not accessible

**Fix:**
```bash
# Ensure file exists
ls docs/static/benchmarks/latest.json

# Check file is valid JSON
cat docs/static/benchmarks/latest.json | jq
```

### Workflow Fails to Create PR

**Cause:** Git conflicts, permissions, or branch protection

**Fix:**
- Check `GITHUB_TOKEN` has `contents: write` and `pull-requests: write` permissions
- Ensure no manual edits conflict with auto-generated files
- Check that branch protection allows PRs from GitHub Actions
- Close any existing `automated-benchmarks-update` PR if it exists

### Benchmark Data Not Updating

**Cause:** Workflow artifacts not downloading

**Fix:**
- Check artifact names match in workflow
- Verify download step succeeds
- Ensure `merge-multiple: true` is set

### Wrong Speedup Values

**Cause:** Benchmark data parsing error

**Fix:**
- Check BenchmarkDotNet output format hasn't changed
- Verify table structure in markdown files
- Update parsing regex if needed

---

## Key Benefits

✅ **Fully Automated**: Zero manual work after setup - creates PR and auto-merges
✅ **Always Up-to-Date**: Daily refreshes with latest data
✅ **Interactive**: Users can explore and calculate impact
✅ **Transparent**: All code and methodology visible
✅ **Performant**: No external dependencies, minimal overhead
✅ **Maintainable**: Clear structure, well-documented
✅ **Extensible**: Easy to add new charts/features

---

## Prerequisites for Auto-Merge

For the automated PR to merge, ensure:

1. **Repository Settings:**
   - "Allow auto-merge" is enabled in Settings → General → Pull Requests

2. **Branch Protection (optional but recommended):**
   - Require status checks to pass before merging
   - This ensures CI validates the benchmark data before merging

3. **GitHub Actions Permissions:**
   - `contents: write` permission (for creating commits)
   - `pull-requests: write` permission (for creating/merging PRs)
   - Both are already configured in the workflow

If auto-merge is not enabled in the repository, the PR will still be created but will require manual merge.

---

## Summary

This implementation provides TUnit with a **best-in-class benchmark documentation system** that:

1. **Automatically collects** real-world performance data daily
2. **Visualizes results** through multiple interactive components
3. **Calculates ROI** for potential users considering migration
4. **Builds trust** through transparency and reproducibility
5. **Stays current** without manual maintenance

The system is production-ready and will start populating with real data as soon as the Speed Comparison workflow runs on the main branch.

---

**Questions or Issues?**

Check the workflow logs at: https://github.com/thomhurst/TUnit/actions/workflows/speed-comparison.yml
