# Enable-AutoMerge.ps1
# Arms GitHub native auto-merge (squash) for PR #<N>, pinned to the reviewed head.
#
#   1. runs  Assert-PrGreen.ps1 -ForAutoMerge  (no failed checks, `claude-review` passed
#      on the current head, no unresolved threads) and stops on any denial
#   2. arms  gh pr merge --auto --squash --match-head-commit <head>
#
# GitHub then merges once main's branch protection passes: its required checks and
# resolved conversations. GitHub does NOT disarm auto-merge when a new commit is pushed,
# so disarm it (gh pr merge --disable-auto) before pushing to an armed PR, and run this
# script again only after the new head's review.
#
# The caller must read and address every review body, inline finding, and issue comment
# BEFORE running this; the gate cannot judge review-body content.
#
# Auto-merge leaves the PR's worktree in place. Clean it up after the merge with
# scripts/Remove-MergedWorktrees.ps1.
#
# Usage: pwsh scripts/Enable-AutoMerge.ps1 -Pr 1234 [-Repo owner/name]
# Exit:  0 = armed; 1 = not armed (reason printed to stderr).

[CmdletBinding()]
param(
    [Parameter(Mandatory)][int]$Pr,
    [string]$Repo
)

$ErrorActionPreference = 'Stop'

function Fail([string]$reason) {
    [Console]::Error.WriteLine("NOT ARMED #${Pr} -- $reason")
    exit 1
}

$repoArgs = @()
if ($Repo) { $repoArgs = @('--repo', $Repo) }

# Capture the head before the gate so a push racing the gate cannot be armed unreviewed:
# --match-head-commit makes GitHub refuse if the head moved since.
$head = gh pr view $Pr @repoArgs --json headRefOid --jq '.headRefOid' 2>$null
if ($LASTEXITCODE -ne 0 -or -not $head) { Fail "could not read PR head (exit $LASTEXITCODE)" }
$head = $head.Trim()

$gateArgs = @('-Pr', $Pr, '-ForAutoMerge')
if ($Repo) { $gateArgs += @('-Repo', $Repo) }
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Assert-PrGreen.ps1') @gateArgs
if ($LASTEXITCODE -ne 0) { Fail "auto-merge gate denied (see above)" }

gh pr merge $Pr @repoArgs --auto --squash --match-head-commit $head
if ($LASTEXITCODE -ne 0) { Fail "gh pr merge --auto failed (exit $LASTEXITCODE)" }

Write-Host "Armed auto-merge for #${Pr} at $head."
exit 0
