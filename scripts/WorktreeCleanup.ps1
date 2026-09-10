# WorktreeCleanup.ps1
# Shared worktree-removal helper, dot-sourced by Merge-Pr.ps1 and
# Remove-MergedWorktrees.ps1. Not meant to be run directly.
#
# Removal policy (one place, both callers):
#   - PRESERVE a worktree with uncommitted tracked changes or untracked files outside
#     known generated directories. Never force-discard possible work.
#   - CLEAR untracked build artifacts (node_modules/bin/obj/etc.) — they are not work.
#   - CLEAR root-level workflow output covered by the repository's .gitignore.
#   - Long-path safe: git's own delete now works because core.longpaths=true is set
#     system-wide; the `\\?\` extended-length Remove-Item is kept as a fallback for
#     environments where that config is missing.

function New-OrdinalStringMap {
    [CmdletBinding()]
    [OutputType([System.Collections.Generic.Dictionary[string, bool]])]
    param()

    return [System.Collections.Generic.Dictionary[string, bool]]::new([System.StringComparer]::Ordinal)
}

$script:DisposableWorktreeGeneratedDirectories = New-OrdinalStringMap
foreach ($directory in @(
    '.artifacts',
    '.vs',
    '__pycache__',
    'ARM',
    'ARM64',
    'artifacts',
    'benchmark-results',
    'BenchmarkDotNet.Artifacts',
    'bin',
    'bld',
    'CodeCoverage',
    'Debug',
    'DebugPublic',
    'log',
    'logs',
    'node_modules',
    'obj',
    'Release',
    'Releases',
    'results',
    'StrykerOutput',
    'temptest',
    'TestResults',
    'Win32',
    'x64',
    'x86'
)) {
    $script:DisposableWorktreeGeneratedDirectories[$directory] = $true
}

$script:DisposableWorktreeScopedDirectories = @(
    'docs/.cache',
    'docs/.docusaurus',
    'docs/build'
)

function Test-DisposableWorktreePath {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    # Quoted porcelain paths require Git's escape decoding. Preserve them rather than
    # risk classifying an unusual source path as generated output.
    if ($Path.StartsWith('"', [System.StringComparison]::Ordinal)) { return $false }

    $normalizedPath = $Path -replace '\\', '/'
    # Keep these root-only patterns aligned with .gitignore. Ignore rules alone
    # cannot authorize deleting arbitrary files: ignored source and secrets survive.
    if (-not $normalizedPath.Contains('/') -and
        ($normalizedPath -cmatch '\.(log|nettrace)$' -or
         $normalizedPath -cmatch '(^|-)(pr-body|review-disposition|review-validation|rebase-validation|comment|issue)\.md$')) {
        return $true
    }
    foreach ($docsGeneratedDirectory in $script:DisposableWorktreeScopedDirectories) {
        if ($normalizedPath -ceq $docsGeneratedDirectory -or
            $normalizedPath.StartsWith("$docsGeneratedDirectory/", [System.StringComparison]::Ordinal)) {
            return $true
        }
    }

    foreach ($segment in ($normalizedPath -split '/')) {
        if ($script:DisposableWorktreeGeneratedDirectories.ContainsKey($segment)) { return $true }
    }

    return $false
}

function Test-WorktreeMatchesMergedPullRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Associations,
        [AllowNull()][string]$Branch,
        [bool]$Detached
    )

    foreach ($association in $Associations) {
        if (-not $association.merged_at) { continue }
        if ($Detached) { return $true }
        if ($Branch -and $association.head -and $association.head.ref -ceq $Branch) { return $true }
    }

    return $false
}

function Test-WorktreeHeadMerged {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Repo,
        [Parameter(Mandatory)][string]$Head,
        [Parameter(Mandatory)][string]$MergedHead
    )

    if ($Head -ceq $MergedHead) { return $true }
    git -C $Repo merge-base --is-ancestor $Head $MergedHead 2>$null
    return $LASTEXITCODE -eq 0
}

function Remove-MergedWorktree {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Repo,       # a checkout that is NOT the one being removed (main)
        [Parameter(Mandatory)][string]$Worktree,   # path to remove
        [string]$Label = '',                       # e.g. "#1234" for log lines
        [string]$ExpectedHead,
        [switch]$WhatIf
    )

    if (-not (Test-Path -LiteralPath $Worktree)) {
        if (-not $WhatIf) { git -C $Repo worktree prune }
        return
    }

    $repoPath = (Resolve-Path -LiteralPath $Repo).Path.TrimEnd('\', '/')
    $worktreePath = (Resolve-Path -LiteralPath $Worktree).Path.TrimEnd('\', '/')
    $relativePath = [IO.Path]::GetRelativePath($repoPath, $worktreePath)
    if ($relativePath -eq '.' -or (-not [IO.Path]::IsPathRooted($relativePath) -and
        $relativePath -ne '..' -and -not $relativePath.StartsWith("..$([IO.Path]::DirectorySeparatorChar)"))) {
        Write-Host "Preserving main or harness-managed worktree: $Worktree"
        return
    }
    $topLevel = git -C $Worktree rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0 -or [IO.Path]::GetFullPath($topLevel) -ne $worktreePath) {
        Write-Host "Preserving worktree $Label : $Worktree (could not verify repository path)"
        return
    }
    $gitDirectory = git -C $Worktree rev-parse --absolute-git-dir 2>$null
    if ($LASTEXITCODE -ne 0 -or (Test-Path -LiteralPath (Join-Path $gitDirectory 'locked'))) {
        Write-Host "Preserving locked or unreadable worktree: $Worktree"
        return
    }
    $commonDirectory = git -C $Worktree rev-parse --path-format=absolute --git-common-dir 2>$null
    if ($LASTEXITCODE -ne 0) { return }
    $repoCommonDirectory = git -C $Repo rev-parse --path-format=absolute --git-common-dir 2>$null
    if ($LASTEXITCODE -ne 0 -or $commonDirectory -ne $repoCommonDirectory -or $gitDirectory -eq $commonDirectory) {
        Write-Host "Preserving worktree from another repository or main checkout: $Worktree"
        return
    }
    $head = git -C $Worktree rev-parse HEAD 2>$null
    if ($LASTEXITCODE -ne 0 -or ($ExpectedHead -and $head -cne $ExpectedHead)) {
        Write-Host "Preserving worktree $Label : $Worktree (HEAD changed during sweep)"
        return
    }

    # Preserve tracked work and every untracked/ignored path that is not under a known
    # generated directory. --force below is safe only after this fail-closed check.
    $status = @(git -C $Worktree status --porcelain=v1 --untracked-files=all --ignored=matching 2>$null)
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Preserving worktree $Label : $Worktree (could not inspect worktree status)"
        return
    }

    $work = @($status | Where-Object {
        if ($_ -notmatch '^(\?\?|!!) ') { return $true }
        return -not (Test-DisposableWorktreePath -Path $_.Substring(3))
    })
    if ($work.Count -gt 0) {
        Write-Host "Preserving dirty worktree $Label : $Worktree (uncommitted work)"
        foreach ($entry in $work) { Write-Host "  $entry" }
        return
    }

    if ($WhatIf) {
        Write-Host "sweep: WOULD remove $Worktree -- $Label"
        return
    }

    # Primary path: let git remove it (force clears untracked artifacts; tracked is clean).
    git -C $Repo worktree remove --force $Worktree 2>$null

    # Fallback for long-path failures (only if core.longpaths is somehow off).
    if (Test-Path -LiteralPath $Worktree) {
        # Avoid recursing through a package-manager junction if one exists in a docs
        # worktree. Leave it for manual cleanup instead of risking deletion outside
        # the worktree.
        $junction = Get-ChildItem -LiteralPath $Worktree -Directory -Recurse -Force -Filter node_modules -ErrorAction SilentlyContinue |
            Where-Object { ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 } |
            Select-Object -First 1
        if ($junction) {
            Write-Host "WARNING: worktree $Label requires manual removal -- detach the node_modules junction at $($junction.FullName) first, then re-run cleanup: $Worktree"
            return
        }
        # \\?\ disables Win32 path normalization, so forward slashes (git's output
        # format) are NOT translated — convert to backslashes or the delete no-ops.
        Remove-Item -LiteralPath ('\\?\' + ($Worktree -replace '/', '\')) -Recurse -Force -ErrorAction SilentlyContinue
    }

    git -C $Repo worktree prune
    if (Test-Path -LiteralPath $Worktree) {
        Write-Host "WARNING: could not fully remove $Worktree"
    } else {
        Write-Host "Removed worktree $Label : $Worktree"
    }
}
