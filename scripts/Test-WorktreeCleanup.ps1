$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'WorktreeCleanup.ps1')

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = [IO.Path]::GetFullPath((Join-Path $tempBase "worktree-cleanup-$([Guid]::NewGuid().ToString('N'))"))
if (-not $testRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to use test directory outside the system temporary directory: $testRoot"
}

$repo = Join-Path $testRoot 'repo'
$sourceWorktree = Join-Path $testRoot 'issue-123-fresh-source'
$artifactWorktree = Join-Path $testRoot 'issue-124-artifacts-only'

try {
    New-Item -ItemType Directory -Path $repo | Out-Null
    git -C $repo init -b main --quiet
    git -C $repo config user.name 'Worktree Cleanup Test'
    git -C $repo config user.email 'worktree-cleanup@example.invalid'
    Set-Content -LiteralPath (Join-Path $repo 'README.md') -Value '# fixture'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../.gitignore') -Destination (Join-Path $repo '.gitignore')
    Add-Content -LiteralPath (Join-Path $repo '.gitignore') -Value "`n.env`nignored-source/"
    git -C $repo add README.md .gitignore
    git -C $repo commit --quiet -m 'fixture'

    git -C $repo worktree add --quiet -b issue-123-fresh-source $sourceWorktree
    $newSource = Join-Path $sourceWorktree 'src/build/NewFeature.cs'
    New-Item -ItemType Directory -Path (Split-Path $newSource -Parent) | Out-Null
    Set-Content -LiteralPath $newSource -Value 'internal sealed class NewFeature;'

    Remove-MergedWorktree -Repo $repo -Worktree $sourceWorktree -Label 'source fixture'
    Assert-True (Test-Path -LiteralPath $sourceWorktree) 'Fresh issue worktree with untracked source was removed.'
    Assert-True (Test-Path -LiteralPath $newSource) 'Untracked source file was removed.'

    git -C $repo worktree remove --force $sourceWorktree
    git -C $repo branch -D issue-123-fresh-source | Out-Null

    git -C $repo worktree add --quiet -b issue-124-artifacts-only $artifactWorktree
    $generatedFile = Join-Path $artifactWorktree 'src/Fixture/bin/generated.dll'
    New-Item -ItemType Directory -Path (Split-Path $generatedFile -Parent) | Out-Null
    Set-Content -LiteralPath $generatedFile -Value 'generated'

    Remove-MergedWorktree -Repo $repo -Worktree $artifactWorktree -Label 'artifact fixture'
    Assert-True (-not (Test-Path -LiteralPath $artifactWorktree)) 'Artifact-only worktree was not removed.'

    $logWorktree = Join-Path $testRoot 'issue-125-logs'
    git -C $repo worktree add --quiet -b issue-125-logs $logWorktree
    $logPath = Join-Path $logWorktree 'test-run.log'
    Set-Content -LiteralPath $logPath -Value 'retained validation evidence'
    Set-Content -LiteralPath (Join-Path $logWorktree 'pr-body.md') -Value 'PR notes'
    git -C $logWorktree check-ignore --quiet test-run.log
    Assert-True ($LASTEXITCODE -eq 0) 'Root-level log was not ignored.'
    git -C $logWorktree check-ignore --quiet pr-body.md
    Assert-True ($LASTEXITCODE -eq 0) 'PR notes were not ignored.'
    Remove-MergedWorktree -Repo $repo -Worktree $logWorktree -WhatIf
    Assert-True (Test-Path -LiteralPath $logPath) 'Dry run removed a log.'
    Remove-MergedWorktree -Repo $repo -Worktree $logWorktree
    Assert-True (-not (Test-Path -LiteralPath $logWorktree)) 'Log-only worktree was not removed.'

    $blockedWorktree = Join-Path $testRoot 'issue-126-ignored-source'
    git -C $repo worktree add --quiet -b issue-126-ignored-source $blockedWorktree
    Set-Content -LiteralPath (Join-Path $blockedWorktree '.env') -Value 'local settings'
    New-Item -ItemType Directory -Path (Join-Path $blockedWorktree 'ignored-source') | Out-Null
    Set-Content -LiteralPath (Join-Path $blockedWorktree 'ignored-source/Feature.cs') -Value 'source'
    Remove-MergedWorktree -Repo $repo -Worktree $blockedWorktree
    Assert-True (Test-Path -LiteralPath (Join-Path $blockedWorktree '.env')) 'Ignored local settings were removed.'
    Assert-True (Test-Path -LiteralPath (Join-Path $blockedWorktree 'ignored-source/Feature.cs')) 'Ignored source was removed.'

    $originalHead = git -C $repo rev-parse HEAD
    Set-Content -LiteralPath (Join-Path $blockedWorktree 'README.md') -Value 'unpublished commit'
    git -C $blockedWorktree commit -am 'unpublished' --quiet
    $localHead = git -C $blockedWorktree rev-parse HEAD
    Assert-True (-not (Test-WorktreeHeadMerged -Repo $repo -Head $localHead -MergedHead $originalHead)) 'Unpublished commit treated as merged.'
    Assert-True (Test-WorktreeHeadMerged -Repo $repo -Head $originalHead -MergedHead $localHead) 'Earlier merged head was not recognized.'
    Assert-True (Test-WorktreeHeadMerged -Repo $repo -Head $localHead -MergedHead $localHead) 'Exact merged head was not recognized.'

    git -C $repo worktree lock $blockedWorktree
    Remove-MergedWorktree -Repo $repo -Worktree $blockedWorktree
    Assert-True (Test-Path -LiteralPath $blockedWorktree) 'Locked worktree was removed.'
    git -C $repo worktree unlock $blockedWorktree
    Remove-MergedWorktree -Repo $repo -Worktree $blockedWorktree -ExpectedHead $originalHead
    Assert-True (Test-Path -LiteralPath $blockedWorktree) 'Worktree with changed HEAD was removed.'
    Remove-MergedWorktree -Repo $repo -Worktree $repo
    Assert-True (Test-Path -LiteralPath (Join-Path $repo '.git')) 'Main checkout was removed.'

    $mainAssociation = [pscustomobject]@{
        state = 'closed'
        merged_at = '2026-08-19T00:00:00Z'
        head = [pscustomobject]@{ ref = 'main' }
    }
    $issueAssociation = [pscustomobject]@{
        state = 'closed'
        merged_at = '2026-08-19T00:00:00Z'
        head = [pscustomobject]@{ ref = 'issue-123-fresh-source' }
    }
    $differentCaseAssociation = [pscustomobject]@{
        state = 'closed'
        merged_at = '2026-08-19T00:00:00Z'
        head = [pscustomobject]@{ ref = 'Issue-123-Fresh-Source' }
    }

    Assert-True (-not (Test-WorktreeMatchesMergedPullRequest -Associations @($mainAssociation) -Branch 'issue-123-fresh-source' -Detached $false)) `
        'Named issue branch matched an unrelated merged PR through a shared commit.'
    Assert-True (Test-WorktreeMatchesMergedPullRequest -Associations @($issueAssociation) -Branch 'issue-123-fresh-source' -Detached $false) `
        'Named issue branch did not match its own merged PR.'
    Assert-True (-not (Test-WorktreeMatchesMergedPullRequest -Associations @($differentCaseAssociation) -Branch 'issue-123-fresh-source' -Detached $false)) `
        'Named issue branch matched a differently-cased merged PR branch.'
    Assert-True (Test-WorktreeMatchesMergedPullRequest -Associations @($mainAssociation) -Branch $null -Detached $true) `
        'Detached worktree did not match its merged commit association.'

    $mergedNames = New-OrdinalStringMap
    $mergedNames['issue-123-fresh-source'] = $true
    Assert-True (-not $mergedNames.ContainsKey('Issue-123-Fresh-Source')) `
        'Merged branch map matched a differently-cased branch name.'

    Assert-True (-not (Test-DisposableWorktreePath -Path 'src/Bin/Generated.cs')) `
        'Differently-cased source directory was treated as disposable output.'
    foreach ($sourcePath in @(
        'src/build/NewFeature.cs',
        'src/.cache/NewFeature.cs',
        'src/.docusaurus/NewFeature.cs'
    )) {
        Assert-True (-not (Test-DisposableWorktreePath -Path $sourcePath)) `
            "Docs-scoped generated directory was treated as disposable outside docs: $sourcePath"
    }
    foreach ($sourcePath in @('src/source.log', 'README.md', 'new-feature.md', 'src/NewFeature.cs', '"quoted.log"')) {
        Assert-True (-not (Test-DisposableWorktreePath -Path $sourcePath)) "Unknown file was treated as workflow output: $sourcePath"
    }
    foreach ($outputPath in @('build.log', 'pressure.nettrace', 'pr-body.md', 'review-pr-body.md', 'sdk-review-disposition.md', 'review-validation.md', 'rebase-validation.md', 'ci-fix-comment.md', 'throttle-issue.md', '.artifacts/run.log')) {
        Assert-True (Test-DisposableWorktreePath -Path $outputPath) "Workflow output was not recognized: $outputPath"
        git -C $repo check-ignore --quiet -- $outputPath
        Assert-True ($LASTEXITCODE -eq 0) "Disposable workflow output is missing from .gitignore: $outputPath"
    }
    foreach ($generatedPath in @(
        'BenchmarkDotNet.Artifacts/results/report.csv',
        'docs/build/index.html',
        'docs/.docusaurus/client-manifest.json',
        'docs/.cache/webpack/default-development.pack',
        'CodeCoverage/report.xml',
        'src/Fixture/Debug/fixture.dll',
        'src/Fixture/Release/fixture.dll',
        'src/Fixture/x64/fixture.dll',
        'src/Fixture/ARM64/fixture.dll',
        'logs/build.log',
        '__pycache__/fixture.pyc',
        'results/test-output.json',
        'StrykerOutput/reports/mutation-report.html',
        'benchmark-results/run/output.log',
        'temptest/fixture.csproj'
    )) {
        Assert-True (Test-DisposableWorktreePath -Path $generatedPath) `
            "Repository-generated path was not treated as disposable: $generatedPath"
    }

    Write-Host 'OK worktree cleanup safety tests passed.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
