# Local checkout lifecycle. Keep branches; retain detached commits before removal.
# Called while AgentLocks still owns the lock, before another worker can acquire it.
function Remove-ReleasedWorktree {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Worktree,
        [Parameter(Mandatory)][string]$LockName
    )

    if (-not (Test-Path -LiteralPath $Worktree)) { return }
    $target = (Resolve-Path -LiteralPath $Worktree).Path.TrimEnd('\', '/')
    $common = git rev-parse --path-format=absolute --git-common-dir 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify the calling repository.' }
    $targetCommon = git -C $target rev-parse --path-format=absolute --git-common-dir 2>$null
    if ($LASTEXITCODE -ne 0 -or $targetCommon -ne $common) {
        throw "Worktree belongs to another repository: $target"
    }
    $top = git -C $target rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0 -or [IO.Path]::GetFullPath($top) -ne $target) {
        throw "Not a worktree root: $target"
    }
    $gitDirectory = git -C $target rev-parse --absolute-git-dir 2>$null
    if ($LASTEXITCODE -ne 0 -or $gitDirectory -eq $common -or
        (Test-Path -LiteralPath (Join-Path $gitDirectory 'locked'))) {
        Write-Host "Preserving main or Git-locked worktree: $target"
        return
    }
    $marker = git -C $target config --worktree --get agent.lockName 2>$null
    $markerOwner = git -C $target config --worktree --get agent.lockWorktree 2>$null
    # A marker copied in by `git worktree add` names another checkout's path.
    if ($marker -cne $LockName -or -not $markerOwner -or
        [IO.Path]::GetFullPath($markerOwner.Trim()).TrimEnd('\', '/') -ne $target) {
        Write-Host "Preserving worktree without a matching lock marker: $target"
        return
    }
    $entries = @(git worktree list --porcelain)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot list registered worktrees.' }
    $paths = @($entries | Where-Object { $_.StartsWith('worktree ') } |
        ForEach-Object { [IO.Path]::GetFullPath($_.Substring(9)).TrimEnd('\', '/') })
    if ($target -notin $paths -or $paths.Count -eq 0) { throw "Unregistered worktree: $target" }
    $repo = $paths[0]
    $relative = [IO.Path]::GetRelativePath($repo, $target)
    if ($relative -eq '.' -or (-not [IO.Path]::IsPathRooted($relative) -and
        $relative -ne '..' -and -not $relative.StartsWith("..$([IO.Path]::DirectorySeparatorChar)"))) {
        Write-Host "Preserving main or harness-managed worktree: $target"
        return
    }

    . (Join-Path $PSScriptRoot 'WorktreeCleanup.ps1')
    $status = @(git -C $target status --porcelain=v1 --untracked-files=all --ignored=matching 2>$null)
    if ($LASTEXITCODE -ne 0) { throw "Cannot inspect worktree: $target" }
    $work = @(Get-WorktreeUncommittedWork -Status $status)
    if ($work.Count -gt 0) {
        Write-Host "Preserving dirty worktree: $target ($($work[0]))"
        return
    }
    # A stable ref keeps unpublished detached commits reachable after Git prunes the
    # worktree reflog.
    Save-DetachedWorktreeHead -Repo $repo -Worktree $target
    # Move this child shell out of the directory before deleting it on Windows.
    Set-Location -LiteralPath $repo
    [Environment]::CurrentDirectory = $repo
    if (-not (Remove-WorktreeDirectoryLinks -Worktree $target)) {
        Write-Host "Preserving released worktree with directory links that could not be detached: $target"
        return
    }
    git -C $repo -c core.longpaths=true worktree remove --force -- $target
    if ($LASTEXITCODE -ne 0) { throw "Could not remove released worktree: $target" }
    Write-Host "Removed released worktree: $target"
}
