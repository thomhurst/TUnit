# Start-IssuePrLoop.ps1
# Runs the issue-pr-loop skill one unit at a time, each in a fresh agent process.
# A long-lived agent session keeps growing its context, and model latency grows with
# it. Starting a new process per unit keeps every unit's context small.
#
# The script, not the agent, does the cheap deterministic work of each unit:
#   1. Checks that the lock Redis answers. When it does not (for example Docker is
#      down), it backs off without starting an agent.
#   2. Runs the merged-worktree sweep and surveys open PRs and issues with `gh`. When
#      the latest push CI run of any workflow on main failed, fixing main comes first.
#   3. Includes draft PRs for review processing. Skips `needs-human` and `on-hold`
#      items, PRs whose checks are still running, and items whose GitHub state is
#      unchanged since an agent last deferred them. Also skips issues that cannot be worked yet: issues with open native
#      sub-issues or open `blocked_by` dependencies, issues that already have an open
#      closing PR, and bot-owned tracking issues. Stranded branches follow the same
#      rules. Ranks local `issue-*` branches with unpublished commits and no open PR
#      above new issues. Takes issues by `priority:*` tier (critical, high, medium or
#      unlabelled, low), `ci` first within a tier, then parents whose sub-issues are all
#      closed, then issues that block the most other issues. An item that a deferral
#      named as its blocker goes first, so the next unit works the blocker instead of
#      the blocked item.
#   4. Acquires the item's Redis lock, moving on to the next candidate when another
#      worker holds it.
#   5. Starts the agent on that one item, with lower reasoning effort for PRs that
#      look ready to merge. When nothing is actionable, or every -TriageEvery units,
#      it starts a triage unit instead: the agent audits a batch of open issues and
#      parked PRs that no unit audited in the last -AuditDays days (-ParkedAuditHours for
#      parked items). When triage has nothing due either, it starts a discovery unit at
#      most every -DiscoveryHours, which files new claimable issues, and otherwise lists
#      the items that wait for a person before it sleeps. A parent whose open sub-issues
#      are all parked or blocked becomes a candidate, so a unit can unpark or flatten it.
# After the agent ends, the script releases the lock if the agent left it held. On a
# deferred, waiting, failed, or missing result it records the item's current fingerprint
# with `AgentLocks.ps1 defer`, so later units skip it until its head, checks,
# mergeability, comments, or labels change, or until the deferral expires. Comments
# marked with `<!-- issue-pr-loop -->` are the loop's own and do not count as a change.
# Each repeat deferral of an unchanged item doubles its expiry, starting at -DeferSeconds
# and capped at -MaxDeferSeconds. A deferral whose blocker is parked (`needs-human` or
# `on-hold`) does not expire while that blocker stays parked. A `waiting` result names a
# workflow run, and the item returns when that run completes. A deferral without a
# category gets a short expiry. The next unit on a deferred item receives the earlier
# reason, so it can start from that blocker. Every unit appends one JSON line to
# units.jsonl in the log directory; -Report summarizes it.
#
# AGENT SELECTION (-Agent auto):
#   - CLAUDECODE=1        -> launched from Claude Code, so run `claude -p`.
#   - CODEX_THREAD_ID set -> launched from Codex, so run `codex exec`.
#   - otherwise           -> the only agent CLI on PATH; Codex when both are present.
#
# Each run gets its own lock owner (the *_AGENT_LOCK_OWNER_ID variable that
# scripts/AgentLocks.ps1 reads), so ownership never leaks between units. Claude units
# stream JSON events: each event goes to the unit log and each tool call prints one
# progress line, because plain `claude -p` prints nothing until the unit exits. The agent
# ends with a `RESULT: ...` line (see the skill's single-unit mode). When nothing is
# actionable the loop waits -IdleSeconds before the next survey, because pending CI and
# reviews make new work appear later.
#
# Run several copies in separate terminals for parallel workers; Redis locks keep
# them on different items.
#
# Stop:   create the stop file printed at startup (checked between units), or Ctrl+C.
# Usage:  pwsh scripts/Start-IssuePrLoop.ps1 [-Agent auto|claude|codex] [-IdleSeconds n] [-MaxUnits n]
#           [-DeferSeconds n] [-MaxDeferSeconds n] [-LightEffort level] [-HeavyEffort level]
#           [-TriageEvery n] [-TriageBatch n] [-AuditDays n] [-ParkedAuditHours n] [-DiscoveryHours n] [-DryRun]
#         pwsh scripts/Start-IssuePrLoop.ps1 -Report [-ReportHours n]

[CmdletBinding()]
param(
    [ValidateSet('auto', 'claude', 'codex')]
    [string]$Agent = 'auto',
    [ValidateRange(0, 86400)]
    [int]$IdleSeconds = 600,
    # 0 means no limit.
    [ValidateRange(0, [int]::MaxValue)]
    [int]$MaxUnits = 0,
    # How long a deferral suppresses an item whose GitHub state has not changed.
    [ValidateRange(60, 604800)]
    [int]$DeferSeconds = 21600,
    # Upper bound for a repeatedly deferred item's backoff.
    [ValidateRange(60, 604800)]
    [int]$MaxDeferSeconds = 259200,
    # Reasoning effort for PRs that look ready to merge, and for all other units.
    # An empty value keeps the agent's configured default.
    [string]$LightEffort = 'medium',
    [string]$HeavyEffort = '',
    [string]$LogDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'issue-pr-loop'),
    # Run a triage unit after this many work units even when work is queued. 0 disables it.
    [ValidateRange(0, 1000)]
    [int]$TriageEvery = 10,
    # Most issues and PRs that one triage unit audits.
    [ValidateRange(1, 50)]
    [int]$TriageBatch = 8,
    # Days before triage audits an item again.
    [ValidateRange(1, 90)]
    [int]$AuditDays = 3,
    # Hours before triage audits a parked (`needs-human` or `on-hold`) item again.
    [ValidateRange(1, 168)]
    [int]$ParkedAuditHours = 24,
    # Least hours between discovery units, which run only when nothing else is due. 0 disables them.
    [ValidateRange(0, 168)]
    [int]$DiscoveryHours = 6,
    # Survey and claim once, print the chosen item and its prompt, release, and exit.
    [switch]$DryRun,
    # Summarize units.jsonl for this repository over the last -ReportHours, and exit.
    [switch]$Report,
    [ValidateRange(1, 8760)]
    [int]$ReportHours = 24
)

$ErrorActionPreference = 'Stop'

function Resolve-Agent([string]$Requested) {
    if ($Requested -ne 'auto') { return $Requested }
    if ($env:CLAUDECODE -eq '1') { return 'claude' }
    if (-not [string]::IsNullOrWhiteSpace($env:CODEX_THREAD_ID)) { return 'codex' }
    $hasCodex = [bool](Get-Command codex -ErrorAction SilentlyContinue)
    $hasClaude = [bool](Get-Command claude -ErrorAction SilentlyContinue)
    if ($hasCodex) { return 'codex' }
    if ($hasClaude) { return 'claude' }
    throw 'Neither codex nor claude is on PATH. Install one or pass -Agent.'
}

# Appends one stream-json line to the log, prints a progress line for each tool call,
# and returns the final answer text from the `result` event.
function Write-ClaudeEvent([string]$Line, [string]$Log) {
    Add-Content -LiteralPath $Log -Value $Line
    try { $streamEvent = $Line | ConvertFrom-Json -ErrorAction Stop }
    catch {
        # stderr and other non-JSON output.
        if ($Line.Trim()) { Write-Host "  $Line" }
        return
    }
    if ($streamEvent.type -eq 'result') { return [string]$streamEvent.result }
    if ($streamEvent.type -ne 'assistant') { return }
    foreach ($block in @($streamEvent.message.content)) {
        if ($block.type -ne 'tool_use') { continue }
        $toolInput = $block.input
        $detail = @($toolInput.description, $toolInput.command, $toolInput.file_path, $toolInput.pattern, $toolInput.skill, $toolInput.prompt) |
            Where-Object { $_ } |
            Select-Object -First 1
        $detail = ([string]$detail -replace '\s+', ' ').Trim()
        if ($detail.Length -gt 120) { $detail = $detail.Substring(0, 117) + '...' }
        Write-Host ('  {0:HH:mm:ss} {1}: {2}' -f (Get-Date), $block.name, $detail)
    }
}

$repo = git -C $PSScriptRoot rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or -not $repo) { throw 'Start-IssuePrLoop.ps1 must run from a Git checkout.' }
$repo = $repo.Trim()
$unitsFile = Join-Path $LogDirectory 'units.jsonl'

if ($Report) {
    Push-Location $repo
    try { $reportSlug = gh repo view --json nameWithOwner --jq .nameWithOwner }
    finally { Pop-Location }
    if (-not (Test-Path -LiteralPath $unitsFile)) { Write-Host "issue-pr-loop: no unit records in $unitsFile"; exit 0 }
    $since = (Get-Date).ToUniversalTime().AddHours(-$ReportHours)
    $records = @(Get-Content -LiteralPath $unitsFile | ForEach-Object { try { $_ | ConvertFrom-Json } catch { } } |
            Where-Object { $_ -and $_.repo -eq ([string]$reportSlug).Trim() -and [DateTime]$_.endedAt -ge $since })
    $total = [Math]::Max(1, ($records | Measure-Object seconds -Sum).Sum)
    Write-Host "issue-pr-loop: $($records.Count) unit(s) for $reportSlug in the last $ReportHours h"
    $records | Group-Object outcome | Sort-Object Count -Descending | ForEach-Object {
        $sum = ($_.Group | Measure-Object seconds -Sum).Sum
        Write-Host ('  {0,-10} {1,5} units  {2,6:N1} min avg  {3,5:N1} h  {4,3:N0}% of agent time' -f $_.Name, $_.Count, ($sum / $_.Count / 60), ($sum / 3600), (100 * $sum / $total))
    }
    $repeats = @($records | Where-Object outcome -eq 'deferred' | Group-Object { "$($_.kind) #$($_.number)" } | Where-Object Count -gt 1 | Sort-Object Count -Descending | Select-Object -First 10)
    if ($repeats.Count -gt 0) {
        Write-Host '  repeat deferrals:'
        foreach ($g in $repeats) { Write-Host ('    {0,-14} {1,3}x  {2}' -f $g.Name, $g.Count, ($g.Group | Select-Object -Last 1).result) }
    }
    exit 0
}

$stopFile = Join-Path (Resolve-Path (git -C $repo rev-parse --git-common-dir)) 'issue-pr-loop.stop'
$selected = Resolve-Agent $Agent
$agentLocks = Join-Path $repo 'scripts/AgentLocks.ps1'
$sweepScript = Join-Path $repo 'scripts/Remove-MergedWorktrees.ps1'

# Each repository's lock script names its own owner variable (for example RESPIRE_AGENT_LOCK_OWNER_ID).
$ownerVariable = Select-String -LiteralPath $agentLocks -CaseSensitive -Pattern '\b([A-Z][A-Z0-9_]*_AGENT_LOCK_OWNER_ID)\b' |
    Select-Object -First 1 |
    ForEach-Object { $_.Matches[0].Groups[1].Value }
if (-not $ownerVariable) { throw 'scripts/AgentLocks.ps1 does not name an *_AGENT_LOCK_OWNER_ID variable.' }

# The skill lives under .claude or .agents depending on the repository; name it by path so either agent finds it.
# With core.symlinks=false a symlinked SKILL.md checks out as a one-line path stub, so require real content.
$skill = @('.agents/skills/issue-pr-loop/SKILL.md', '.claude/skills/issue-pr-loop/SKILL.md') |
    Where-Object {
        $candidate = Join-Path $repo $_
        (Test-Path -LiteralPath $candidate) -and (Select-String -LiteralPath $candidate -Pattern '^name:\s*issue-pr-loop' -Quiet)
    } |
    Select-Object -First 1
if (-not $skill) { throw 'issue-pr-loop SKILL.md not found under .claude/skills or .agents/skills.' }
New-Item -ItemType Directory -Force $LogDirectory | Out-Null

Push-Location $repo
try { $repoSlug = gh repo view --json nameWithOwner --jq .nameWithOwner }
finally { Pop-Location }
if ($LASTEXITCODE -ne 0 -or -not $repoSlug) { throw 'gh repo view failed; check gh auth status.' }
$repoSlug = $repoSlug.Trim()

function Invoke-Locks([string[]]$Arguments, [int]$TimeoutSeconds = 120) {
    # Docker Desktop can hang `docker` calls indefinitely, so bound every lock call.
    $ownerValue = [Environment]::GetEnvironmentVariable($ownerVariable)
    $job = Start-Job -ScriptBlock {
        param($Script, $LockArgs, $OwnerName, $OwnerValue)
        if ($OwnerName) { Set-Item -Path "Env:$OwnerName" -Value $OwnerValue }
        $out = @(& pwsh -NoProfile -File $Script @LockArgs 2>&1 | ForEach-Object { $_.ToString() })
        [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($out -join "`n").Trim() }
    } -ArgumentList $agentLocks, $Arguments, $ownerVariable, $ownerValue
    try {
        if (-not (Wait-Job $job -Timeout $TimeoutSeconds)) {
            return [pscustomobject]@{ ExitCode = 124; Output = "timed out after $TimeoutSeconds s" }
        }
        return Receive-Job $job
    }
    finally { Remove-Job $job -Force }
}

function Get-Hash([string]$Text) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text))).ToLowerInvariant()
}

function Get-CheckState($Check) {
    if ($Check.PSObject.Properties['conclusion'] -and $Check.conclusion) { return [string]$Check.conclusion }
    if ($Check.PSObject.Properties['state'] -and $Check.state) { return [string]$Check.state }
    return [string]$Check.status
}

# Agents mark every comment they post with this, so the loop's own blocker comments do
# not make a deferred item look changed.
$loopMarker = '<!-- issue-pr-loop -->'

# Returns the newest creation time among comments the loop did not write.
function Get-LatestOutsideComment($Comments) {
    @($Comments | Where-Object { $_ -and -not ([string]$_.body).Contains($loopMarker) } |
        ForEach-Object { [string]$_.createdAt } | Sort-Object) | Select-Object -Last 1
}

# The fingerprint covers everything that can make a deferred item worth another look.
# Survey and post-unit fingerprints use the same fields, so they compare directly.
# They leave out updatedAt, which every loop comment and cross-reference changes.
function Get-PrFingerprint($Pr) {
    $checks = @($Pr.statusCheckRollup | ForEach-Object {
            $name = if ($_.PSObject.Properties['name'] -and $_.name) { $_.name } else { $_.context }
            '{0}={1}' -f $name, (Get-CheckState $_)
        } | Sort-Object)
    $labels = @($Pr.labels | ForEach-Object { $_.name } | Sort-Object)
    # The loop replies to review threads as the PR author, which GitHub records as reviews.
    $review = @($Pr.reviews | Where-Object { $_.author.login -ne $Pr.author.login } |
        ForEach-Object { [string]$_.submittedAt } | Sort-Object) | Select-Object -Last 1
    $comment = Get-LatestOutsideComment $Pr.comments
    # Codex can approve with a reaction on our request instead of posting a review.
    # Wake the agent on reaction changes; it verifies the reacting account and verdict.
    $codexReactions = @($Pr.comments | Where-Object {
            ([string]$_.body).Contains("<!-- codex-review:$($Pr.headRefOid) -->")
        } | ForEach-Object {
            '{0}={1}' -f $_.id, (ConvertTo-Json -InputObject $_.reactionGroups -Compress -Depth 10)
        } | Sort-Object)
    Get-Hash ((@($Pr.headRefOid, $Pr.isDraft, $Pr.mergeable, ($labels -join ','), $review, $comment) + $checks + $codexReactions) -join '|')
}

function Get-IssueFingerprint($Issue) {
    $labels = @($Issue.labels.nodes | ForEach-Object { $_.name } | Sort-Object)
    $assignees = @($Issue.assignees.nodes | ForEach-Object { $_.login } | Sort-Object)
    $prs = @($Issue.closedByPullRequestsReferences.nodes | ForEach-Object { '{0}:{1}' -f $_.number, $_.state } | Sort-Object)
    $children = '{0}/{1}' -f $Issue.subIssuesSummary.completed, $Issue.subIssuesSummary.total
    Get-Hash ((@($Issue.lastEditedAt, ($labels -join ','), ($assignees -join ','), (Get-LatestOutsideComment $Issue.comments.nodes),
                $children, $Issue.issueDependenciesSummary.blockedBy, ($prs -join ','))) -join '|')
}

# Returns number -> state (OPEN, PARKED, CLOSED, MERGED, MISSING) for issues and PRs, in
# one query. PARKED is an open item labelled `needs-human` or `on-hold`: no unit works it,
# so an item blocked on it waits for a person or for the label to go. Returns $null when
# the lookup fails, so callers can fall back to the item's own fingerprint.
function Get-ReferenceStates([int[]]$Numbers) {
    $Numbers = @($Numbers | Sort-Object -Unique)
    $states = @{}
    if ($Numbers.Count -eq 0) { return $states }
    $owner, $name = $repoSlug -split '/', 2
    $item = 'state labels(first: 30) { nodes { name } }'
    $fields = ($Numbers | ForEach-Object { "n${_}: issueOrPullRequest(number: $_) { ... on Issue { $item } ... on PullRequest { $item } }" }) -join ' '
    $query = 'query($owner: String!, $name: String!) { repository(owner: $owner, name: $name) { ' + $fields + ' } }'
    # A missing number makes gh exit 1, but stdout still carries the data for the rest.
    $json = gh api graphql -f "query=$query" -f "owner=$owner" -f "name=$name" 2>$null
    try { $repository = ($json | ConvertFrom-Json -ErrorAction Stop).data.repository }
    catch { return $null }
    if (-not $repository) { return $null }
    foreach ($n in $Numbers) {
        $node = $repository."n$n"
        $states[$n] = if (-not $node) { 'MISSING' }
        elseif ($node.state -eq 'OPEN' -and @($node.labels.nodes | Where-Object { $_.name -in $parkedLabels }).Count -gt 0) { 'PARKED' }
        else { [string]$node.state }
    }
    return $states
}

# A deferral reason names its blockers as #N. Recording their states lets a survey retry
# the item as soon as a blocker closes or merges, even though the item itself is unchanged.
# Longer digit runs after # are run, job or comment ids, not issue numbers, and overflow [int].
function Get-ReasonReferences([string]$Reason, [int]$Self) {
    @([regex]::Matches($Reason, '#(\d{1,7})(?!\d)') | ForEach-Object { [int]$_.Groups[1].Value } |
        Where-Object { $_ -ne $Self } | Sort-Object -Unique | Select-Object -First 20)
}

function Format-References([int[]]$Numbers, [hashtable]$States) {
    ($Numbers | ForEach-Object { '{0}:{1}' -f $_, $States[$_] }) -join ','
}

$prFields = 'number,title,author,headRefOid,mergeable,mergeStateStatus,statusCheckRollup,isDraft,labels,comments,reviews'
$issueNodeFields = 'number title lastEditedAt author { login __typename } labels(first: 50) { nodes { name } } ' +
    'assignees(first: 10) { nodes { login } } comments(last: 20) { totalCount nodes { createdAt body } } ' +
    'subIssuesSummary { total completed } subIssues(first: 50) { nodes { number state } } issueDependenciesSummary { blockedBy blocking } ' +
    'closedByPullRequestsReferences(first: 10, includeClosedPrs: false) { nodes { number state } }'

# Labels that take an issue out of the queue. `in-progress` is handled separately.
$excludedIssueLabels = @('wontfix', 'duplicate', 'question', 'needs-human', 'on-hold')
# Labels that take a PR out of the queue until a human removes them.
$excludedPrLabels = @('needs-human', 'on-hold')
# Labels that park an item: an item blocked on a parked item waits until the label goes.
$parkedLabels = @('needs-human', 'on-hold')
# Deferral categories the skill allows. A deferral without one gets a short expiry.
$deferCategories = @('human', 'upstream', 'credentials', 'locked')

# Counts why the last survey skipped items, so an idle survey can say why nothing was actionable.
$surveyStats = [ordered]@{}

function Reset-SurveyStats {
    foreach ($key in 'issues', 'issue-deferred', 'issue-locked', 'issue-excluded', 'issue-parent', 'issue-blocked', 'issue-has-pr', 'issue-claimed',
        'prs', 'pr-deferred', 'pr-locked', 'pr-pending', 'pr-excluded', 'pr-draft') {
        $surveyStats[$key] = 0
    }
}

function Format-SurveyStats {
    $s = $surveyStats
    ('issues: {0} open, {1} deferred, {2} locked, {3} excluded by label/assignee/bot, {4} with open sub-issues, {5} blocked, {6} with open PR, {7} claimed; ' +
        'PRs: {8} open, {9} deferred, {10} locked, {11} pending checks, {12} needs-human/on-hold, {13} draft') -f `
        $s['issues'], $s['issue-deferred'], $s['issue-locked'], $s['issue-excluded'], $s['issue-parent'], $s['issue-blocked'], $s['issue-has-pr'], $s['issue-claimed'],
        $s['prs'], $s['pr-deferred'], $s['pr-locked'], $s['pr-pending'], $s['pr-excluded'], $s['pr-draft']
}

function Invoke-GraphQl([string]$Query, [string[]]$Variables) {
    $owner, $name = $repoSlug -split '/', 2
    $json = gh api graphql -f "query=$Query" -f "owner=$owner" -f "name=$name" @Variables
    if ($LASTEXITCODE -ne 0) { throw 'gh api graphql failed.' }
    return ($json | ConvertFrom-Json).data.repository
}

function Get-OpenIssues {
    $query = 'query($owner: String!, $name: String!, $cursor: String) { repository(owner: $owner, name: $name) { ' +
        'issues(states: OPEN, first: 50, after: $cursor) { pageInfo { hasNextPage endCursor } nodes { ' + $issueNodeFields + ' } } } }'
    $issues = [Collections.Generic.List[object]]::new()
    $cursor = $null
    do {
        $variables = if ($cursor) { @('-f', "cursor=$cursor") } else { @() }
        $page = (Invoke-GraphQl $query $variables).issues
        foreach ($node in @($page.nodes)) { $issues.Add($node) }
        $cursor = $page.pageInfo.endCursor
    } while ($page.pageInfo.hasNextPage)
    return $issues
}

function Get-Issue([int]$Number) {
    $query = 'query($owner: String!, $name: String!, $number: Int!) { repository(owner: $owner, name: $name) { issue(number: $number) { ' +
        $issueNodeFields + ' } } }'
    return (Invoke-GraphQl $query @('-F', "number=$Number")).issue
}

function Test-HasLabel($Issue, [string[]]$Names) {
    @($Issue.labels.nodes | Where-Object { $_.name -in $Names }).Count -gt 0
}

# Lower sorts first. An issue without a priority label ranks as medium.
function Get-IssuePriority($Issue) {
    if (Test-HasLabel $Issue @('priority:critical')) { 0 }
    elseif (Test-HasLabel $Issue @('priority:high')) { 1 }
    elseif (Test-HasLabel $Issue @('priority:low')) { 3 }
    else { 2 }
}

function Test-HasOpenPr($Issue) {
    @($Issue.closedByPullRequestsReferences.nodes | Where-Object { $_.state -eq 'OPEN' }).Count -gt 0
}

# Local issue branches with commits that are not on origin/main, for open issues that have
# no open PR. A unit that could not push (for example because the lock service hung)
# leaves finished work here, and nothing else would publish it. A branch that ever had a
# PR is skipped: it was merged, split, or closed on purpose.
function Get-StrandedBranches([hashtable]$OpenIssues) {
    $byIssue = @{}
    foreach ($line in @(git -C $repo for-each-ref --format='%(refname:short) %(objectname)' 'refs/heads/issue-*' 2>$null)) {
        $branch, $sha = ([string]$line).Trim() -split ' ', 2
        if ($branch -notmatch '^issue-(\d+)-') { continue }
        $number = [int]$Matches[1]
        $issue = $OpenIssues[$number]
        if (-not $issue -or $byIssue.ContainsKey($number) -or (Test-HasOpenPr $issue)) { continue }
        # `git cherry` prefixes commits whose patch is already upstream with '-'.
        $unpublished = @(git -C $repo cherry origin/main $branch 2>$null | Where-Object { $_ -like '+*' }).Count
        if ($unpublished -eq 0) { continue }
        $prs = gh pr list --repo $repoSlug --state all --head $branch --json number --jq 'length'
        if ($LASTEXITCODE -ne 0 -or [int]$prs -gt 0) { continue }
        $byIssue[$number] = [pscustomobject]@{ Branch = $branch; Commits = $unpublished; Issue = $issue }
    }
    return $byIssue
}

function Get-StrandedFingerprint([string]$Branch, $Issue) {
    $sha = git -C $repo rev-parse --verify --quiet "refs/heads/$Branch"
    Get-Hash ('{0}|{1}' -f $sha, (Get-IssueFingerprint $Issue))
}

function New-Candidate([string]$Kind, [int]$Number, [int]$Rank, [string]$Fingerprint, [bool]$Light, [string]$Hint = '', [string]$Branch = '') {
    $lockKind = if ($Kind -eq 'pr') { 'pr' } else { 'issue' }
    $label = if ($Kind -eq 'pr') { "PR #$Number" } elseif ($Kind -eq 'stranded') { "stranded branch $Branch" } else { "issue #$Number" }
    [pscustomobject]@{
        Kind        = $Kind
        Number      = $Number
        LockName    = "$lockKind-$Number"
        Label       = $label
        Rank        = $Rank
        Fingerprint = $Fingerprint
        Light       = $Light
        Hint        = $Hint
        Branch      = $Branch
        Blocking    = 0
        Comments    = 0
    }
}

# Returns the latest completed push run of each workflow on main that failed. A newer
# run still in progress hides an older failure, because it may already carry the fix.
# A dispatched run of a push workflow counts as well: a fix outside the workflow's push
# path filter starts no push run, so an agent dispatches it on main to re-validate the
# fixed head. Workflows that never run on push (scheduled or manual jobs) stay out.
# Each event is a separate query, so comment-triggered and scheduled runs cannot push the
# push runs out of a shared --limit window.
function Get-MainRuns([string]$Event) {
    $runs = gh run list --repo $repoSlug --branch main --event $Event --limit 50 `
        --json databaseId,workflowName,status,conclusion,headSha,url,createdAt | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "gh run list --event $Event failed." }
    @($runs)
}

function Get-MainFailures {
    $pushRuns = Get-MainRuns 'push'
    $pushWorkflows = @($pushRuns | ForEach-Object workflowName | Select-Object -Unique)
    $dispatchRuns = @(Get-MainRuns 'workflow_dispatch' | Where-Object workflowName -in $pushWorkflows)
    @(@($pushRuns) + $dispatchRuns |
        Group-Object workflowName | ForEach-Object { $_.Group | Sort-Object createdAt | Select-Object -Last 1 } |
        Where-Object { $_.status -eq 'completed' -and $_.conclusion -in 'failure', 'timed_out', 'startup_failure' } |
        Sort-Object workflowName)
}

function Get-MainFingerprint([object[]]$Failures) {
    if ($Failures.Count -eq 0) { return $null }
    Get-Hash (($Failures | ForEach-Object { '{0}={1}' -f $_.workflowName, $_.databaseId }) -join '|')
}

# A red main fails every PR for the same reason, so fixing it comes before PR repairs.
function Get-MainCandidate {
    $failures = @(Get-MainFailures)
    if ($failures.Count -eq 0) { return $null }
    $list = ($failures | ForEach-Object { "$($_.workflowName) run $($_.databaseId) at $($_.headSha.Substring(0, 10)) ($($_.url))" }) -join '; '
    [pscustomobject]@{
        Kind        = 'main'
        Number      = 0
        LockName    = 'main-ci'
        Label       = 'red main'
        Rank        = -1
        Fingerprint = Get-MainFingerprint $failures
        Light       = $false
        Hint        = "The latest push CI on main failed: $list."
        Branch      = ''
        Blocking    = 0
        Comments    = 0
    }
}

function Get-Candidates {
    $candidates = [Collections.Generic.List[object]]::new()

    $main = Get-MainCandidate
    if ($main) { $candidates.Add($main) }

    $prs = gh pr list --repo $repoSlug --author '@me' --state open --limit 500 --json $prFields | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'gh pr list failed.' }
    $surveyStats['prs'] = @($prs).Count
    $ranked = foreach ($pr in $prs) {
        if ($pr.isDraft) { $surveyStats['pr-draft']++ }
        if (@($pr.labels | Where-Object { $_.name -in $excludedPrLabels }).Count -gt 0) { $surveyStats['pr-excluded']++; continue }
        $checks = @($pr.statusCheckRollup)
        $pending = @($checks | Where-Object { (Get-CheckState $_) -in 'PENDING', 'EXPECTED', 'QUEUED', 'IN_PROGRESS', 'WAITING', 'REQUESTED', '' })
        $failed = @($checks | Where-Object { (Get-CheckState $_) -in 'FAILURE', 'ERROR', 'TIMED_OUT', 'CANCELLED', 'ACTION_REQUIRED', 'STARTUP_FAILURE' })
        $conflicting = $pr.mergeable -eq 'CONFLICTING'
        # Running checks leave nothing to do yet; the next survey picks the PR up.
        if ($pending.Count -gt 0 -and -not $conflicting) { $surveyStats['pr-pending']++; continue }
        $rank = if (-not $pr.isDraft -and $pr.mergeStateStatus -eq 'CLEAN' -and $failed.Count -eq 0) { 0 }
        elseif ($conflicting) { 1 }
        elseif ($failed.Count -gt 0) { 2 }
        else { 3 }
        # A green, clean PR usually needs only the merge gate or a disposition reply.
        New-Candidate 'pr' ([int]$pr.number) $rank (Get-PrFingerprint $pr) ($rank -eq 0)
    }
    foreach ($c in @($ranked | Sort-Object Rank, Number)) { $candidates.Add($c) }

    $issues = @(Get-OpenIssues)
    $surveyStats['issues'] = $issues.Count
    $openIssues = @{}
    foreach ($issue in $issues) { $openIssues[[int]$issue.number] = $issue }
    $script:lastIssues = $issues
    $stranded = Get-StrandedBranches $openIssues
    foreach ($number in @($stranded.Keys | Sort-Object)) {
        $s = $stranded[$number]
        # A stranded branch waits under the same rules as its issue. The branch stays on
        # disk, and the issue's own candidate is skipped below while the branch exists.
        if (Test-HasLabel $s.Issue $excludedIssueLabels) { $surveyStats['issue-excluded']++; continue }
        if ($s.Issue.issueDependenciesSummary.blockedBy -gt 0) { $surveyStats['issue-blocked']++; continue }
        $hint = "Local branch $($s.Branch) holds $($s.Commits) commit(s) for issue #$number that are not on origin/main, and no PR exists for it. " +
            'Follow the stranded-branch procedure in the recovery guidance: publish the finished work as a PR, or record why it cannot ship.'
        $candidates.Add((New-Candidate 'stranded' $number 4 (Get-StrandedFingerprint $s.Branch $s.Issue) $false $hint $s.Branch))
    }

    $ranked = foreach ($issue in $issues) {
        $number = [int]$issue.number
        if ($stranded.ContainsKey($number)) { continue }
        $tracking = $issue.author.__typename -eq 'Bot' -or $issue.title -eq 'Dependency Dashboard'
        if ($tracking -or @($issue.assignees.nodes).Count -gt 0 -or (Test-HasLabel $issue $excludedIssueLabels)) { $surveyStats['issue-excluded']++; continue }
        # Work on an issue with an open closing PR happens on the PR.
        if (Test-HasOpenPr $issue) { $surveyStats['issue-has-pr']++; continue }
        # A blocked issue returns to the queue on its own when its blockers close.
        if ($issue.issueDependenciesSummary.blockedBy -gt 0) { $surveyStats['issue-blocked']++; continue }
        $children = $issue.subIssuesSummary
        $fingerprint = Get-IssueFingerprint $issue
        if ($children.total -gt $children.completed) {
            # A parent normally waits for its children. When no open child can be worked,
            # the parent is the only place left to move the tree.
            $open = @($issue.subIssues.nodes | Where-Object state -eq 'OPEN' | ForEach-Object { $openIssues[[int]$_.number] })
            $stuck = $open.Count -gt 0 -and $open.Count -eq ($children.total - $children.completed) -and
                @($open | Where-Object { $_ -and ((Test-HasLabel $_ $parkedLabels) -or $_.issueDependenciesSummary.blockedBy -gt 0) }).Count -eq $open.Count
            if (-not $stuck) { $surveyStats['issue-parent']++; continue }
            $list = ($open | ForEach-Object { "#$($_.number)" }) -join ', '
            $hint = "Every open sub-issue of #$number ($list) is parked or blocked. Check each child's blocker: unpark children whose condition is met, " +
                'flatten the chain into one claimable issue where it is deeper than parent and child, and implement work that does not need the blocker.'
            New-Candidate 'issue' $number 5 $fingerprint $false $hint
            continue
        }
        if (Test-HasLabel $issue @('in-progress')) {
            # A claim with no open PR is stale unless a worker holds its lock; the acquire
            # in Select-Item skips it while one does.
            $surveyStats['issue-claimed']++
            $hint = "Issue #$number carries an in-progress claim, but it has no open PR and no lock holder. Look for its branch or worktree and publish that work; " +
                'otherwise remove the stale claim and implement the issue.'
            New-Candidate 'issue' $number 7 $fingerprint $false $hint
            continue
        }
        if ($children.total -gt 0) {
            $hint = "All $($children.total) native sub-issues of #$number are closed. Check the parent's remaining acceptance criteria against origin/main, " +
                'then close it with evidence or implement what is left.'
            New-Candidate 'issue' $number 5 $fingerprint $true $hint
            continue
        }
        $candidate = New-Candidate 'issue' $number 6 $fingerprint $false
        $candidate.Blocking = [int]$issue.issueDependenciesSummary.blocking
        $candidate.Comments = [int]$issue.comments.totalCount
        $candidate
    }
    # Take the highest priority tier first, pipeline health first within a tier, since
    # every later PR pays for it. Then unblock the most other issues, then prefer less
    # discussion: such issues are usually smaller and clearer.
    $ordered = $ranked | Sort-Object @{ Expression = { Get-IssuePriority $openIssues[$_.Number] } },
        @{ Expression = { -not (Test-HasLabel $openIssues[$_.Number] @('ci')) } },
        Rank, @{ Expression = 'Blocking'; Descending = $true }, Comments, Number
    foreach ($c in @($ordered)) { $candidates.Add($c) }

    # A deferral that named an open blocker promotes it: working the blocker is the only
    # way the blocked item moves. Red main still comes first.
    $promotedFirst = @($candidates | Where-Object { $_.Kind -ne 'main' -and $script:promoted.Contains($_.Number) })
    if ($promotedFirst.Count -eq 0) { return $candidates }
    foreach ($c in $promotedFirst) { $c.Hint = (("A previous unit deferred another item because #$($c.Number) blocks it. " +
                'Resolve this item so the blocked work can move.') + $(if ($c.Hint) { " $($c.Hint)" } else { '' })) }
    return @(@($candidates | Where-Object Kind -eq 'main') + $promotedFirst +
        @($candidates | Where-Object { $_.Kind -ne 'main' -and -not $script:promoted.Contains($_.Number) }))
}

function Get-CurrentFingerprint($Item) {
    try {
        switch ($Item.Kind) {
            'pr' {
                $pr = gh pr view $Item.Number --repo $repoSlug --json $prFields | ConvertFrom-Json
                if ($LASTEXITCODE -ne 0) { return $null }
                return Get-PrFingerprint $pr
            }
            'stranded' { return Get-StrandedFingerprint $Item.Branch (Get-Issue $Item.Number) }
            # A main that turned green needs no deferral record.
            'main' { return Get-MainFingerprint @(Get-MainFailures) }
            default { return Get-IssueFingerprint (Get-Issue $Item.Number) }
        }
    }
    catch { return $null }
}

# Returns lock name -> fingerprint for every recorded deferral among the given items.
function Get-Deferrals([object[]]$Items) {
    $deferrals = @{}
    if ($Items.Count -eq 0) { return $deferrals }
    $r = Invoke-Locks @('deferred', '-LockName', (($Items | ForEach-Object LockName) -join ','))
    if ($r.ExitCode -ne 0) { throw "deferral lookup failed (exit $($r.ExitCode)): $($r.Output)" }
    foreach ($line in ($r.Output -split "`n")) {
        $name, $fingerprint = $line.Trim() -split '=', 2
        if ($fingerprint) { $deferrals[$name] = $fingerprint }
    }
    return $deferrals
}

# A recorded deferral is `<fingerprint>` followed by optional `|key=value` fields:
# `until=<unix seconds>` ends the deferral, `n=<count>` counts consecutive deferrals of
# the same fingerprint, `refs=<n>:<state>,...` records blocker states, and `run=<id>`
# names the workflow run a `waiting` item waits for. The Redis record outlives `until`,
# so a repeat deferral can still read the count.
function Split-Deferral([string]$Recorded) {
    $fingerprint, $fields = $Recorded -split '\|'
    $values = @{}
    foreach ($field in @($fields)) {
        $key, $value = $field -split '=', 2
        if ($key) { $values[$key] = $value }
    }
    $refs = $values['refs']
    $numbers = @(if ($refs) { $refs -split ',' | ForEach-Object { [int]($_ -split ':', 2)[0] } })
    [pscustomobject]@{
        Fingerprint = $fingerprint
        Refs        = $refs
        Numbers     = $numbers
        Until       = if ($values['until']) { [long]$values['until'] } else { [long]::MaxValue }
        Count       = if ($values['n']) { [int]$values['n'] } else { 1 }
        Run         = $values['run']
    }
}

# Caches run id -> completed for one survey, so several items waiting on one run cost one call.
$runCompleted = @{}

function Test-RunCompleted([string]$RunId) {
    if (-not $runCompleted.ContainsKey($RunId)) {
        $status = gh run view $RunId --repo $repoSlug --json status --jq .status 2>$null
        # A run that cannot be read cannot be waited on; let the item return.
        $runCompleted[$RunId] = $LASTEXITCODE -ne 0 -or ([string]$status).Trim() -eq 'completed'
    }
    return $runCompleted[$RunId]
}

function Test-Deferred($Item, [hashtable]$Deferrals, $States) {
    if (-not $Item.Fingerprint -or -not $Deferrals[$Item.LockName]) { return $false }
    $deferral = Split-Deferral $Deferrals[$Item.LockName]
    if ($deferral.Fingerprint -ne $Item.Fingerprint) { return $false }
    # A waiting item returns when its run completes, or when the deferral expires.
    if ($deferral.Run -and (Test-RunCompleted $deferral.Run)) { return $false }
    # Without fresh blocker states, fall back to the item's own fingerprint.
    $refsKnown = $deferral.Refs -and $null -ne $States
    if ($refsKnown -and (Format-References $deferral.Numbers $States) -ne $deferral.Refs) { return $false }
    # A parked blocker is not worked by any unit, so the expiry would only buy a repeat
    # deferral. The item returns when the blocker closes or loses its parking label.
    if ($refsKnown -and $deferral.Refs -match ':PARKED(,|$)') { return $true }
    return [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() -lt $deferral.Until
}

# Returns the claimed item, $null when nothing is actionable, or throws when the lock
# service fails mid-survey.
function Select-Item {
    Reset-SurveyStats
    $runCompleted.Clear()
    $candidates = @(Get-Candidates)
    $deferrals = Get-Deferrals $candidates
    $states = Get-ReferenceStates @($deferrals.Values | ForEach-Object { (Split-Deferral $_).Numbers })
    foreach ($item in $candidates) {
        if (Test-Deferred $item $deferrals $states) { $surveyStats["$($item.Kind)-deferred"]++; continue }
        $r = Invoke-Locks @('acquire', '-LockName', $item.LockName)
        if ($r.ExitCode -eq 3) { $surveyStats["$($item.Kind)-locked"]++; continue }
        if ($r.ExitCode -ne 0) { throw "lock acquire for $($item.LockName) failed (exit $($r.ExitCode)): $($r.Output)" }
        $null = $script:promoted.Remove($item.Number)
        $item | Add-Member -NotePropertyName PreviousReason -NotePropertyValue (Get-DeferralNote $item.LockName) -Force
        return $item
    }
    return $null
}

# The last deferral reason for an item, stored next to its deferral record, so the next
# unit can start from that blocker instead of rediscovering it.
function Get-DeferralNote([string]$LockName) {
    try {
        $r = Invoke-Locks @('deferred', '-LockName', "note-$LockName")
        if ($r.ExitCode -ne 0 -or -not $r.Output) { return '' }
        return (($r.Output -split "`n")[0] -split '=', 2)[1]
    }
    catch { return '' }
}

function Set-DeferralNote([string]$LockName, [string]$Reason, [long]$Seconds) {
    # The note is plain text in a Redis value; keep it on one line and bounded.
    $text = ($Reason -replace '\s+', ' ' -replace '"', "'").Trim()
    if ($text.Length -gt 400) { $text = $text.Substring(0, 397) + '...' }
    $null = Invoke-Locks @('defer', '-LockName', "note-$LockName", '-Fingerprint', $text, '-DeferSeconds', [Math]::Min(604800, [Math]::Max(60, $Seconds)))
}

# Builds a triage unit: open issues and parked PRs that no unit audited in -AuditDays,
# parked and blocked items first because nothing else looks at them.
function Get-TriageItem {
    if (-not $script:lastIssues) { return $null }
    $prs = @(gh pr list --repo $repoSlug --author '@me' --state open --limit 500 --json number,labels | ConvertFrom-Json |
            Where-Object { @($_.labels | Where-Object { $_.name -in $parkedLabels }).Count -gt 0 })
    $pool = @(
        foreach ($issue in $script:lastIssues) {
            if ($issue.author.__typename -eq 'Bot' -or $issue.title -eq 'Dependency Dashboard') { continue }
            $order = if (Test-HasLabel $issue $parkedLabels) { 0 }
            elseif ($issue.issueDependenciesSummary.blockedBy -gt 0 -or $issue.subIssuesSummary.total -gt $issue.subIssuesSummary.completed) { 1 }
            elseif (Test-HasLabel $issue @('in-progress')) { 2 }
            else { 3 }
            [pscustomobject]@{ Number = [int]$issue.number; Kind = 'issue'; Order = $order }
        }
        foreach ($pr in $prs) { [pscustomobject]@{ Number = [int]$pr.number; Kind = 'pr'; Order = 0 } }
    )
    if ($pool.Count -eq 0) { return $null }
    $audited = @{}
    $r = Invoke-Locks @('deferred', '-LockName', (($pool | ForEach-Object { "audit-$($_.Number)" }) -join ','))
    if ($r.ExitCode -ne 0) { return $null }
    foreach ($line in ($r.Output -split "`n")) { if ($line.Trim()) { $audited[($line.Trim() -split '=', 2)[0]] = $true } }
    $due = @($pool | Where-Object { -not $audited["audit-$($_.Number)"] } | Sort-Object Order, Number | Select-Object -First $TriageBatch)
    if ($due.Count -eq 0) { return $null }
    $r = Invoke-Locks @('acquire', '-LockName', 'triage')
    if ($r.ExitCode -ne 0) { return $null }
    [pscustomobject]@{
        Kind        = 'triage'
        Number      = 0
        LockName    = 'triage'
        Label       = "triage of $($due.Count) item(s)"
        Rank        = 8
        Fingerprint = $null
        Light       = $true
        Items       = $due
        Queueable   = Get-QueueableCount
    }
}

# Issues that a unit can claim now: not parked, claimed, blocked, or already on a PR.
function Get-QueueableCount {
    @($script:lastIssues | Where-Object {
            -not (Test-HasLabel $_ ($excludedIssueLabels + 'in-progress')) -and $_.issueDependenciesSummary.blockedBy -eq 0 -and -not (Test-HasOpenPr $_)
        }).Count
}

# Builds a discovery unit, which files new claimable issues, when the last one ran more
# than -DiscoveryHours ago. The `discovery-ran` record spaces them out across workers.
function Get-DiscoveryItem {
    if ($DiscoveryHours -eq 0 -or -not $script:lastIssues) { return $null }
    $r = Invoke-Locks @('deferred', '-LockName', 'discovery-ran')
    if ($r.ExitCode -ne 0 -or ([string]$r.Output).Trim()) { return $null }
    $r = Invoke-Locks @('acquire', '-LockName', 'discovery')
    if ($r.ExitCode -ne 0) { return $null }
    [pscustomobject]@{
        Kind        = 'discovery'
        Number      = 0
        LockName    = 'discovery'
        Label       = 'discovery'
        Rank        = 9
        Fingerprint = $null
        Light       = $false
        Queueable   = Get-QueueableCount
    }
}

# Lists the items that only a person can move, so an idle loop says what it waits for.
function Write-HumanDigest {
    $issues = @($script:lastIssues | Where-Object { Test-HasLabel $_ @('needs-human', 'question') } | ForEach-Object { "#$($_.number)" })
    $prs = @(gh pr list --repo $repoSlug --state open --label needs-human --json number --jq '.[].number' 2>$null | ForEach-Object { "PR #$_" })
    $all = @($issues + $prs)
    if ($all.Count -gt 0) { Write-Host "issue-pr-loop: waiting on a person: $($all -join ', ')" }
}

# A preserved worktree with new source may hold unpublished work worth recovering.
function Get-RecoveryItem([string[]]$SweepOutput) {
    $lines = @($SweepOutput | Where-Object { $_ -match '^Preserving worktree .*\(new source' } | Sort-Object)
    if ($lines.Count -eq 0) { return $null }
    $item = [pscustomobject]@{
        Kind        = 'recovery'
        Number      = 0
        LockName    = 'recovery-survey'
        Label       = 'worktree recovery'
        Rank        = 5
        Fingerprint = Get-Hash ($lines -join "`n")
        Light       = $false
        Lines       = $lines
    }
    if (Test-Deferred $item (Get-Deferrals @($item))) { return $null }
    return $item
}

function Get-Prompt($Item) {
    $head = "Read $skill and follow the issue-pr-loop skill in single-unit mode for an assigned item."
    # An earlier deferral reason lets this unit start from the blocker it named.
    if ($Item.PSObject.Properties['PreviousReason'] -and $Item.PreviousReason) {
        $head += " An earlier unit deferred this item with the reason: `"$($Item.PreviousReason)`". Check whether that blocker still holds, and try every route in Resolve, do not defer before you defer it again."
    }
    switch ($Item.Kind) {
        'pr' {
            $hint = if ($Item.Hint) { " $($Item.Hint)" } else { '' }
            return "$head The loop driver already ran the worktree sweep and the survey, and it holds lock pr-$($Item.Number) for you through `$env:$ownerVariable. Skip the sweep, the survey and the acquire.$hint Start from PR #$($Item.Number); act on linked items under their own locks when the skill allows it. Release every lock and print the RESULT line."
        }
        'discovery' {
            return "$head This is a discovery unit. The loop driver holds lock discovery for you through `$env:$ownerVariable. Skip the sweep and the survey. Nothing is actionable and every open item was audited recently, so follow the discovery procedure in the skill. The queue has $($Item.Queueable) issue(s) that a unit can claim now. Release every lock and print the RESULT line."
        }
        'triage' {
            $list = ($Item.Items | ForEach-Object { if ($_.Kind -eq 'pr') { "PR #$($_.Number)" } else { "#$($_.Number)" } }) -join ', '
            return "$head This is a triage unit. The loop driver holds lock triage for you through `$env:$ownerVariable. Skip the sweep and the survey. Follow the triage procedure in the skill for these items: $list. The queue has $($Item.Queueable) issue(s) that a unit can claim now. Release every lock and print the RESULT line."
        }
        'issue' {
            $hint = if ($Item.Hint) { " $($Item.Hint)" } else { '' }
            return "$head The loop driver already ran the worktree sweep and the survey, and it holds lock issue-$($Item.Number) for you through `$env:$ownerVariable. Skip the sweep, the survey and the acquire.$hint Re-check and claim issue #$($Item.Number) and implement it; act on linked items under their own locks when the skill allows it. Release every lock and print the RESULT line."
        }
        'stranded' {
            return "$head The loop driver already ran the worktree sweep and the survey, and it holds lock issue-$($Item.Number) for you through `$env:$ownerVariable. Skip the sweep, the survey and the acquire. $($Item.Hint) Release the lock and print the RESULT line."
        }
        'main' {
            return "$head The loop driver already ran the worktree sweep and the survey, and it holds lock main-ci for you through `$env:$ownerVariable. Skip the sweep and the survey. $($Item.Hint) Follow the red-main procedure in the skill, release every lock, and print the RESULT line."
        }
        default {
            $list = $Item.Lines -join '; '
            return "$head The loop driver found no actionable PR or issue. The worktree sweep preserved these worktrees with new source: $list. Run the recovery procedure for the first one you can lock, release every lock, and print the RESULT line. Print RESULT: queue-empty when none needs recovery."
        }
    }
}

# Runs one agent unit and returns its exit code and final answer text.
function Invoke-Agent([string]$Prompt, [string]$Effort, [string]$Log) {
    $finalText = $null
    if ($selected -eq 'codex') {
        $effortArgs = if ($Effort) { @('-c', "model_reasoning_effort=$Effort") } else { @() }
        # Out-Host keeps Codex progress on the console instead of in this function's output.
        codex exec --dangerously-bypass-approvals-and-sandbox @effortArgs -C $repo -o $Log $Prompt | Out-Host
        $exitCode = $LASTEXITCODE
        if (Test-Path -LiteralPath $Log) { $finalText = Get-Content -LiteralPath $Log -Raw }
    }
    else {
        $effortArgs = if ($Effort) { @('--effort', $Effort) } else { @() }
        Push-Location $repo
        try {
            # The empty pipe closes stdin; otherwise claude waits 3 s for piped input.
            $finalText = @() | claude -p $Prompt --dangerously-skip-permissions @effortArgs --output-format stream-json --verbose 2>&1 |
                ForEach-Object { Write-ClaudeEvent -Line "$_" -Log $Log }
            $exitCode = $LASTEXITCODE
        }
        finally { Pop-Location }
    }
    return [pscustomobject]@{ ExitCode = $exitCode; Text = ($finalText -join "`n") }
}

function Wait-Backoff([string]$Reason) {
    $seconds = [Math]::Max(60, [Math]::Min($IdleSeconds, 300))
    Write-Host "issue-pr-loop: $Reason; retrying in $seconds s"
    Start-Sleep -Seconds $seconds
}

# Codex leaks a full marketplace clone into .staging on every failed auto-upgrade
# (openai/codex#38770), and every `codex exec` unit can start one. An upgrade times out
# after 30 s, so a directory older than -MinutesOld belongs to no running upgrade. Git
# marks pack files read-only, which blocks Remove-Item on Windows until it is cleared.
function Remove-CodexMarketplaceStaging([int]$MinutesOld = 10) {
    $codexHome = if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $HOME '.codex' }
    $staging = Join-Path $codexHome '.tmp/marketplaces/.staging'
    if (-not (Test-Path -LiteralPath $staging)) { return }
    $cutoff = (Get-Date).AddMinutes(-$MinutesOld)
    $stale = @(Get-ChildItem -LiteralPath $staging -Directory -Filter 'marketplace-upgrade-*' -Force -ErrorAction SilentlyContinue |
        Where-Object LastWriteTime -lt $cutoff)
    $removed = 0
    foreach ($dir in $stale) {
        try {
            Get-ChildItem -LiteralPath $dir.FullName -Recurse -File -Force -ErrorAction SilentlyContinue |
                Where-Object IsReadOnly |
                ForEach-Object { $_.IsReadOnly = $false }
            Remove-Item -LiteralPath $dir.FullName -Recurse -Force -ErrorAction Stop
            $removed++
        }
        catch { Write-Host "issue-pr-loop: could not remove $($dir.FullName): $($_.Exception.Message)" }
    }
    if ($removed -gt 0) { Write-Host "issue-pr-loop: removed $removed stale Codex marketplace staging dir(s)" }
}

# One JSON line per unit. The log directory is shared by every repository's loop, so each
# record names its repository.
function Write-UnitRecord([string]$RunId, $Item, [string]$Result, [string]$Category, [int]$ExitCode, [double]$Seconds) {
    $outcome = if ($Result -match '^([a-z-]+)') { $Matches[1] } else { 'no-result' }
    $record = [ordered]@{
        runId    = $RunId
        repo     = $repoSlug
        agent    = $selected
        kind     = $Item.Kind
        number   = $Item.Number
        outcome  = $outcome
        category = $Category
        result   = $Result
        exitCode = $ExitCode
        seconds  = [Math]::Round($Seconds)
        endedAt  = (Get-Date).ToUniversalTime().ToString('o')
    }
    try { Add-Content -LiteralPath $unitsFile -Value ($record | ConvertTo-Json -Compress) }
    catch { Write-Host "issue-pr-loop: could not append to ${unitsFile}: $($_.Exception.Message)" }
}

Write-Host "issue-pr-loop: agent=$selected repo=$repo"
Write-Host "issue-pr-loop: logs in $LogDirectory"
Write-Host "issue-pr-loop: create $stopFile to stop after the current unit"

# A nested `claude -p` refuses to start when it inherits CLAUDECODE from a parent session.
Remove-Item Env:CLAUDECODE -ErrorAction SilentlyContinue

$units = 0
$sinceTriage = 0
# Item numbers that a deferral named as open blockers; the next survey ranks them first.
$script:promoted = [Collections.Generic.HashSet[int]]::new()
$script:lastIssues = $null
while (-not (Test-Path -LiteralPath $stopFile)) {
    if ($MaxUnits -gt 0 -and $units -ge $MaxUnits) { break }

    $runId = '{0:yyyyMMdd-HHmmss}-{1}' -f (Get-Date), ([guid]::NewGuid().ToString('N').Substring(0, 8))
    $log = Join-Path $LogDirectory "$runId.log"
    Set-Item -Path "Env:$ownerVariable" -Value "issue-pr-loop-$runId"
    Remove-CodexMarketplaceStaging

    $ping = Invoke-Locks @('ping') -TimeoutSeconds 90
    if ($ping.ExitCode -ne 0) {
        Wait-Backoff "lock Redis unavailable (exit $($ping.ExitCode): $($ping.Output))"
        continue
    }

    $sweep = @(& pwsh -NoProfile -WorkingDirectory $repo -File $sweepScript 2>&1 | ForEach-Object { $_.ToString() })
    $sweep | Set-Content -LiteralPath (Join-Path $LogDirectory "$runId.sweep.log")

    try {
        # A periodic triage unit keeps issue state current while work is queued. It uses
        # the previous survey's issue list, which is at most one unit old.
        $item = if ($TriageEvery -gt 0 -and $sinceTriage -ge $TriageEvery) { Get-TriageItem } else { $null }
        if (-not $item) { $item = Select-Item }
        if (-not $item) { $item = Get-RecoveryItem $sweep }
        # Idle time goes to triage instead of sleep.
        if (-not $item) { $item = Get-TriageItem }
        # With every item audited, look for new work before sleeping.
        if (-not $item) { $item = Get-DiscoveryItem }
    }
    catch {
        Wait-Backoff "survey failed: $($_.Exception.Message)"
        continue
    }
    if (-not $item) {
        Write-Host "issue-pr-loop: nothing actionable ($(Format-SurveyStats))"
        Write-HumanDigest
        if ($IdleSeconds -gt 0) {
            Write-Host "issue-pr-loop: next survey in $IdleSeconds s"
            Start-Sleep -Seconds $IdleSeconds
        }
        continue
    }

    if ($DryRun) {
        Write-Host "issue-pr-loop: dry run would start $($item.Label) (light=$($item.Light))"
        Write-Host (Get-Prompt $item)
        if ($item.Kind -ne 'recovery') { $null = Invoke-Locks @('release', '-LockName', $item.LockName) }
        break
    }

    $units++
    if ($item.Kind -in 'triage', 'discovery') { $sinceTriage = 0 } else { $sinceTriage++ }
    $effort = if ($item.Light) { $LightEffort } else { $HeavyEffort }
    Write-Host "issue-pr-loop: unit $units ($runId) $($item.Label)$(if ($effort) { " effort=$effort" })"
    $started = Get-Date
    $run = Invoke-Agent (Get-Prompt $item) $effort $log
    $exitCode = $run.ExitCode
    $result = [regex]::Matches([string]$run.Text, '(?m)^\s*RESULT:\s*(.+)$') |
        Select-Object -Last 1 |
        ForEach-Object { $_.Groups[1].Value.Trim() }

    if ($item.Kind -ne 'recovery') {
        $status = Invoke-Locks @('status', '-LockName', $item.LockName)
        if ($status.Output -eq 'HELD-BY-ME') { $null = Invoke-Locks @('release', '-LockName', $item.LockName) }
    }

    # A triage unit audits its batch whatever it changed, so the next triage takes new items.
    if ($item.Kind -eq 'triage' -and $result) {
        foreach ($audited in $item.Items) {
            # Parked items come back sooner: their condition often clears without a GitHub change.
            $auditSeconds = if ($audited.Order -eq 0) { $ParkedAuditHours * 3600 } else { $AuditDays * 86400 }
            $null = Invoke-Locks @('defer', '-LockName', "audit-$($audited.Number)", '-Fingerprint', [string][DateTimeOffset]::UtcNow.ToUnixTimeSeconds(),
                '-DeferSeconds', $auditSeconds)
        }
    }
    # A discovery unit ran, whatever it found; the next one waits -DiscoveryHours. A crash
    # still waits an hour, so a broken unit cannot spin.
    if ($item.Kind -eq 'discovery') {
        $wait = if ($result) { $DiscoveryHours * 3600 } else { 3600 }
        $null = Invoke-Locks @('defer', '-LockName', 'discovery-ran', '-Fingerprint', [string][DateTimeOffset]::UtcNow.ToUnixTimeSeconds(), '-DeferSeconds', $wait)
    }

    # `deferred #N [category]: reason`. A deferral without an allowed category names no
    # blocker that an agent cannot remove, so the item comes back soon.
    $category = if ($result -match '^deferred\s+(#\d+|main)\s*\[([a-z-]+)\]') { $Matches[2] } else { '' }
    $uncategorized = $result -match '^deferred\b' -and $category -notin $deferCategories
    $waitRun = if ($result -match '^waiting\b' -and $result -match '\brun\s+(\d{6,})') { $Matches[1] } else { '' }

    # Remember an unchanged blocker so later units do not re-examine it.
    if ($item.Kind -notin 'triage', 'discovery' -and (-not $result -or $result -match '^(deferred|failed|waiting)\b' -or ($item.Kind -eq 'recovery' -and $result -eq 'queue-empty'))) {
        $fingerprint = if ($item.Kind -eq 'recovery') { $item.Fingerprint } else { Get-CurrentFingerprint $item }
        if ($fingerprint) {
            # Each repeat deferral of an unchanged item doubles its expiry. A crash says
            # nothing about the item, so it neither counts nor waits long. A wait ends when
            # its run completes, so it neither counts nor needs a short expiry.
            $previous = try { (Get-Deferrals @($item))[$item.LockName] } catch { $null }
            $previous = if ($previous) { Split-Deferral $previous } else { $null }
            $count = if ($previous -and $previous.Fingerprint -eq $fingerprint) { $previous.Count } else { 0 }
            if ($result -and -not $waitRun) { $count++ }
            $seconds = if (-not $result) { [Math]::Min($DeferSeconds, 3600) }
            elseif ($waitRun) { $MaxDeferSeconds }
            elseif ($uncategorized) { [Math]::Min($DeferSeconds, 3600) }
            else { [Math]::Min($MaxDeferSeconds, $DeferSeconds * [Math]::Pow(2, [Math]::Min($count - 1, 20))) }
            $until = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() + [long]$seconds
            $record = '{0}|until={1}|n={2}' -f $fingerprint, $until, [Math]::Max($count, 1)
            if ($waitRun) { $record += "|run=$waitRun" }
            $refs = if ($item.Kind -ne 'recovery') { Get-ReasonReferences $result $item.Number } else { @() }
            if ($refs.Count -gt 0) {
                $refStates = Get-ReferenceStates $refs
                if ($refStates) {
                    $record += '|refs=' + (Format-References $refs $refStates)
                    # Work the open blockers next. Parked ones wait for a person.
                    foreach ($n in $refs) { if ($refStates[$n] -eq 'OPEN') { $null = $script:promoted.Add($n) } }
                }
            }
            # The record outlives `until` so the next deferral can read the count.
            $ttl = [Math]::Min(604800, [long]$seconds + $MaxDeferSeconds)
            $null = Invoke-Locks @('defer', '-LockName', $item.LockName, '-Fingerprint', $record, '-DeferSeconds', $ttl)
            if ($result) { Set-DeferralNote $item.LockName $result $ttl }
            if ($waitRun) { Write-Host "issue-pr-loop: $($item.Label) waits for run $waitRun" }
            elseif ($uncategorized) { Write-Host "issue-pr-loop: $($item.Label) deferred without an allowed category ($($deferCategories -join '|')); next look in 1 h" }
            elseif ($result) { Write-Host ('issue-pr-loop: {0} deferred {1} time(s) unchanged; next look in {2:N1} h' -f $item.Label, $count, ($seconds / 3600)) }
        }
    }

    Write-UnitRecord $runId $item $result $category $exitCode ((Get-Date) - $started).TotalSeconds

    if (-not $result) {
        # A crashed or misconfigured agent must not spin; back off before retrying.
        Write-Host "issue-pr-loop: unit $units ($runId) no RESULT line (agent exit $exitCode), see $log"
        Wait-Backoff 'agent ended without a result'
        continue
    }
    Write-Host "issue-pr-loop: unit $units ($runId) $result"
}

# Clear the stop request so the next start runs.
Remove-Item -LiteralPath $stopFile -ErrorAction SilentlyContinue
Remove-Item -Path "Env:$ownerVariable" -ErrorAction SilentlyContinue
Write-Host "issue-pr-loop: stopped after $units unit(s)"
