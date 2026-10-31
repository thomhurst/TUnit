# Assert-PrGreen.ps1
# Fail-closed merge gate for the issue-pr-loop skill.
#
# Default mode exits 0 ONLY when PR #<N> is provably safe to merge now:
#   - state OPEN, not a draft
#   - mergeable == MERGEABLE
#   - mergeStateStatus == CLEAN
#   - every current check is terminal AND passing (SUCCESS/SKIPPED/NEUTRAL)
#   - the `claude-review` check ran on the current head and passed
#   - no unresolved review threads
#
# -ForAutoMerge mode is the gate for arming native auto-merge (Enable-AutoMerge.ps1).
# CI may still be running, because GitHub waits for the required checks itself, but:
#   - no check has failed
#   - the `claude-review` check for the current head has COMPLETED and passed, so its
#     findings exist and can be read before arming
#   - no unresolved review threads
#
# Claude Code Review posts its findings as a PR conversation comment, and Codex and
# other bots post COMMENTED reviews, so no review state proves the findings were
# handled. The caller still reads every review body and conversation comment itself;
# this script is necessary but not sufficient.
#
# Any other state -- an empty check list, a failed check, an unresolved thread, or an API
# hiccup -- exits 1 so the caller MUST NOT merge or arm. The default on uncertainty is DENY.
#
# Usage: pwsh scripts/Assert-PrGreen.ps1 -Pr 1234 [-Repo owner/name] [-ForAutoMerge]
# Exit:  0 = safe to merge (or arm); 1 = DO NOT MERGE (reason printed to stderr).

[CmdletBinding()]
param(
    [Parameter(Mandatory)][int]$Pr,
    [string]$Repo,
    [switch]$ForAutoMerge
)

$ErrorActionPreference = 'Stop'

function Deny([string]$reason) {
    [Console]::Error.WriteLine("DO NOT MERGE #${Pr} -- $reason")
    exit 1
}

$repoArgs = @()
if ($Repo) { $repoArgs = @('--repo', $Repo) }

# Re-fetch fresh: survey output goes stale within seconds.
$raw = gh pr view $Pr @repoArgs --json number,state,headRefOid,isDraft,mergeable,mergeStateStatus 2>$null
if ($LASTEXITCODE -ne 0) { Deny "gh pr view failed (exit $LASTEXITCODE)" }
try {
    $view = $raw | ConvertFrom-Json
}
catch {
    Deny "could not parse PR state: $($_.Exception.Message)"
}

if ($view.state -ne 'OPEN') { Deny "state=$($view.state) (need OPEN)" }
if ($view.isDraft) { Deny "PR is a draft" }
if ($view.mergeable -ne 'MERGEABLE') { Deny "mergeable=$($view.mergeable) (need MERGEABLE)" }
if (-not $ForAutoMerge -and $view.mergeStateStatus -ne 'CLEAN') { Deny "mergeStateStatus=$($view.mergeStateStatus) (need CLEAN)" }

# `pr view` retains superseded check suites after same-head recovery. `pr checks`
# selects current executions by check name, workflow and event, and paginates them.
$checksRaw = gh pr checks $Pr @repoArgs --json name,state,workflow,link 2>$null
# `gh pr checks` exits 8 while checks are pending; the JSON is still complete.
if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 8) { Deny "gh pr checks failed (exit $LASTEXITCODE)" }
try {
    $checks = ConvertFrom-Json -InputObject ($checksRaw -join "`n") -NoEnumerate
}
catch {
    Deny "could not parse PR checks: $($_.Exception.Message)"
}
if ($checks -isnot [array] -or $checks.Count -eq 0) {
    Deny "PR checks response is empty or not an array -- refusing to merge blind"
}

# Workflow concurrency cancels an in-flight run when a newer run of the same workflow
# starts on the PR. The cancelled run still publishes checks. Drop a run's checks only
# when GitHub reports that run as cancelled and a non-cancelled run of the same workflow
# started after it. A check whose run cannot be read is always evaluated.
function Get-RunId($check) {
    if ("$($check.link)" -match '/actions/runs/(\d+)/') { return [long]$Matches[1] }
    return $null
}
$runsPath = if ($Repo) { "repos/$Repo/actions/runs" } else { 'repos/{owner}/{repo}/actions/runs' }
$runs = @{}
foreach ($runId in @($checks | ForEach-Object { Get-RunId $_ } | Where-Object { $null -ne $_ } | Sort-Object -Unique)) {
    $runRaw = gh api "$runsPath/$runId" --jq '{workflow_id, conclusion, run_started_at}' 2>$null
    if ($LASTEXITCODE -ne 0) { continue }
    try { $run = $runRaw | ConvertFrom-Json } catch { continue }
    if ($null -eq $run.run_started_at) { continue }
    $runs[$runId] = @{ Workflow = $run.workflow_id; Cancelled = $run.conclusion -eq 'cancelled'; Started = [datetimeoffset]$run.run_started_at }
}
$supersededRuns = @($runs.Keys | Where-Object {
        $cancelled = $runs[$_]
        $cancelled.Cancelled -and @($runs.Values | Where-Object {
                $_.Workflow -eq $cancelled.Workflow -and -not $_.Cancelled -and $_.Started -gt $cancelled.Started
            }).Count -gt 0
    })
if ($supersededRuns.Count -gt 0) {
    $checks = @($checks | Where-Object { $supersededRuns -notcontains (Get-RunId $_) })
}

$allowedStates = @('SUCCESS', 'SKIPPED', 'NEUTRAL')
$pendingStates = @('EXPECTED', 'REQUESTED', 'WAITING', 'QUEUED', 'PENDING', 'IN_PROGRESS')
$reviewCheck = 'claude-review'
$review = @($checks | Where-Object { $_.name -eq $reviewCheck })
if ($review.Count -eq 0) {
    Deny "no $reviewCheck check on the current head yet; wait for Claude Code Review, or dispatch it with gh workflow run claude-code-review.yml -f pr_number=$Pr"
}
if (@($review | Where-Object { $_.state -ne 'SUCCESS' }).Count -gt 0) {
    Deny "$reviewCheck has not passed on the current head ($(($review | ForEach-Object state) -join ', ')); read its findings once it completes"
}
$bad = @()
foreach ($c in $checks) {
    if ([string]::IsNullOrWhiteSpace($c.name) -or [string]::IsNullOrWhiteSpace($c.state)) {
        $bad += "unrecognized check entry: $($c | ConvertTo-Json -Compress)"
        continue
    }
    if ($allowedStates -contains $c.state) { continue }
    # Auto-merge waits for pending CI itself. Unknown states fail closed rather than
    # being mistaken for pending work.
    if ($ForAutoMerge -and $pendingStates -contains $c.state) { continue }
    $bad += "$($c.name): state=$($c.state)"
}
if ($bad.Count -gt 0) {
    Deny ("checks not green: " + ($bad -join '; '))
}

# Unresolved review threads block merge even when a review's state is COMMENTED.
if ($Repo) {
    $owner, $name = $Repo -split '/', 2
}
else {
    $nwo = gh repo view --json nameWithOwner --jq '.nameWithOwner' 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $nwo) { Deny "could not resolve repo for review-thread check" }
    $owner, $name = $nwo.Trim() -split '/', 2
}

$query = 'query($owner:String!,$repo:String!,$pr:Int!,$after:String){repository(owner:$owner,name:$repo){pullRequest(number:$pr){reviewThreads(first:100,after:$after){pageInfo{hasNextPage endCursor} nodes{isResolved}}}}}'
$unresolved = 0
$after = $null
do {
    $graphqlArgs = @('api', 'graphql', '-f', "query=$query", '-F', "owner=$owner", '-F', "repo=$name", '-F', "pr=$Pr")
    if ($after) { $graphqlArgs += @('-f', "after=$after") }
    $threadsRaw = & gh @graphqlArgs 2>$null
    if ($LASTEXITCODE -ne 0) { Deny "could not fetch review threads (exit $LASTEXITCODE)" }
    try {
        $reviewThreads = ($threadsRaw | ConvertFrom-Json).data.repository.pullRequest.reviewThreads
    }
    catch {
        Deny "could not parse review threads: $($_.Exception.Message)"
    }
    if ($null -eq $reviewThreads) { Deny "review-thread response did not contain the requested PR" }
    $unresolved += @($reviewThreads.nodes | Where-Object { -not $_.isResolved }).Count
    $next = if ($reviewThreads.pageInfo.hasNextPage) { [string]$reviewThreads.pageInfo.endCursor } else { $null }
    if ($reviewThreads.pageInfo.hasNextPage -and (-not $next -or $next -eq $after)) { Deny "review-thread pagination returned no new cursor" }
    $after = $next
}
while ($after)
if ($unresolved -gt 0) { Deny "$unresolved unresolved review thread(s)" }

if ($ForAutoMerge) {
    Write-Host "OK #${Pr} -- MERGEABLE, no failed checks, $reviewCheck passed on head, no unresolved threads. Safe to arm auto-merge."
}
else {
    Write-Host "OK #${Pr} -- MERGEABLE, CLEAN, $($checks.Count) check(s) green, no unresolved threads. Safe to merge."
}
exit 0
