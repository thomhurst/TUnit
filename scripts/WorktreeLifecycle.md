# Local worktree lifecycle

`AgentLocks.ps1 release` removes its recorded checkout before releasing ownership. Register the path once after checkout with `renew -LockName <name> -Worktree <absolute-path>`. Use the shared checkout's current script, and release in `finally` after stopping owned processes and saving evidence under the evidence root named in [AGENTS.md](../AGENTS.md#local-worktree-lifecycle). `Remove-MergedWorktrees.ps1` deletes the evidence of a closed item once no registered worktree resolves to it, holding a temporary `AgentLocks.ps1` lease on the item through deletion, and deletes any entry after the retention window. Release keeps the evidence of an open item. The sweep resolves a worktree's lock name with the same `Get-WorktreeLockName` (in `WorktreeCleanup.ps1`) that `AgentLocks.ps1` uses.

The worktree's `agent.lockName` marker counts only while its `agent.lockWorktree` names that same worktree: `git worktree add` copies the source checkout's per-worktree config, so a copied marker is ignored. The main checkout never receives a marker.

Local branches remain available for `git worktree add <path> <branch>`. Detached commits are retained as `retained-worktrees/<SHA>`. Uncommitted source, unknown ignored files, Git-locked worktrees, the main checkout and foreign repositories are preserved. Known generated output is disposable under `WorktreeCleanup.ps1`. Release prints why a checkout was retained.

A crashed process or an expired Redis TTL cannot run release. `Remove-MergedWorktrees.ps1` remains the conservative fallback for merged work. No GitHub Actions job manages local disk.

Run regression tests locally with PowerShell 7, Git and Docker:

```powershell
pwsh scripts/Test-ReleasedWorktreeCleanup.ps1
pwsh scripts/Test-AgentEvidenceCleanup.ps1   # no Docker needed
```
