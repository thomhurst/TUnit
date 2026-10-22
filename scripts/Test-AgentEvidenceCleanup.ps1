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
    Write-Host 'OK agent evidence cleanup: root naming, stale files and folders, fresh nested writes, dry run, links, missing root.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
