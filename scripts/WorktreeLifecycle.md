# Local worktree lifecycle

`AgentLocks.ps1 release` removes its recorded checkout before releasing ownership. Register the path once after checkout with `renew -LockName <name> -Worktree <absolute-path>`. Use the shared checkout's current script, and release in `finally` after stopping owned processes and saving evidence under the evidence root named in [AGENTS.md](../AGENTS.md#local-worktree-lifecycle). `Remove-MergedWorktrees.ps1` deletes the evidence of a closed item once no registered worktree resolves to it, holding a temporary `AgentLocks.ps1` lease on the item through deletion, and deletes any entry after the retention window. Release keeps the evidence of an open item. The sweep resolves a worktree's lock name with the same `Get-WorktreeLockName` (in `WorktreeCleanup.ps1`) that `AgentLocks.ps1` uses.

The worktree's `agent.lockName` marker counts only while its `agent.lockWorktree` names that same worktree: `git worktree add` copies the source checkout's per-worktree config, so a copied marker is ignored. The main checkout never receives a marker.

Local branches remain available for `git worktree add <path> <branch>`. Detached commits are retained as `retained-worktrees/<SHA>`. Uncommitted source, unknown ignored files, Git-locked worktrees, the main checkout and foreign repositories are preserved. Known generated output is disposable under `WorktreeCleanup.ps1`. Release prints why a checkout was retained.

A crashed process or an expired Redis TTL cannot run release. `Remove-MergedWorktrees.ps1` remains the conservative fallback for merged work. It also removes the checkout of a closed `pr-<N>` or `issue-<N>` item that no merge signal matches, such as a review branch whose name differs from the PR head, a branch with local commits after its merge, or a PR closed unmerged. The item comes from `Get-WorktreeLockName`, else a `pr-<N>-*` or `issue-<N>-*` directory name. The sweep holds a temporary `AgentLocks.ps1` lease on the item through removal, keeps the branch, saves uncommitted changes to `retained-worktrees/<item>-wip-<sha>` (restore with `git stash apply <branch>`), retains a detached HEAD as `retained-worktrees/<SHA>`, and preserves a checkout with unknown ignored files. A merged checkout with leftover uncommitted changes is removed the same way, under the same lease, its changes saved to `retained-worktrees/<item>-wip-<sha>`. It is kept when its lock is held, Redis is unavailable, or it has no item identity. A clean merged checkout is removed without a lease, so a Redis outage never keeps it. A detached checkout of a main commit with changes is still preserved. No GitHub Actions job manages local disk.

Run regression tests locally with PowerShell 7, Git and Docker:

```powershell
pwsh scripts/Test-ReleasedWorktreeCleanup.ps1
pwsh scripts/Test-AgentEvidenceCleanup.ps1   # no Docker needed
pwsh scripts/Test-RemoveMergedWorktrees.ps1   # no Docker needed
```
