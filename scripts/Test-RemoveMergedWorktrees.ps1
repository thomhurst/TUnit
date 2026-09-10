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
    if ($args[0] -eq 'api') { return '[]' }
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
    $mergedJson = @(
        foreach ($name in @('merged', 'dirty', 'divergent', 'open')) {
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

        & $sweep -Repo fixture/repo
        Assert-True (-not (Test-Path -LiteralPath $mergedWorktree)) 'Merged worktree was not removed autonomously.'
        Assert-True (Test-Path -LiteralPath $dirtyWorktree) 'Dirty worktree was removed.'
        Assert-True (Test-Path -LiteralPath $divergentWorktree) 'Divergent local commit was removed.'
        Assert-True (Test-Path -LiteralPath $openWorktree) 'Open PR worktree was removed.'
        git -C $repo show-ref --verify --quiet refs/heads/merged
        Assert-True ($LASTEXITCODE -eq 0) 'Merged branch was deleted without an explicit request.'
        git -C $repo show-ref --verify --quiet refs/heads/divergent
        Assert-True ($LASTEXITCODE -eq 0) 'Divergent branch was deleted.'
    }
    finally { Pop-Location }
    Write-Host 'OK merged-worktree sweep tests passed.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
