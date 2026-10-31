# Split an over-budget PR

Read when a PR exceeds the rework budget in SKILL.md § Rework budget. Do not push more fixes to it; split it instead:

1. File one native sub-issue of the PR's linked issue per part that can merge on its own. When the linked issue is itself a child, file the parts as siblings under its parent and close it as superseded. Relationship commands: [repository guidance](repository.md). Each sub-issue names the files it owns, the blocking findings it must resolve, and the PR branch whose commits it can reuse, and carries `<!-- split-from-pr #<N> -->` in its body.
2. Post one comment on the PR listing the open blocking findings, the files that keep conflicting, and the sub-issues.
3. Close the PR without merging and keep its branch. Remove `in-progress` from the linked issue.
4. Release the lock. Later units pick up the sub-issues like any other issue.

Add `needs-human` only as a last resort, when either holds:

- The PR's linked issue body contains `<!-- split-from-pr #<N> -->` (the PR is already a split) and it is over budget again.
- The remaining blocker needs a decision no code or evidence can settle: a public API break, an upstream dependency change, a security trade-off.

Before adding `needs-human`, ensure the label exists. Post one comment stating the decision needed and the options, then release the lock. Surveys skip the PR while it carries `needs-human`; a human removes the label to return it to the queue.
