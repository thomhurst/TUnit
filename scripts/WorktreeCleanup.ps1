# WorktreeCleanup.ps1
# Shared worktree-removal helper, dot-sourced by Merge-Pr.ps1 and
# Remove-MergedWorktrees.ps1. Not meant to be run directly.
#
# Removal policy (one place, both callers):
#   - PRESERVE a worktree with uncommitted tracked changes or untracked files outside
#     known generated directories. Never force-discard possible work. Only with
#     -RetainWork (a closed work item) are such changes saved to a retained branch first;
#     unknown ignored files (secrets, local settings) always preserve the worktree.
#   - CLEAR untracked build artifacts (node_modules/bin/obj/etc.) — they are not work.
#   - CLEAR root-level workflow output covered by the repository's .gitignore.
#   - DETACH directory junctions and symlinks before deleting, so no delete follows a link.
#   - Long-path safe: git's own delete now works because core.longpaths=true is set
#     system-wide; the `\\?\` extended-length Remove-Item is kept as a fallback for
#     environments where that config is missing (Windows only; see Remove-DirectoryTree).

function Remove-DirectoryTree {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    # On Windows, \\?\ disables Win32 path normalization (and with it MAX_PATH), so forward
    # slashes (git's output format) are NOT translated — convert to backslashes or the delete
    # no-ops. The prefix means nothing on Linux/macOS, where it makes the path unresolvable.
    $target = if ($IsWindows -or $PSVersionTable.PSVersion.Major -lt 6) { '\\?\' + ($Path -replace '/', '\') } else { $Path }
    Remove-Item -LiteralPath $target -Recurse -Force -ErrorAction SilentlyContinue
}

# Git for Windows follows a directory junction while deleting a worktree: it empties the
# target, then fails with "Directory not empty" on the link and leaves a remnant without
# its .git file. Package managers link node_modules packages with junctions, and a junction
# can also point into another checkout or a shared store. Delete each link itself, never its
# target, before any recursive delete. Returns whether no directory link remains.
function Remove-WorktreeDirectoryLinks {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Worktree,
        [string]$Label = ''
    )

    if (-not $IsWindows) { return $true }

    # Get-ChildItem does not recurse through links, so every match is a link to remove.
    $find = { @(Get-ChildItem -LiteralPath $Worktree -Directory -Recurse -Force -Attributes ReparsePoint -ErrorAction SilentlyContinue) }
    foreach ($link in (& $find)) {
        # DirectoryInfo.Delete() without recursion removes the reparse point only.
        try { $link.Delete() } catch { Write-Host "Could not detach directory link $($link.FullName): $_" }
    }

    $remaining = & $find
    if ($remaining.Count -gt 0) {
        Write-Host "WARNING: worktree $Label kept: $($remaining.Count) directory link(s) could not be detached, first $($remaining[0].FullName): $Worktree"
        return $false
    }
    return $true
}

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
        ($normalizedPath -cmatch '\.(binlog|log|nettrace)$' -or
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
        # The work item is closed: save uncommitted changes and a detached HEAD to
        # retained branches instead of preserving the checkout. The branch is kept.
        [switch]$RetainWork,
        [string]$WorkItem,                         # names the retained branch (default: directory name)
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
    # A link to a worktree is not that worktree: git would follow it and remove the target.
    # Only the path itself is checked; a symlinked ancestor (macOS's /var) is fine and expected.
    # Fail closed: if the path cannot be inspected, it cannot be shown not to be a link.
    $worktreeItem = Get-Item -LiteralPath $Worktree -Force -ErrorAction SilentlyContinue
    if (-not $worktreeItem) {
        Write-Host "Preserving worktree $Label : $Worktree (path could not be inspected)"
        return
    }
    if ($worktreeItem.LinkType) {
        Write-Host "Preserving worktree $Label : $Worktree (path is a link)"
        return
    }
    # Ask git whether the path is the work tree's root rather than comparing paths: git reports
    # symlink-resolved paths (/private/var/... for macOS's /var/...), Resolve-Path does not.
    $null = git -C $Worktree rev-parse --show-toplevel 2>$null
    $isInsideWorkTree = $LASTEXITCODE -eq 0
    $prefix = git -C $Worktree rev-parse --show-prefix 2>$null
    $prefixKnown = $LASTEXITCODE -eq 0
    $isWorkTreeRoot = $isInsideWorkTree -and $prefixKnown -and -not $prefix
    if (-not $isWorkTreeRoot) {
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

    $work = @(Get-WorktreeUncommittedWork -Status $status)
    # A stash cannot carry ignored files, so they preserve the checkout even with -RetainWork.
    if ($work.Count -gt 0 -and (-not $RetainWork -or @($work | Where-Object { $_.StartsWith('!! ') }).Count -gt 0)) {
        Write-Host "Preserving dirty worktree $Label : $Worktree (uncommitted work)"
        foreach ($entry in $work) { Write-Host "  $entry" }
        return
    }

    if ($WhatIf) {
        $retained = if ($work.Count -gt 0) { " (saving $($work.Count) uncommitted entr(ies) to a retained branch)" } else { '' }
        Write-Host "sweep: WOULD remove $Worktree -- $Label$retained"
        return
    }

    if ($RetainWork) {
        # Nothing will come back for a closed item's checkout, so keep everything it holds
        # reachable from a branch instead of keeping the directory.
        $name = if ($WorkItem) { $WorkItem } else { [IO.Path]::GetFileName($worktreePath) }
        try {
            if ($work.Count -gt 0) {
                $saved = Save-WorktreeUncommittedWork -Repo $Repo -Worktree $Worktree -Name $name
                Write-Host "Saved uncommitted work of $Worktree to $saved (restore with: git stash apply $saved)"
                $status = @(git -C $Worktree status --porcelain=v1 --untracked-files=all --ignored=matching 2>$null)
                $statusRead = $LASTEXITCODE -eq 0
                $work = @(Get-WorktreeUncommittedWork -Status $status)
                if (-not $statusRead -or $work.Count -gt 0) {
                    Write-Host "Preserving dirty worktree $Label : $Worktree (uncommitted work remains after saving)"
                    foreach ($entry in $work) { Write-Host "  $entry" }
                    return
                }
            }
            Save-DetachedWorktreeHead -Repo $Repo -Worktree $Worktree
        }
        catch {
            Write-Host "Preserving worktree $Label : $Worktree (could not retain its work: $_)"
            return
        }
    }

    if (-not (Remove-WorktreeDirectoryLinks -Worktree $Worktree -Label $Label)) { return }

    # Primary path: let git remove it (force clears untracked artifacts; tracked is clean).
    git -C $Repo worktree remove --force $Worktree 2>$null

    # Fallback for long-path failures (only if core.longpaths is somehow off).
    if (Test-Path -LiteralPath $Worktree) {
        if (-not (Remove-WorktreeDirectoryLinks -Worktree $Worktree -Label $Label)) { return }
        Remove-DirectoryTree -Path $Worktree
    }

    git -C $Repo worktree prune
    if (Test-Path -LiteralPath $Worktree) {
        Write-Host "WARNING: could not fully remove $Worktree"
    } else {
        Write-Host "Removed worktree $Label : $Worktree"
    }
}

# Returns the porcelain status entries that are not disposable generated output.
function Get-WorktreeUncommittedWork {
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Status)

    return @($Status | Where-Object {
        if ($_ -notmatch '^(\?\?|!!) ') { return $true }
        return -not (Test-DisposableWorktreePath -Path $_.Substring(3))
    })
}

# Saves a worktree's tracked and untracked changes as a stash commit on the branch
# `retained-worktrees/<name>-wip-<sha12>`, leaving the checkout clean. Restore it anywhere
# with `git stash apply <branch>`. Ignored files are not saved, so a caller that re-checks
# the status still preserves them. Returns the branch; throws when nothing was saved.
function Save-WorktreeUncommittedWork {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Repo,
        [Parameter(Mandatory)][string]$Worktree,
        [Parameter(Mandatory)][string]$Name
    )

    $before = git -C $Repo rev-parse -q --verify refs/stash 2>$null
    git -C $Worktree stash push --include-untracked --quiet -m "retained worktree $Name" 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'git stash failed' }
    $stash = git -C $Repo rev-parse -q --verify refs/stash 2>$null
    if (-not $stash -or $stash -eq $before) { throw 'git stash saved nothing' }
    $branch = "retained-worktrees/$Name-wip-$($stash.Substring(0, 12))"
    git -C $Repo branch $branch $stash 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "could not create $branch; the changes remain in git stash list" }
    # Every worktree shares one stash stack; drop only the entry created here.
    if ((git -C $Repo rev-parse -q --verify 'stash@{0}' 2>$null) -eq $stash) {
        git -C $Repo stash drop --quiet 'stash@{0}' 2>$null | Out-Null
    }
    return $branch
}

# Keeps a detached HEAD reachable from `retained-worktrees/<SHA>` after Git prunes the
# worktree reflog. Never moves an existing branch. Throws when HEAD cannot be retained.
function Save-DetachedWorktreeHead {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Repo,
        [Parameter(Mandatory)][string]$Worktree
    )

    git -C $Worktree symbolic-ref -q HEAD 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { return }
    $head = git -C $Worktree rev-parse HEAD 2>$null
    if ($LASTEXITCODE -ne 0) { throw "Cannot resolve HEAD: $Worktree" }
    $branch = "retained-worktrees/$head"
    $existing = git -C $Repo rev-parse -q --verify "refs/heads/$branch" 2>$null
    if ($LASTEXITCODE -ne 0) {
        git -C $Repo branch $branch $head 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Cannot retain detached HEAD: $Worktree" }
    } elseif ($existing -ne $head) {
        throw "Retention branch has moved: $branch"
    }
}

# Resolves the lock name a worktree works under, exactly as AgentLocks.ps1 does: its
# agent.lockName marker when agent.lockWorktree records this worktree's root (`git
# worktree add` copies config.worktree, so a copied marker does not count), otherwise
# issue-<N> from an issue-<N>-<desc> branch. Returns $null when the worktree names no
# work item; throws when Git cannot read it.
function Get-WorktreeLockName {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Worktree)

    $top = git -C $Worktree rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $top) { throw "Cannot resolve worktree root: $Worktree" }
    # Without this extension `--worktree` aliases shared config, which can hold a legacy
    # marker of another checkout.
    $enabled = git -C $Worktree config --local --bool --get extensions.worktreeConfig 2>$null
    if ($LASTEXITCODE -notin 0, 1) { throw "Cannot inspect worktree lock configuration: $Worktree" }
    if ($enabled -and $enabled.Trim() -eq 'true') {
        $marker = git -C $Worktree config --worktree --get agent.lockName 2>$null
        if ($LASTEXITCODE -notin 0, 1) { throw "Cannot inspect worktree lock marker: $Worktree" }
        $owner = git -C $Worktree config --worktree --get agent.lockWorktree 2>$null
        if ($LASTEXITCODE -notin 0, 1) { throw "Cannot inspect worktree lock marker owner: $Worktree" }
        if ($marker -and $owner -and
            [IO.Path]::GetFullPath($owner.Trim()).TrimEnd('\', '/') -eq [IO.Path]::GetFullPath($top.Trim()).TrimEnd('\', '/')) {
            return $marker.Trim()
        }
    }
    $branch = git -C $Worktree rev-parse --abbrev-ref HEAD 2>$null
    if ($LASTEXITCODE -ne 0) { throw "Cannot resolve worktree branch: $Worktree" }
    if ($branch -match '^issue-(\d+)-') { return "issue-$($Matches[1])" }
    return $null
}

# Runs one AgentLocks.ps1 verb in a child process (the script exits). Status returns
# its state; other output, including an acquired token, is never surfaced.
function Invoke-AgentLockCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Script,
        [Parameter(Mandatory)][string]$Verb,
        [Parameter(Mandatory)][string]$LockName,
        [Parameter(Mandatory)][string]$OwnerId
    )

    $state = $null
    if ($Verb -eq 'status') {
        $state = (& pwsh -NoProfile -File $Script $Verb -LockName $LockName -OwnerId $OwnerId 2>$null | Out-String).Trim()
    }
    else {
        & pwsh -NoProfile -File $Script $Verb -LockName $LockName -OwnerId $OwnerId *> $null
    }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; State = $state }
}

# Agents keep logs, reports and PR-body drafts for a work item under
# <main checkout>-evidence/<lock name>/, beside the worktree root, so nothing lands
# loose in the parent directory. Evidence matters only while its item is active.
function Get-AgentEvidenceRoot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$MainCheckout)

    return "$(($MainCheckout -replace '\\', '/').TrimEnd('/'))-evidence"
}

# Removes each top-level entry of the evidence root whose newest write is older than
# the retention window. Returns the number of entries removed (or that would be).
function Remove-StaleAgentEvidence {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][int]$RetentionDays,
        [switch]$WhatIf
    )

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { return 0 }
    $cutoff = [DateTime]::UtcNow.AddDays(-$RetentionDays)
    $removed = 0
    foreach ($entry in (Get-ChildItem -LiteralPath $Root -Force -ErrorAction SilentlyContinue)) {
        $items = @($entry)
        if ($entry.PSIsContainer -and ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0) {
            $items += @(Get-ChildItem -LiteralPath $entry.FullName -Recurse -Force -ErrorAction SilentlyContinue)
        }
        $newest = ($items | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum
        if ($newest -ge $cutoff) { continue }
        if (Remove-AgentEvidenceEntry -Entry $entry -Kind 'stale' -WhatIf:$WhatIf) { $removed++ }
    }
    return $removed
}

# Removes the evidence of finished work items: each `pr-<N>` or `issue-<N>` folder whose
# item is closed (merged or not) and that no registered worktree still resolves to.
# Evidence of an open item survives release, because later units cite it. A temporary
# AgentLocks lease on the item is held through deletion, so a new owner cannot start
# writing midway; a held lease, unavailable Redis or an unreadable state keeps the
# evidence. Returns the number of entries removed (or that would be).
function Remove-FinishedAgentEvidence {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$AgentLocks,
        # Receives the lock name; returns $true only when the item is confirmed closed.
        [Parameter(Mandatory)][scriptblock]$IsClosed,
        [AllowEmptyCollection()][string[]]$ActiveLockNames = @(),
        [switch]$WhatIf
    )

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { return 0 }
    if (-not (Test-Path -LiteralPath $AgentLocks)) { return 0 }
    $active = New-OrdinalStringMap
    foreach ($name in $ActiveLockNames) { if ($name) { $active[$name] = $true } }

    $removed = 0
    foreach ($entry in (Get-ChildItem -LiteralPath $Root -Directory -Force -ErrorAction SilentlyContinue)) {
        $name = $entry.Name
        if ($name -cnotmatch '^(pr|issue)-\d+$' -or $active.ContainsKey($name)) { continue }
        try { if (-not (& $IsClosed $name)) { continue } }
        catch {
            Write-Host "sweep: could not read the state of $name; keeping its evidence"
            continue
        }

        $owner = Enter-WorkItemCleanupLease -AgentLocks $AgentLocks -LockName $name -WhatIf:$WhatIf
        if (-not $owner) {
            Write-Host "sweep: keeping evidence of $name (lock held or Redis unavailable)"
            continue
        }
        try {
            if (Remove-AgentEvidenceEntry -Entry $entry -Kind 'finished' -WhatIf:$WhatIf) { $removed++ }
        }
        finally { Exit-WorkItemCleanupLease -AgentLocks $AgentLocks -LockName $name -Owner $owner -WhatIf:$WhatIf }
    }
    return $removed
}

# Takes a temporary AgentLocks lease on a work item before the sweep deletes anything of
# it, so a new owner cannot start midway. Returns the lease's owner id, or $null when the
# lock is held, Redis is unavailable or the lock script is missing. -WhatIf only checks
# that the lock is FREE.
function Enter-WorkItemCleanupLease {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$AgentLocks,
        [Parameter(Mandatory)][string]$LockName,
        [switch]$WhatIf
    )

    if (-not (Test-Path -LiteralPath $AgentLocks)) { return $null }
    $owner = "sweep-cleanup-$([Guid]::NewGuid().ToString('N'))"
    $verb = if ($WhatIf) { 'status' } else { 'acquire' }
    try { $lease = Invoke-AgentLockCommand -Script $AgentLocks -Verb $verb -LockName $LockName -OwnerId $owner }
    catch { return $null }
    if ($lease.ExitCode -ne 0 -or ($WhatIf -and $lease.State -ne 'FREE')) { return $null }
    return $owner
}

# Releases a lease taken by Enter-WorkItemCleanupLease (nothing to release under -WhatIf).
function Exit-WorkItemCleanupLease {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$AgentLocks,
        [Parameter(Mandatory)][string]$LockName,
        [Parameter(Mandatory)][string]$Owner,
        [switch]$WhatIf
    )

    if ($WhatIf) { return }
    $released = Invoke-AgentLockCommand -Script $AgentLocks -Verb release -LockName $LockName -OwnerId $Owner
    if ($released.ExitCode -ne 0) {
        Write-Host "sweep: WARNING could not release cleanup lock for $LockName (exit $($released.ExitCode))"
    }
}

# Deletes one top-level evidence entry without following links. Returns whether it is
# gone (or, with -WhatIf, would be removed).
function Remove-AgentEvidenceEntry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][IO.FileSystemInfo]$Entry,
        [Parameter(Mandatory)][string]$Kind,
        [switch]$WhatIf
    )

    # Never follow a link out of the evidence root.
    if (($Entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        Write-Host "sweep: skipping evidence entry that is a link: $($Entry.FullName)"
        return $false
    }
    if ($WhatIf) {
        Write-Host "sweep: WOULD remove $Kind evidence $($Entry.FullName)"
        return $true
    }
    if ($Entry -is [IO.DirectoryInfo]) {
        if (-not (Remove-WorktreeDirectoryLinks -Worktree $Entry.FullName -Label '(evidence)')) { return $false }
        Remove-DirectoryTree -Path $Entry.FullName
    }
    else {
        Remove-Item -LiteralPath $Entry.FullName -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path -LiteralPath $Entry.FullName) {
        Write-Host "sweep: WARNING could not remove $Kind evidence $($Entry.FullName)"
        return $false
    }
    Write-Host "sweep: removed $Kind evidence $($Entry.FullName)"
    return $true
}
