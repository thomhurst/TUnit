# AgentLocks.ps1
# Reusable Redis work-item locks for the issue-pr-loop skill (concurrent agents).
#
# Release removes its recorded clean worktree before dropping the Redis lock.
# Local branches and detached commits survive for later checkout. Dirty or Git-locked
# worktrees remain on disk with an explicit diagnostic.
#
# The lock is a machine-level mutex that AUTO-EXPIRES after TTL (see $lockTtlSeconds,
# 2h). Agents do NOT heartbeat it periodically — they just `release` when done, or let
# it expire if they die. The cross-process claim that outlives the lock is GitHub's
# in-progress label / open PR, so a lock expiring under a slow-but-live agent is
# backstopped there; `renew` exists only for the rare unit that runs past the TTL.
#
# The acquiring agent's token is cached under a stable owner-identity namespace, then
# keyed by lock name, so renew/release do NOT need the token passed back. Owner identity
# resolves from -OwnerId, TUNIT_AGENT_LOCK_OWNER_ID, or CODEX_THREAD_ID (in that order).
# Non-Codex callers must provide one of the explicit overrides; there is deliberately
# no machine-wide fallback. Redis remains the sole ownership authority (token-checked
# EVAL on release/renew); the state file is only the agent's private token cache, never
# an ownership signal. A losing acquire prints HELD and writes nothing.
#
# Verbs:
#   acquire  -LockName pr-1234 [-Worktree <path>]
#              stdout = token, exit 0    -> acquired (auto-expires after TTL)
#              stderr = "HELD", exit 3   -> already locked by someone; skip
#   release  [-LockName pr-1234]
#              exit 0                    -> released; reports checkout removal/preservation
#              stderr = "STALE", exit 5  -> token mismatch/expired; do NOT delete or
#                                           overwrite; just skip
#   renew    -LockName pr-1234 [-Worktree <path>]   (OPTIONAL — NOT periodic)
#              exit 0 (silent)           -> TTL re-armed. Call only for a unit that may
#                                           run past the TTL, or once with -Worktree to
#                                           write the auto-derive marker.
#                                           A FREE lock (Redis lost the key) is taken
#                                           back with the cached token.
#              stderr = "LOST", exit 4   -> another owner holds it; abandon the item
#   status   [-LockName pr-1234]
#              stdout = HELD-BY-ME | HELD | FREE, exit 0
#              read-only; unavailable Redis returns exit 1 without starting it
#   ping                                  (no lock name or owner needed)
#              exit 0                    -> lock Redis answers; exit 1 -> it does not
#   defer    -LockName pr-1234 -Fingerprint <text> [-DeferSeconds n]
#              exit 0                    -> records that the item was deferred while its
#                                           GitHub state matched <text>; expires after n s
#   deferred -LockName pr-1234[,issue-56,...]
#              stdout = one `<lock name>=<fingerprint>` line per recorded deferral; exit 0
#   Deferral records are advisory. Start-IssuePrLoop.ps1 skips an item while its current
#   fingerprint equals the recorded one, so an unchanged blocker is not re-examined.
#
# Common failure exit: 1 (docker/redis unavailable, or no cached token for
# renew/release). 2 = bad usage / lock name or owner identity could not be resolved.
#
# Lock-name resolution (renew/release/status): -LockName is OPTIONAL there. When
# omitted it is resolved from, in order: (1) a marker persisted in the current
# worktree's per-worktree git config (`agent.lockName`), which
# `acquire`/`renew -Worktree` writes after enabling `extensions.worktreeConfig`,
# together with `agent.lockWorktree` (the worktree's own path). The marker counts only
# when that path matches the current worktree: `git worktree add` copies the source
# checkout's config.worktree, so a copied marker names another path and is ignored.
# The main checkout never receives a marker; then (2) an `issue-<N>-*` branch name -> `issue-<N>`. `acquire` still REQUIRES an
# explicit name (or an `issue-<N>-*` branch): it runs before the worktree exists, and
# a PR lock's number (`pr-<N>`) is not derivable from the branch (`issue-<M>-...`).
# For PR locks either pass -LockName to release, or write the marker once via
# `renew -Worktree` (or `acquire -Worktree` if the worktree already exists).
#
# Usage:
#   $token = pwsh scripts/AgentLocks.ps1 acquire -LockName pr-1234
#   if ($LASTEXITCODE -ne 0) { <skip this item> }
#   ... do the unit of work — NO periodic refresh ...
#   pwsh scripts/AgentLocks.ps1 release -LockName pr-1234
#   # optional, only if the unit may exceed the TTL:
#   pwsh scripts/AgentLocks.ps1 renew -LockName pr-1234
#   # outside Codex, set one stable value for every command from the same agent:
#   $env:TUNIT_AGENT_LOCK_OWNER_ID = 'my-agent-run-id'

[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet('acquire', 'release', 'renew', 'status', 'ping', 'defer', 'deferred')]
    [string]$Verb,

    [string]$LockName = '',
    [string]$Worktree = '',
    [string]$OwnerId = '',
    [string]$Fingerprint = '',
    [ValidateRange(60, 604800)]
    [int]$DeferSeconds = 21600
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'WorktreeCleanup.ps1')

# Store absolute paths so release works from any checkout of this repository.
if ($Worktree) { $Worktree = [IO.Path]::GetFullPath($Worktree) }

$redisContainer = 'tunit-agent-locks-redis'
$lockTtlSeconds = 7200 # 2 hours: the dead-man's switch. Longer than nearly every
# unit of work; a dead agent frees the item within this window. No periodic
# heartbeat — use `renew` only for the rare unit that runs past it.
$stateRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'tunit-agent-locks'

function Die([int]$code, [string]$msg) { if ($msg) { [Console]::Error.WriteLine($msg) }; exit $code }

function Get-MarkerPath([string]$Path) { return [IO.Path]::GetFullPath($Path.Trim()).TrimEnd('\', '/') }

function Set-WorktreeLockMarker([string]$Path) {
    # The main checkout is shared, never a work item's worktree, and new worktrees
    # inherit its config.worktree, so it never receives a marker.
    $gitDirectory = git -C $Path rev-parse --path-format=absolute --git-dir 2>$null
    if ($LASTEXITCODE -ne 0) { return }
    $commonDirectory = git -C $Path rev-parse --path-format=absolute --git-common-dir 2>$null
    if ($LASTEXITCODE -ne 0) { return }
    $top = git -C $Path rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $top -or $gitDirectory -eq $commonDirectory) {
        [Console]::Error.WriteLine("not writing lock marker: $Path is not a linked worktree")
        return
    }
    # Linked worktrees share --local config. Enable the extension in that common
    # config, then persist the marker in the target worktree's config.worktree file.
    git -C $Path config --local extensions.worktreeConfig true 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { return }
    git -C $Path config --worktree agent.lockName $LockName 2>$null | Out-Null
    git -C $Path config --worktree agent.lockWorktree (Get-MarkerPath $top) 2>$null | Out-Null
}

function Resolve-OwnerIdentity([string]$Explicit) {
    if (-not [string]::IsNullOrWhiteSpace($Explicit)) { return $Explicit.Trim() }
    if (-not [string]::IsNullOrWhiteSpace($env:TUNIT_AGENT_LOCK_OWNER_ID)) {
        return $env:TUNIT_AGENT_LOCK_OWNER_ID.Trim()
    }
    if (-not [string]::IsNullOrWhiteSpace($env:CODEX_THREAD_ID)) { return $env:CODEX_THREAD_ID.Trim() }
    return $null
}

function Resolve-LockName([string]$Explicit) {
    if ($Explicit) { return $Explicit }
    # The current worktree's marker, else issue-<N> from an issue-<N>-<desc> branch
    # (never pr-<N>). The sweep resolves every registered worktree the same way.
    $top = (git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $top) { return $null }
    try { return Get-WorktreeLockName -Worktree $top } catch { return $null }
}

if ($Verb -ne 'ping') {
    $LockName = Resolve-LockName $LockName
    if (-not $LockName) {
        Die 2 "cannot resolve lock name: pass -LockName (e.g. pr-1234 / issue-1234), or run inside an issue-<N>-* worktree"
    }
}

# defer/deferred record advisory survey state, not ownership, so they need no owner.
if ($Verb -in 'acquire', 'release', 'renew', 'status') {
    $OwnerId = Resolve-OwnerIdentity $OwnerId
    if (-not $OwnerId) {
        Die 2 'cannot resolve lock owner identity: pass -OwnerId or set TUNIT_AGENT_LOCK_OWNER_ID (CODEX_THREAD_ID is used automatically in Codex)'
    }

    $ownerIdBytes = [System.Text.Encoding]::UTF8.GetBytes($OwnerId)
    $ownerIdHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($ownerIdBytes)).ToLowerInvariant()
    $stateDir = Join-Path $stateRoot $ownerIdHash
    $tokenFile = Join-Path $stateDir ("{0}.token" -f ($LockName -replace '[\\/:*?"<>| ]', '_'))
}
$lockKey = "tunit:agent-lock:$LockName"
$metaKey = "tunit:agent-lock-meta:$LockName"
$deferKey = "tunit:agent-defer:$LockName"

function Ensure-AgentRedis {
    # One inspect tells missing vs stopped vs running: error exit = missing,
    # 'false' = stopped, 'true' = running. Warm path (the common loop case) costs
    # this single call — no separate `docker ps` or a redundant PING; the verb's
    # own Redis command is the connectivity check.
    $state = docker inspect -f '{{.State.Running}} {{join .Args " "}}' $redisContainer 2>$null
    $found = $LASTEXITCODE -eq 0
    if ($found -and $state -like 'true *') { return }

    # Locks and deferral records must survive a Docker restart, or a restart strips every
    # running unit of its lock. A stopped container holds no data in memory, so replacing
    # one created without the append-only file loses nothing. `docker rm` without -f
    # refuses a container that another caller has just started.
    if ($found -and ([string]$state -split ' ')[1] -eq 'redis-server' -and $state -notlike '*--appendonly yes*') {
        docker rm $redisContainer 2>$null | Out-Null
        $null = docker inspect -f '{{.State.Running}}' $redisContainer 2>$null
        $found = $LASTEXITCODE -eq 0
    }
    if (-not $found) {
        docker run -d --name $redisContainer --restart unless-stopped -v "${redisContainer}-data:/data" redis:7-alpine `
            redis-server --save '' --appendonly yes --appendfsync everysec 2>$null | Out-Null
    }
    else {
        docker start $redisContainer | Out-Null
    }
    # Cold start only: the container may not be answering yet, so confirm.
    if ((docker exec $redisContainer redis-cli PING) -ne 'PONG') {
        Die 1 "redis PING failed on $redisContainer"
    }
}

function Redis {
    $output = & docker exec $redisContainer redis-cli @args
    if ($LASTEXITCODE -ne 0) { Die 1 "redis command failed on $redisContainer" }
    ($output | Out-String).Trim()
}

function New-Meta([string]$Token) {
    @{
        lockVersion     = 4
        lockBackend     = 'redis'
        lockName        = $LockName
        token           = $Token
        pid             = $PID
        startedAt       = (Get-Date).ToString('o')
        lastRenewedAt   = (Get-Date).ToString('o')
        worktree        = $Worktree
    } | ConvertTo-Json -Compress
}

function Read-Token {
    $t = (Get-Content -LiteralPath $tokenFile -Raw -ErrorAction SilentlyContinue)
    if ($t) { $t.Trim() } else { $null }
}

if ($Verb -ne 'status') { Ensure-AgentRedis }

switch ($Verb) {

    'acquire' {
        $token = [Guid]::NewGuid().ToString('N')
        # Lock + meta set atomically in one round-trip: SET NX, then write meta only
        # if we won the lock. Returns 1 on acquire, 0 if already held.
        $script = "if redis.call('SET', KEYS[1], ARGV[1], 'NX', 'EX', ARGV[3]) then redis.call('SET', KEYS[2], ARGV[2], 'EX', ARGV[3]); return 1; else return 0; end"
        if ((Redis EVAL $script 2 $lockKey $metaKey $token (New-Meta $token) $lockTtlSeconds) -ne '1') { Die 3 'HELD' }
        New-Item -ItemType Directory -Force $stateDir | Out-Null
        Set-Content -LiteralPath $tokenFile -Value $token -NoNewline
        # If the worktree already exists at acquire time, write the auto-derive marker.
        if ($Worktree) { Set-WorktreeLockMarker $Worktree }
        Write-Output $token
        exit 0
    }

    'renew' {
        # OPTIONAL — not periodic. Re-arm the TTL for a unit that may outlive it, and/or
        # persist the auto-derive marker via -Worktree. Skip entirely for normal units.
        $token = Read-Token
        if (-not $token) { Die 1 "no cached token for $LockName" }
        # Re-arm TTL on lock + meta only if we still own the token. A free lock means Redis
        # lost the key (a restart, or a unit that ran past the TTL) and no one else took
        # the item, so take it back with the same token. Another owner's lock is LOST.
        $script = "local held = redis.call('GET', KEYS[1]); if held == ARGV[1] or (not held and redis.call('SET', KEYS[1], ARGV[1], 'NX', 'EX', ARGV[2])) then local meta = cjson.decode(ARGV[3]); local previous = redis.call('GET', KEYS[2]); if meta.worktree == '' and previous then meta.worktree = cjson.decode(previous).worktree; end; redis.call('EXPIRE', KEYS[1], ARGV[2]); redis.call('SET', KEYS[2], cjson.encode(meta), 'EX', ARGV[2]); return 1; else return 0; end"
        if ((Redis EVAL $script 2 $lockKey $metaKey $token $lockTtlSeconds (New-Meta $token)) -ne '1') {
            Die 4 'LOST'
        }
        if ($Worktree) { Set-WorktreeLockMarker $Worktree }
        exit 0
    }

    'release' {
        $token = Read-Token
        if (-not $token) { Die 1 "no cached token for $LockName" }
        # Verify ownership and renew the lease before touching the checkout. Keep the
        # lock held through removal so a new owner cannot reuse its path midway.
        $prepare = "if redis.call('GET', KEYS[1]) == ARGV[1] then redis.call('EXPIRE', KEYS[1], ARGV[2]); redis.call('EXPIRE', KEYS[2], ARGV[2]); return redis.call('GET', KEYS[2]) or '{}'; else return ''; end"
        $metadata = Redis EVAL $prepare 2 $lockKey $metaKey $token $lockTtlSeconds
        if (-not $metadata) {
            Remove-Item -LiteralPath $tokenFile -ErrorAction SilentlyContinue
            Die 5 'STALE'
        }
        try {
            $recordedWorktree = ($metadata | ConvertFrom-Json).worktree
            if ($recordedWorktree) {
                . (Join-Path $PSScriptRoot 'Remove-ReleasedWorktree.ps1')
                Remove-ReleasedWorktree -Worktree $recordedWorktree -LockName $LockName
            }
        } catch {
            # File locks, dirty trees and inspection failures must not strand ownership.
            [Console]::Error.WriteLine("Worktree retained: $($_.Exception.Message)")
        }
        # Delete lock + meta only if we still own the token; never clobber otherwise.
        $script = "if redis.call('GET', KEYS[1]) == ARGV[1] then redis.call('DEL', KEYS[2]); return redis.call('DEL', KEYS[1]); else return 0; end"
        $released = Redis EVAL $script 2 $lockKey $metaKey $token
        Remove-Item -LiteralPath $tokenFile -ErrorAction SilentlyContinue
        if ($released -ne '1') { Die 5 'STALE' }
        exit 0
    }

    'status' {
        $held = Redis GET $lockKey
        if (-not $held) { Write-Output 'FREE'; exit 0 }
        if ($held -eq (Read-Token)) { Write-Output 'HELD-BY-ME' } else { Write-Output 'HELD' }
        exit 0
    }

    'ping' {
        if ((Redis PING) -ne 'PONG') { Die 1 "redis PING failed on $redisContainer" }
        exit 0
    }

    'defer' {
        if (-not $Fingerprint) { Die 2 'defer requires -Fingerprint' }
        if ((Redis SET $deferKey $Fingerprint EX $DeferSeconds) -ne 'OK') { Die 1 "could not record deferral for $LockName" }
        exit 0
    }

    'deferred' {
        # Accepts a comma-separated list so a survey checks every candidate in one call.
        # Prints one `<lock name>=<fingerprint>` line per recorded deferral.
        $names = @($LockName -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        $keys = @($names | ForEach-Object { "tunit:agent-defer:$_" })
        $script = "local r = {}; for i, k in ipairs(KEYS) do local v = redis.call('GET', k); if v then r[#r + 1] = ARGV[i] .. '=' .. v; end; end; return r"
        $recorded = Redis EVAL $script $keys.Count @keys @names
        if ($recorded) { Write-Output $recorded }
        exit 0
    }
}
