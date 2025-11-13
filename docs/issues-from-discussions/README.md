# Issue Reports for TUnit Discussion #3803

## Overview

This directory contains documentation for two separate issues identified in the TUnit repository discussion #3803:
https://github.com/thomhurst/TUnit/discussions/3803#discussioncomment-14957214

## Issues Identified

### Issue 1: Constructor Called Twice Non-Deterministically
**File:** `ISSUE-constructor-called-twice.md`
- **Severity:** High
- **Type:** Bug
- **Summary:** Constructors of `SharedType.PerTestSession` data sources are called multiple times non-deterministically when `InitializeAsync` involves time-consuming operations

### Issue 2: Constructor Console Output Not Captured
**File:** `ISSUE-constructor-output-lost.md`
- **Severity:** Medium
- **Type:** Bug / Enhancement
- **Summary:** Console output from test data source constructors is not captured in test output, making debugging difficult

## Files in this Directory

1. **ISSUE-constructor-called-twice.md** - Detailed issue report for the constructor duplication bug
2. **ISSUE-constructor-output-lost.md** - Detailed issue report for the missing console output
3. **ISSUES-SUMMARY.md** - Combined summary with instructions for creating the issues
4. **create-issues.sh** - Bash script to create both issues using GitHub CLI (requires authentication)
5. **README.md** - This file

## How to Create the Issues

### Option 1: Use the provided script (requires GitHub CLI authentication)

```bash
cd /tmp/issue-reports
./create-issues.sh
```

**Note:** This requires:
- GitHub CLI (`gh`) installed
- Authenticated with proper permissions: `gh auth login`
- Write access to the thomhurst/TUnit repository

### Option 2: Create manually via GitHub web interface

1. Go to https://github.com/thomhurst/TUnit/issues/new
2. For each issue:
   - Copy the title and body from `ISSUES-SUMMARY.md`
   - Add the suggested labels
   - Submit

### Option 3: Use GitHub CLI manually

See the commands in `ISSUES-SUMMARY.md` under "Method 2: GitHub CLI"

## Why These Were Not Created Automatically

Due to security constraints and lack of GitHub API authentication in the current environment, issues cannot be created automatically. The comprehensive documentation provided ensures that:

1. All relevant information from the discussion is captured
2. Issues are well-structured with proper sections
3. Context and reproduction steps are documented
4. Suggested investigation areas are identified
5. Related issues are cross-referenced

## Next Steps

Choose one of the options above to create the actual GitHub issues in the thomhurst/TUnit repository.
