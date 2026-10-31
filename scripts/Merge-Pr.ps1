# Merge-Pr.ps1
# One-command, fail-closed merge for the issue-pr-loop skill:
#   1. runs the pure gate  Assert-PrGreen.ps1  (read-only; exits 0 only when green)
#   2. merges with  gh pr merge --squash  (only if the gate passed)
#   3. removes the PR's isolated worktree, then deletes its local branch
#
# Cleanup is part of the merge command, so an agent cannot merge and forget the
# worktree. The gate stays a separate, pure predicate: Merge-Pr composes it.
#
# The worktree is resolved by matching the PR's head branch against `git worktree list`
# unless -Worktree names it. Remove-MergedWorktree preserves a worktree with uncommitted
# tracked changes or commits beyond the merged head. GitHub deletes the remote branch
# on merge (delete_branch_on_merge); the local branch is deleted only while it still
# points at the merged head.
#
# Usage:  pwsh scripts/Merge-Pr.ps1 -Pr 1234 [-Repo owner/name] [-Worktree <path>]
# Exit:   0 = merged (worktree removed, or preserved because it holds work)
#         1 = NOT merged (gate denied, or merge failed); nothing destroyed

[CmdletBinding()]
param(
    [Parameter(Mandatory)][int]$Pr,
    [string]$Repo,
    [string]$Worktree
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'WorktreeCleanup.ps1')
$repoArgs = @(); if ($Repo) { $repoArgs = @('--repo', $Repo) }

function Fail([string]$msg) { [Console]::Error.WriteLine("MERGE ABORTED #${Pr} -- $msg"); exit 1 }

# --- 1. Gate: the pure predicate decides. No merge unless it exits 0. -----------
$gateArgs = @('-Pr', $Pr); if ($Repo) { $gateArgs += @('-Repo', $Repo) }
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Assert-PrGreen.ps1') @gateArgs
if ($LASTEXITCODE -ne 0) { Fail "Assert-PrGreen denied (exit $LASTEXITCODE). Not merging." }

# Resolve the head before merging so cleanup does not depend on later PR state.
$headJson = gh pr view $Pr @repoArgs --json headRefName,headRefOid 2>$null
if ($LASTEXITCODE -ne 0 -or -not $headJson) { Fail "could not resolve PR head (exit $LASTEXITCODE)" }
$head = $headJson | ConvertFrom-Json
$headRef = $head.headRefName
$headRefOid = $head.headRefOid
if (-not $headRef -or -not $headRefOid) { Fail "PR head response omitted branch or commit" }

# Git removals must run from a checkout that is NOT the one being removed; the first
# `worktree list` entry is always the main checkout.
$mainRepo = ((git worktree list --porcelain) | Where-Object { $_ -like 'worktree *' } |
    Select-Object -First 1) -replace '^worktree ', ''

# --- 2. Merge, pinned to the gated head. -------------------------------------------
gh pr merge $Pr @repoArgs --squash --match-head-commit $headRefOid
$mergeExit = $LASTEXITCODE
if ($mergeExit -ne 0) {
    # gh can complete the remote merge and then fail a local follow-up. GitHub's PR
    # state is authoritative; never report an already-merged PR as an abort.
    $state = gh pr view $Pr @repoArgs --json state --jq '.state' 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $state -or $state.Trim() -ne 'MERGED') {
        Fail "gh pr merge failed (exit $mergeExit). Worktree untouched."
    }
    Write-Host "gh exited $mergeExit after GitHub merged #${Pr}; continuing cleanup."
}
Write-Host "Merged #${Pr} ($headRef)."

# --- 3. Remove the PR's worktree, then its local branch. ---------------------------
if (-not $Worktree) {
    $cur = $null
    foreach ($line in (git -C $mainRepo worktree list --porcelain)) {
        if ($line -like 'worktree *') { $cur = $line.Substring(9) }
        elseif ($line -eq "branch refs/heads/$headRef" -and
            [IO.Path]::GetFullPath($cur) -ne [IO.Path]::GetFullPath($mainRepo)) {
            $Worktree = $cur
            break
        }
    }
}
if (-not $Worktree) {
    Write-Host "No isolated worktree found for branch '$headRef'."
}
else {
    Remove-MergedWorktree -Repo $mainRepo -Worktree $Worktree -ExpectedHead $headRefOid -Label "#${Pr}"
    if (Test-Path -LiteralPath $Worktree) {
        Write-Host "Preserving local branch '$headRef' because its worktree remains."
        exit 0
    }
}

$localHead = git -C $mainRepo rev-parse --verify --quiet "refs/heads/$headRef" 2>$null
if ($localHead -and $localHead.Trim() -eq $headRefOid) {
    git -C $mainRepo branch -D -- $headRef | Out-Null
}
elseif ($localHead) {
    Write-Host "Preserving local branch '$headRef' because it moved beyond PR head $headRefOid."
}
git -C $mainRepo worktree prune
exit 0
