$ErrorActionPreference = 'Stop'
$sweep = Join-Path $PSScriptRoot 'Remove-MergedWorktrees.ps1'
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = [IO.Path]::GetFullPath((Join-Path $tempBase "worktree-sweep-$([Guid]::NewGuid().ToString('N'))"))
if (-not $testRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Test directory is outside the temporary directory: $testRoot"
}
$repo = Join-Path $testRoot 'repo'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

# Exercise the real sweep and Git filesystem operations without GitHub or network.
function gh {
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'pr' -and $args[1] -eq 'list') {
        if ($args -contains 'merged') { return $mergedJson }
        if ($failOpenQuery) { $global:LASTEXITCODE = 1; return }
        return $openJson
    }
    if ($args[0] -eq 'api') {
        if ($args[1] -match '/issues/(\d+)$') { return $(if ($Matches[1] -in $closedItems) { 'closed' } else { 'open' }) }
        return '[]'
    }
    throw "Unexpected gh call: $args"
}

try {
    New-Item -ItemType Directory -Path $repo | Out-Null
    git -C $repo init -b main --quiet
    git -C $repo config user.name 'Sweep Test'
    git -C $repo config user.email 'sweep@example.invalid'
    Set-Content -LiteralPath (Join-Path $repo 'README.md') -Value 'base'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../.gitignore') -Destination (Join-Path $repo '.gitignore')
    git -C $repo add README.md .gitignore
    git -C $repo commit --quiet -m 'base'
    $baseHead = git -C $repo rev-parse HEAD
    git -C $repo update-ref refs/remotes/origin/main $baseHead

    $mergedWorktree = Join-Path $testRoot 'merged'
    $dirtyWorktree = Join-Path $testRoot 'dirty'
    $divergentWorktree = Join-Path $testRoot 'divergent'
    $openWorktree = Join-Path $testRoot 'open'
    foreach ($name in @('merged', 'dirty', 'divergent', 'open')) {
        git -C $repo worktree add --quiet -b $name (Join-Path $testRoot $name)
    }
    Set-Content -LiteralPath (Join-Path $mergedWorktree 'build.log') -Value 'evidence'
    Set-Content -LiteralPath (Join-Path $dirtyWorktree 'NewFeature.cs') -Value 'untracked source'
    Set-Content -LiteralPath (Join-Path $divergentWorktree 'README.md') -Value 'unpublished'
    git -C $divergentWorktree commit --quiet -am 'unpublished'
    Set-Content -LiteralPath (Join-Path $openWorktree 'README.md') -Value 'open PR'
    git -C $openWorktree commit --quiet -am 'open PR'
    $openHead = git -C $openWorktree rev-parse HEAD

    # Closed work items whose checkouts no merge tier recognizes.
    $closedItems = @('500', '501', '503', '504')
    # A review branch with a local commit after its recorded merge, plus uncommitted work.
    $review = Join-Path $testRoot 'pr-500-review'
    git -C $repo worktree add --quiet -b pr-500-review $review $baseHead
    Set-Content -LiteralPath (Join-Path $review 'README.md') -Value 'local review commit'
    git -C $review commit --quiet -am 'local review commit'
    $reviewHead = git -C $review rev-parse HEAD
    Set-Content -LiteralPath (Join-Path $review 'README.md') -Value 'uncommitted edit'
    Set-Content -LiteralPath (Join-Path $review 'Draft.cs') -Value 'untracked draft'
    # A detached checkout identified only by its lock marker.
    $closedDetached = Join-Path $testRoot 'closed-detached'
    git -C $repo worktree add --quiet --detach $closedDetached $baseHead
    git -C $repo config --local extensions.worktreeConfig true
    git -C $closedDetached config --worktree agent.lockName issue-501
    git -C $closedDetached config --worktree agent.lockWorktree $closedDetached
    Set-Content -LiteralPath (Join-Path $closedDetached 'README.md') -Value 'detached commit'
    git -C $closedDetached commit --quiet -am 'detached commit'
    $detachedHead = git -C $closedDetached rev-parse HEAD
    $openItem = Join-Path $testRoot 'pr-502-open'
    git -C $repo worktree add --quiet -b pr-502-open $openItem $baseHead
    # An ignored secret cannot be stashed, so it keeps the checkout.
    $ignoredItem = Join-Path $testRoot 'pr-503-ignored'
    git -C $repo worktree add --quiet -b pr-503-ignored $ignoredItem $baseHead
    Set-Content -LiteralPath (Join-Path $ignoredItem 'signing.pfx') -Value 'secret'
    $heldItem = Join-Path $testRoot 'pr-504-held'
    git -C $repo worktree add --quiet -b pr-504-held $heldItem $baseHead
    # Live Redis semantics are tested separately; pr-504 has a live owner.
    New-Item -ItemType Directory -Path (Join-Path $repo 'scripts') | Out-Null
    Set-Content -LiteralPath (Join-Path $repo 'scripts/AgentLocks.ps1') -Value @'
param($Verb, [string]$LockName, [string]$OwnerId)
if (-not $OwnerId) { exit 2 }
if ($LockName -eq 'pr-504') { if ($Verb -eq 'status') { 'HELD'; exit 0 }; exit 3 }
if ($Verb -eq 'status') { 'FREE' }
exit 0
'@

    $mergedJson = @(
        foreach ($name in @('merged', 'dirty', 'divergent', 'open', 'pr-500-review')) {
            @{ headRefName = $name; headRefOid = $baseHead }
        }
    ) | ConvertTo-Json
    $openJson = @(@{ headRefName = 'open'; headRefOid = $openHead }) | ConvertTo-Json -AsArray

    Push-Location $repo
    try {
        $failOpenQuery = $true
        & $sweep -Repo fixture/repo
        Assert-True (Test-Path -LiteralPath $mergedWorktree) 'Failed open-PR query allowed removal.'
        $failOpenQuery = $false

        $preview = (& $sweep -Repo fixture/repo -WhatIf 6>&1) -join "`n"
        Assert-True ($preview -match 'WOULD remove .*[\\/]merged') 'Preview did not identify merged worktree.'
        Assert-True ($preview -match 'NewFeature.cs') 'Preview did not report untracked source blocker.'
        Assert-True ($preview -notmatch 'WOULD remove .*[\\/]dirty') 'Preview claimed a dirty worktree would be removed.'
        Assert-True (Test-Path -LiteralPath $mergedWorktree) 'Preview removed worktree.'
        Assert-True ($preview -match 'WOULD remove .*pr-500-review -- \(closed work item pr-500\) \(saving 2 ') 'Preview did not report retained work.'
        Assert-True ($preview -match 'WOULD remove .*closed-detached -- \(closed work item issue-501\)') 'Preview did not resolve the marker identity.'
        Assert-True ($preview -match 'keeping worktree of closed pr-504') 'Preview did not keep a held item.'
        Assert-True (Test-Path -LiteralPath (Join-Path $review 'Draft.cs')) 'Preview saved or removed closed-item work.'
        Assert-True (@(git -C $repo for-each-ref 'refs/heads/retained-worktrees/').Count -eq 0) 'Preview created a retained branch.'

        & $sweep -Repo fixture/repo
        Assert-True (-not (Test-Path -LiteralPath $mergedWorktree)) 'Merged worktree was not removed autonomously.'
        Assert-True (Test-Path -LiteralPath $dirtyWorktree) 'Dirty worktree was removed.'
        Assert-True (Test-Path -LiteralPath $divergentWorktree) 'Divergent local commit was removed.'
        Assert-True (Test-Path -LiteralPath $openWorktree) 'Open PR worktree was removed.'
        git -C $repo show-ref --verify --quiet refs/heads/merged
        Assert-True ($LASTEXITCODE -eq 0) 'Merged branch was deleted without an explicit request.'
        git -C $repo show-ref --verify --quiet refs/heads/divergent
        Assert-True ($LASTEXITCODE -eq 0) 'Divergent branch was deleted.'

        Assert-True (-not (Test-Path -LiteralPath $review)) 'Closed PR review checkout was not removed.'
        Assert-True ((git -C $repo rev-parse refs/heads/pr-500-review) -eq $reviewHead) 'Closed item branch was not kept.'
        $wip = @(git -C $repo for-each-ref --format='%(refname:short)' 'refs/heads/retained-worktrees/pr-500-wip-*')
        Assert-True ($wip.Count -eq 1) 'Uncommitted work was not saved to a retained branch.'
        Assert-True ((git -C $repo show "$($wip[0]):README.md") -eq 'uncommitted edit') 'Saved tracked change is wrong.'
        Assert-True ((git -C $repo show "$($wip[0])^3:Draft.cs") -eq 'untracked draft') 'Saved untracked file is wrong.'
        Assert-True (@(git -C $repo stash list).Count -eq 0) 'Saving work left a stash entry.'
        Assert-True (-not (Test-Path -LiteralPath $closedDetached)) 'Closed detached checkout was not removed.'
        Assert-True ((git -C $repo rev-parse "refs/heads/retained-worktrees/$detachedHead") -eq $detachedHead) 'Detached commit was not retained.'
        Assert-True (Test-Path -LiteralPath $openItem) 'Open item checkout was removed.'
        Assert-True (Test-Path -LiteralPath (Join-Path $ignoredItem 'signing.pfx')) 'Ignored file of a closed item was removed.'
        Assert-True (Test-Path -LiteralPath $heldItem) 'Checkout of a closed item with a live owner was removed.'
    }
    finally { Pop-Location }
    Write-Host 'OK merged-worktree sweep tests passed.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
