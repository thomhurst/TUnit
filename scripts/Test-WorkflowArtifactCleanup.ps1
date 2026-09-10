$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'WorktreeCleanup.ps1')
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = [IO.Path]::GetFullPath((Join-Path $tempBase "workflow-cleanup-$([Guid]::NewGuid().ToString('N'))"))
if (-not $testRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture path.' }
$repo = Join-Path $testRoot 'repo'
$worktree = Join-Path $testRoot 'worktree'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

try {
    New-Item -ItemType Directory -Path $repo | Out-Null
    git -C $repo init -b main --quiet
    git -C $repo config user.name 'Workflow Cleanup Test'
    git -C $repo config user.email 'workflow-cleanup@example.invalid'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../.gitignore') -Destination (Join-Path $repo '.gitignore')
    Add-Content -LiteralPath (Join-Path $repo '.gitignore') -Value "`n.env`nignored-source/"
    git -C $repo add .gitignore
    git -C $repo commit --quiet -m 'fixture'
    git -C $repo worktree add --quiet -b merged $worktree
    $head = git -C $repo rev-parse HEAD
    $removeArgs = @{ Repo = $repo; Worktree = $worktree }
    if ((Get-Command Remove-MergedWorktree).Parameters.ContainsKey('ExpectedHead')) { $removeArgs.ExpectedHead = $head }

    foreach ($file in @('build.log', 'pressure.nettrace', 'pr-body.md', 'review-pr-body.md', 'sdk-review-disposition.md', 'rebase-validation.md', 'ci-fix-comment.md', 'throttle-issue.md', '.artifacts/run.txt')) {
        git -C $worktree check-ignore --quiet -- $file
        Assert-True ($LASTEXITCODE -eq 0) "Workflow output is not ignored: $file"
        Assert-True (Test-DisposableWorktreePath -Path $file) "Workflow output is not disposable: $file"
        $path = Join-Path $worktree $file
        New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
        Set-Content -LiteralPath $path -Value 'temporary output'
    }

    $source = Join-Path $worktree 'NewFeature.cs'
    Set-Content -LiteralPath $source -Value 'untracked source'
    Remove-MergedWorktree @removeArgs
    Assert-True (Test-Path -LiteralPath $source) 'Untracked source was removed.'
    Remove-Item -LiteralPath $source

    $settings = Join-Path $worktree '.env'
    Set-Content -LiteralPath $settings -Value 'local settings'
    if ($IsWindows) { (Get-Item -LiteralPath $settings -Force).Attributes = [IO.FileAttributes]::Hidden }
    Remove-MergedWorktree @removeArgs
    Assert-True (Test-Path -LiteralPath $settings) 'Unknown ignored files were removed.'
    Remove-Item -LiteralPath $settings -Force

    $trackedLog = Join-Path $worktree 'tracked.log'
    Set-Content -LiteralPath $trackedLog -Value 'tracked fixture'
    git -C $worktree add --force tracked.log
    Remove-MergedWorktree @removeArgs
    Assert-True (Test-Path -LiteralPath $trackedLog) 'Tracked log changes were discarded.'
    git -C $worktree reset --quiet HEAD -- tracked.log

    if ((Get-Command Remove-MergedWorktree).Parameters.ContainsKey('WhatIf')) {
        Remove-MergedWorktree @removeArgs -WhatIf
        Assert-True (Test-Path -LiteralPath $worktree) 'Dry run removed a worktree.'
    }
    Remove-MergedWorktree @removeArgs
    Assert-True (-not (Test-Path -LiteralPath $worktree)) 'Ignored workflow output prevented cleanup.'
    foreach ($path in @('src/source.log', 'src/pr-body.md', 'README.md', 'new-feature.md', '.env')) {
        Assert-True (-not (Test-DisposableWorktreePath -Path $path)) "Source or unknown file marked disposable: $path"
    }
    Write-Host 'OK workflow artifact cleanup tests passed.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
