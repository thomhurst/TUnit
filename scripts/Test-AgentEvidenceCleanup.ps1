# Local regression suite: requires PowerShell 7 only (no Git remote, no Docker).
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'WorktreeCleanup.ps1')
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = [IO.Path]::GetFullPath((Join-Path $tempBase "evidence-cleanup-$([Guid]::NewGuid().ToString('N'))"))
if (-not $testRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Test directory is outside the temporary directory: $testRoot"
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function New-Evidence([string]$Path, [int]$AgeDays) {
    New-Item -ItemType Directory -Path (Split-Path $Path -Parent) -Force | Out-Null
    Set-Content -LiteralPath $Path -Value 'evidence'
    (Get-Item -LiteralPath $Path).LastWriteTimeUtc = [DateTime]::UtcNow.AddDays(-$AgeDays)
}

try {
    $root = Get-AgentEvidenceRoot -MainCheckout (Join-Path $testRoot 'Repo')
    Assert-True ($root -ceq (($testRoot -replace '\\', '/') + '/Repo-evidence')) "Unexpected evidence root: $root"
    Assert-True ((Get-AgentEvidenceRoot -MainCheckout "$testRoot/Repo/") -ceq $root) 'Trailing separator changed the evidence root.'

    New-Evidence (Join-Path $root 'issue-1/build.log') 10
    New-Evidence (Join-Path $root 'issue-2/old.log') 10
    New-Evidence (Join-Path $root 'issue-2/nested/fresh.log') 1
    New-Evidence (Join-Path $root 'pr-3-body.md') 10
    New-Evidence (Join-Path $root 'pr-4-body.md') 1
    # Directory timestamps must not keep stale evidence alive or hide fresh files.
    foreach ($directory in Get-ChildItem -LiteralPath $root -Directory -Recurse) {
        $directory.LastWriteTimeUtc = [DateTime]::UtcNow.AddDays(-10)
    }

    $wouldRemove = Remove-StaleAgentEvidence -Root $root -RetentionDays 7 -WhatIf
    Assert-True ($wouldRemove -eq 2) "Dry run counted $wouldRemove stale entries."
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'issue-1')) 'Dry run removed evidence.'
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'pr-3-body.md')) 'Dry run removed an evidence file.'

    $removed = Remove-StaleAgentEvidence -Root $root -RetentionDays 7
    Assert-True ($removed -eq 2) "Removed $removed stale entries."
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $root 'issue-1'))) 'Stale evidence folder survived.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $root 'pr-3-body.md'))) 'Stale evidence file survived.'
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'issue-2/old.log')) 'Folder with recent writes was removed.'
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'pr-4-body.md')) 'Recent evidence file was removed.'

    # A stale link at the top level is skipped, and its target is never touched.
    if ($IsWindows) {
        $outside = Join-Path $testRoot 'outside'
        New-Evidence (Join-Path $outside 'keep.txt') 10
        $link = Join-Path $root 'linked'
        New-Item -ItemType Junction -Path $link -Target $outside | Out-Null
        $removed = Remove-StaleAgentEvidence -Root $root -RetentionDays 7
        Assert-True ($removed -eq 0) "Removed $removed entries through a link."
        Assert-True (Test-Path -LiteralPath (Join-Path $outside 'keep.txt')) 'Evidence cleanup deleted files through a link.'
        [IO.Directory]::Delete($link)
    }

    Assert-True ((Remove-StaleAgentEvidence -Root (Join-Path $testRoot 'missing') -RetentionDays 7) -eq 0) 'Missing root reported removals.'

    # Finished evidence: closed items without a worktree go, whatever their age. The
    # fake lock script holds pr-13, records every call, and requires an owner.
    $locks = Join-Path $testRoot 'AgentLocks.ps1'
    $lockLog = Join-Path $testRoot 'locks.log'
    Set-Content -LiteralPath $locks -Value @"
param(`$Verb, [string]`$LockName, [string]`$OwnerId)
if (-not `$OwnerId) { exit 2 }
Add-Content -LiteralPath '$lockLog' -Value "`$Verb `$LockName"
if (`$LockName -eq 'pr-13') { if (`$Verb -eq 'status') { 'HELD'; exit 0 }; exit 3 }
if (`$Verb -eq 'status') { 'FREE' }
exit 0
"@
    foreach ($name in @('pr-10', 'issue-11', 'pr-12', 'pr-13', 'pr-14', 'pr-15', 'pr-16-followup', 'main-ci')) {
        New-Evidence (Join-Path $root "$name/build.log") 0
    }
    $closed = { param($LockName)
        if ($LockName -eq 'pr-15') { throw 'state unavailable' }
        return $LockName -notin @('pr-12', 'issue-2')
    }
    $finishedArguments = @{ Root = $root; AgentLocks = $locks; IsClosed = $closed; ActiveLockNames = @('pr-14', $null) }

    $wouldRemove = Remove-FinishedAgentEvidence @finishedArguments -WhatIf
    Assert-True ($wouldRemove -eq 2) "Dry run counted $wouldRemove finished entries."
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'pr-10')) 'Dry run removed finished evidence.'
    Assert-True (-not (Select-String -LiteralPath $lockLog -Pattern '^(acquire|release) ' -Quiet)) 'Dry run took a lock.'
    Remove-Item -LiteralPath $lockLog

    $removed = Remove-FinishedAgentEvidence @finishedArguments
    Assert-True ($removed -eq 2) "Removed $removed finished entries."
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $root 'pr-10'))) 'Closed PR evidence survived.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $root 'issue-11'))) 'Closed issue evidence survived.'
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'pr-12')) 'Open item evidence was removed.'
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'pr-13')) 'Evidence of a locked item was removed.'
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'pr-14')) 'Evidence of an item with a worktree was removed.'
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'pr-15')) 'Evidence with unreadable state was removed.'
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'pr-16-followup')) 'Non-item evidence was removed as finished.'
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'main-ci')) 'Non-item evidence was removed as finished.'
    $calls = @(Get-Content -LiteralPath $lockLog)
    foreach ($name in @('pr-10', 'issue-11')) {
        Assert-True ($calls.IndexOf("acquire $name") -ge 0 -and $calls.IndexOf("release $name") -gt $calls.IndexOf("acquire $name")) "Lease for $name was not acquired then released: $($calls -join '; ')"
    }
    Assert-True ($calls -notcontains 'release pr-13') 'Released a lock the cleanup did not hold.'

    # Worktree identity resolves like AgentLocks.ps1: own marker, else issue branch.
    $repo = Join-Path $testRoot 'git'
    git init --quiet --initial-branch main $repo
    git -C $repo -c user.name=t -c user.email=t@t commit --quiet --allow-empty -m init
    git -C $repo config --local extensions.worktreeConfig true
    $marked = Join-Path $testRoot 'wt-marked'
    git -C $repo worktree add --quiet -b work $marked
    git -C $marked config --worktree agent.lockName pr-20
    git -C $marked config --worktree agent.lockWorktree $marked
    # `git worktree add` copies config.worktree; the copied marker names another path.
    $copied = Join-Path $testRoot 'wt-copied'
    git -C $marked worktree add --quiet -b copied $copied
    git -C $copied config --worktree agent.lockName pr-20
    git -C $copied config --worktree agent.lockWorktree $marked
    $issue = Join-Path $testRoot 'wt-issue'
    git -C $repo worktree add --quiet -b issue-21-fix $issue
    Assert-True ((Get-WorktreeLockName -Worktree $marked) -ceq 'pr-20') 'Own marker was not resolved.'
    Assert-True ($null -eq (Get-WorktreeLockName -Worktree $copied)) 'Copied marker was resolved.'
    Assert-True ((Get-WorktreeLockName -Worktree $issue) -ceq 'issue-21') 'Issue branch was not resolved.'
    Assert-True ($null -eq (Get-WorktreeLockName -Worktree $repo)) 'Main checkout resolved a lock name.'
    $threw = $false
    try { Get-WorktreeLockName -Worktree (Join-Path $testRoot 'not-a-repo') | Out-Null } catch { $threw = $true }
    Assert-True $threw 'Unreadable worktree did not throw.'

    Write-Host 'OK agent evidence cleanup: root naming, stale files and folders, fresh nested writes, dry run, links, missing root, finished items, leases, worktree identity.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
