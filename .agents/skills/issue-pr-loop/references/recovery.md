# Recover preserved merged-worktree changes

Use this procedure only when a cleanup survey preserves a merged-PR worktree with potentially valuable changes. Prioritize actionable open PRs first. Recovery is not permission to discard another agent's work.

1. Verify the original merged PR identity and acquire `pr-<original-N>` through the canonical lock script. Confirm no active agent, open PR, or harness owns the worktree; otherwise leave it untouched.
2. Compare tracked changes, untracked source, and local-only commits against both the original PR and fresh `origin/main`. Identify coherent, unique source/test changes separately from obsolete or generated churn. Search for an existing recovery PR/issue before creating another.
3. Preserve mixed, unclear, sensitive, or untestable changes. When authorized, record a recovery issue with the original PR, worktree path/branch, and a non-sensitive description; do not publish secrets or speculative changes.
4. Before modifying or removing the old worktree, preserve its exact valuable edits and local-only commits in a local snapshot branch/commit or another verified backup. Do not blindly stage secrets, unrelated user files, or generated artifacts.
5. Create a separate worktree from current `origin/main` and transplant only the reviewed diff. Publishing the old squash-merged branch can replay already-merged history. Split unrelated edits, omit already-merged changes, and regenerate generated files from their sources.
6. Validate the recovered change under the repository's normal requirements, review the complete diff, and open a draft PR with `Follow-up to #<original-N>` explaining the recovered intent. Follow the skill's draft review lifecycle. Confirm the remote PR head matches local HEAD.
7. Remove the old worktree/snapshot only after every valuable change is accounted for in the confirmed PR or retained backup, the path is verified, and repository cleanup rules permit removal. Otherwise preserve them. Release ownership in every outcome.

## Publish a stranded issue branch

The driver assigns a local `issue-<N>-*` branch when it has commits that are not on `origin/main`, its issue is open, and no PR, open or closed, has ever used the branch. Such a branch usually holds finished work that a unit could not push, for example because the lock service hung.

1. Work under the `issue-<N>` lock. Inspect the branch's commits against fresh `origin/main` and read the issue's latest comments for the reason the work stopped.
2. When `origin/main` already contains the change, or the issue no longer needs it, delete the local branch and report the result. Close the issue with evidence when it is fully delivered.
3. Otherwise check the branch out in a new worktree, rebase it onto fresh `origin/main`, finish any missing scope, validate it under the repository's normal requirements, and open a draft PR. Follow the skill's draft review lifecycle. Report `opened #PR closing #N`.
4. When the issue is blocked by another open issue, still push the branch so that the work is safe. Open the PR when the work can merge on its own. Otherwise comment the branch name on the issue and defer, naming the blocker.
