# Pick an issue

Read before claiming an issue, or when an issue needs splitting.

Ensure the `in-progress` label exists, then list open, unassigned candidates without it:

```powershell
gh issue list --state open --search 'no:assignee -label:in-progress' --json number,title,labels,body --limit 1000
```

Skip `wontfix`, `duplicate`, `question`, `needs-human`, `on-hold`, claimed items, issues with open native sub-issues or open `blocked_by` dependencies, and issues with an open closing PR. Take the remaining issues strictly by priority tier: `priority:critical`, then `priority:high`, then `priority:medium` (and unlabelled issues), then `priority:low`, unless [repository guidance](repository.md) defines other labels; never take a lower tier while a higher one has a claimable issue. Within a tier, take pipeline-health issues (`ci`, or the label repository guidance names) first, since every later PR pays for them; then issues that block other issues; then clear, smaller tasks with fewer discussion comments; size alone is not a reason to stop when larger coherent work remains.

Under the issue lock, re-fetch state, assignees, labels, native dependencies, and linked PRs. Skip closed, assigned, or actively claimed items. Work an open blocker instead of a blocked item, and close an already implemented item with evidence (SKILL.md § Resolve, do not defer). An `in-progress` claim with no open PR and no lock holder is stale: find its branch or worktree and publish that work, or remove the claim and implement the issue. Add `in-progress` and verify the claim before branching; keep it while the PR is open. Remove only your own claim if abandoning before PR creation; after lost ownership, leave shared claim state untouched and report the loss.

Establish full scope from source, tests, and docs; a missing API may be the requested work. Deliver a coherent issue in one PR.

Split into native sub-issues and `blocked_by` relationships only for independent deliverables, after inspecting existing children (closed ones too). Never track by checklist or `blocked` label. Each part must merge on its own as one PR, and a child is never split again: when the issue is already a child, deliver it in one PR or move what is left into sibling issues of the same parent. Do not file a child whose only work is to wait for, re-run, or re-validate another child's output. Give children acceptance criteria, leave the parent open, remove your parent claim, and implement the first unclaimed child whose prerequisites are closed. Relationship write commands (numeric REST `.id` trap): [repository guidance](repository.md). Verify each write before proceeding; if native links are unavailable, document the blocker in the issue.
