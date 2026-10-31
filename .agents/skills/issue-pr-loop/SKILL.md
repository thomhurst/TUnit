---
name: issue-pr-loop
description: "Run the GitHub issue/PR queue unattended when asked for continuous work or /issue-pr-loop. Handle reviews, CI, merges, recovery, and issue implementation. Not for handling one PR's feedback."
---

# Issue/PR Loop

Read the root agent instructions and [repository guidance](references/repository.md), which maps the repository skills this loop defers to: review handling, the merge gate, issue filing and relationships, and worktree lifecycle each have one home there, and this skill does not restate them. Keep this file identical across repositories; repository specifics belong in that reference. Reviewing or editing this skill does not start the loop. Stay within the user's authorized scope.

## Loop and completion

Run unattended. At each iteration, run `pwsh scripts/Remove-MergedWorktrees.ps1` from the shared checkout, survey the queue, then complete one unit in priority order:

1. Fix a red `main` ([Red main](#red-main)).
2. Merge an eligible PR.
3. Fix conflicts, failed CI, or review findings on an open PR and push.
4. Publish finished work stranded on a local `issue-<N>-*` branch that has no PR, or recover valuable changes from a preserved merged-PR worktree ([recovery](references/recovery.md)).
5. Tidy the issue queue: close a parent whose native sub-issues are all closed, and close an issue that `origin/main` already implements or that another issue supersedes.
6. Claim an available issue, implement its full scope, and open a PR. Choose in [Pick an issue](#pick-an-issue) order: highest priority tier first, pipeline health first within a tier.
7. When nothing above is actionable, run a [triage unit](#triage) instead of waiting.

After pushing or deferring, survey again. Pending CI or review means work another item; do not watch, sleep, poll one item, or schedule monitors while work is queueable. When only CI or review remains, periodically survey the whole queue and stay responsive.

Stop on the user's stop or pause, or when a fresh survey finds no queueable issue, recovery candidate, or actionable PR and the remaining work needs an external decision or dependency. Pending CI alone is not completion. Continue without blocking questions.

Preserve item IDs, worktree path, lock name and script, owned services, and unresolved validation across compaction.

### Resolve, do not defer

Every unit costs a full agent run, so a deferral that changes nothing is wasted. Before deferring, try to finish the item another way:

- **Blocker first.** When the blocker is an open issue or PR in this repository that you can work, release this item's lock, acquire the blocker's, and work the blocker in this unit; report the blocker's result. Defer only when the blocker is itself locked or needs a human decision or external dependency.
- **Already implemented.** When `origin/main` already delivers an issue, close it with a comment naming the commit or PR. When an open PR delivers it, make sure the PR body has `Closes #<N>`, then work the PR.
- **Finished parent.** When every native sub-issue of a parent is closed, check the parent's own acceptance criteria against `origin/main`; close it with evidence or implement what is left.
- **Stale issue.** When the body no longer matches the code (part of it shipped), rewrite it to the remaining work, then implement that.
- **Pushed and waiting.** A pushed fix that waits for CI, hosted generation, or review is `pushed fixes to #N` or `opened #N closing #M`, not `deferred`.
- **Waiting on a run.** When the item waits only for a workflow run that you or an earlier unit started, report `waiting #N: run <id>`; the driver returns the item when that run completes. Never add `on-hold` for a wait that ends by itself.
- **Superseded PR.** Close a PR that another open or merged PR replaces, or a defective or conflicting automated PR, and link the replacement. Closed PRs keep their history; an open PR is not a place to keep evidence.
- **Earlier deferral.** When the prompt quotes an earlier deferral reason, start from that blocker: check whether it still holds and try each route above before deferring again for the same reason.
- **Decide and document.** When an item offers options and one is reasonable and reversible, pick it, state why in the PR or issue, and continue; review catches a wrong choice faster than a parked item gets a human answer. Do not invent constraints the repository does not state: code that does not ship in a package (tools, tests, scripts, samples) has no compatibility contract. When acceptance criteria conflict with each other or with the code, rewrite them to the option you chose and say so.

Defer only for a reason no agent can remove, and name its category in the result:

| Category | Reason |
| --- | --- |
| `[human]` | a decision no evidence can settle: a break to a shipped public API outside a breaking milestone, a security trade-off, spending money, a choice the user reserved, or an issue the repository reserves for the owner ([repository guidance](references/repository.md)) |
| `[upstream]` | an upstream release, fix, or date |
| `[credentials]` | missing credentials or infrastructure access |
| `[locked]` | a lock held by another worker |

The driver gives an uncategorized deferral a short expiry. Record the blocker once, visibly, so later units do not re-examine an unchanged item:

- Prerequisite: add a native `blocked_by` dependency. The driver skips the item until the prerequisite closes.
- Human decision: add `needs-human` and state the decision, its options, and your recommendation. Do the work that does not depend on the answer first.
- External gate (a release date, an upstream fix): add `on-hold` (create the label if needed) and name the condition that removes it; remove it yourself once met. Never use `on-hold` for CI, reviews, workflow runs, or items in this repository. Before parking behind a release, do the parts that do not need it.

Name every blocker as `#N` in the result; the driver works an open blocker next and skips items whose blocker carries `needs-human` or `on-hold`. On an item whose blocker is unchanged, edit your existing blocker comment instead of posting another. Start every comment you post with `<!-- issue-pr-loop -->` so the driver can tell your comments from new information.

### Triage

When the driver assigns a triage or discovery unit, or nothing is actionable, follow [references/queue-units.md](references/queue-units.md) § Triage or § Discovery.

### Red main

Follow [references/queue-units.md](references/queue-units.md) § Red main, under the `main-ci` lock.

### Single-unit mode

When the invocation asks for single-unit mode ("run once", "single unit"), it overrides the loop. Run the cleanup script, survey, and complete exactly one unit in priority order. Release every lock you hold and stop owned services. Print one final line and stop:

```text
RESULT: merged #N | pushed fixes to #N | opened #N closing #M | reran main run <id> | waiting #N: run <id> | deferred #N [category]: reason | deferred main [category]: reason | failed #N: reason | triaged: summary | queue-empty
```

Do not survey again, start a second item, or wait for CI or reviews. When the survey finds nothing actionable now, including when only pending CI or reviews remain, run a triage unit instead; print `RESULT: queue-empty` only when triage finds every item current and audited. Record anything a later run needs, such as a defer reason, on the issue or PR, not in agent memory. `scripts/Start-IssuePrLoop.ps1` runs this mode repeatedly, with a fresh agent process per unit.

A lock held by another owner is not a unit: when `acquire` exits 3, try the next candidate. Report `deferred` only for an item you acquired and examined.

When the invocation assigns an item and says the driver holds its lock, skip the cleanup script, the survey, and `acquire`; the lock belongs to the owner variable the driver set. The assigned item is where the unit starts, not a limit: when moving it needs action on a linked item (a blocker, a replacement or superseded PR, a duplicate, its parent or children), acquire that item's lock and act on it in this unit. Do not start unrelated work. Re-check state before acting, because the driver's survey is mechanical. After a `deferred` or `failed` result the driver skips the item until its GitHub state changes, but a clear blocker comment is still required, because the driver's record is not visible to people.

### Context and delegation

Model latency grows with context size and dominates loop time. Keep each unit's context small.

- When the runtime can start subagents, invoking this loop authorizes one per unit. `CLAUDECODE=1` means Claude Code: use the Agent tool with a `general-purpose` subagent. `CODEX_THREAD_ID` set means Codex: use its subagent or thread tool. The coordinator surveys, chooses one item, and hands over the item ID, this skill in single-unit mode for that item, the shared checkout, and the canonical lock script path. The subagent acquires the lock and does the unit. Require a confirmed `RESULT:` value from the grammar above, then continue surveying. Keep only queue state in the coordinator, never diffs, logs, or review bodies.
- When the runtime lets you pick reasoning effort, use lower effort for mechanical units (merges, surveys, conflict-free rebases, disposition replies) and high effort for implementation, conflict resolution in shared code, and CI failure diagnosis.
- Without subagents, drop the previous item's details once it is pushed or deferred. Send long command output (builds, tests, `gh api` dumps, logs) to a file and read only the lines you need; use `--jq` to select fields.

## Isolation and ownership

Use the shared checkout only for surveys and setup; every implementation, validation, checkout, commit, and push runs in an isolated worktree. Worktree creation, sparse-checkout, location checks, push refspec, hazards, evidence location, and cleanup rules: the worktree skill in [repository guidance](references/repository.md). Capture from the shared checkout:

```powershell
$repo = git rev-parse --show-toplevel
$repoSlug = gh repo view --json nameWithOwner --jq .nameWithOwner
$agentLocks = Join-Path $repo 'scripts/AgentLocks.ps1'
```

Acquire the Redis lock before acting on an item: `pr-<N>` for PR work or recovery, `issue-<N>` for new implementation. Invoke the absolute shared-checkout `$agentLocks` for every verb; older branch copies can have incompatible token caches. The contract's source is the script header.

| Command | Result |
| --- | --- |
| `pwsh $agentLocks acquire -LockName $lockName` | 0 grants ownership; 3 (`HELD`) means skip; any other failure means defer, never bypass Redis |
| `pwsh $agentLocks renew -LockName $lockName` | Only for work approaching the two-hour TTL; no periodic heartbeats. A `FREE` lock is taken back (after a Redis restart): re-fetch the item's state before pushing. 4 (`LOST`) means another owner holds it: stop the item and do not push |
| `pwsh $agentLocks status -LockName $lockName` | `FREE` / `HELD` / `HELD-BY-ME`; verify before pushing or merging. On `FREE`, `renew` to take it back |
| `pwsh $agentLocks release -LockName $lockName` | 0 confirms release; 5 (`STALE`) means stale ownership: leave the key alone |

Redis is authoritative: never take a lock on the evidence of PIDs, files, or inactivity, and never add another backend. Do not print or manage cached tokens. Owner identity: Codex supplies `CODEX_THREAD_ID`, and `scripts/Start-IssuePrLoop.ps1` sets the owner variable for each run. In an interactive Claude Code session pass `-OwnerId $env:CLAUDE_CODE_SESSION_ID` on every verb; other automation passes the same stable `-OwnerId` on every verb. A missing identity fails closed. The `in-progress` label persists independently of the lock.

Worktrees:

- Issue work: branch and worktree `issue-<N>-<short-desc>` from freshly fetched `origin/main`. Choose a unique path when one already exists.
- PR work: create a detached worktree from `origin/main`, then run `gh pr checkout <N>` with that worktree as the working directory. Name the directory `pr-<N>-<description>`; never rename or reuse it for another PR. A separate local review or rebase branch keeps the same `pr-<N>` identity.
- Immediately after checkout, run `pwsh $agentLocks renew -LockName $lockName -Worktree $worktree` once to register the path.
- Set every mutating tool call's working directory explicitly. Before the first edit and after each checkout change, check that `git rev-parse --show-toplevel` is the intended worktree and inspect `git status --short --branch`; abort on a mismatch.
- Release in `finally`, from a command whose working directory is the shared checkout, after stopping owned processes and saving evidence. Delete raw captures (traces, span exports, dumps) from the evidence entry once their numbers are in the issue or PR. On Windows a shell inside the worktree blocks its deletion; run `Merge-Pr.ps1` the same way. Release removes clean checkouts even for open PRs; branches and detached commits stay available. Never `git worktree lock` a checkout to keep it (except the temporary setup lock): commit and push what you need. Preservation and crash behavior: [scripts/WorktreeLifecycle.md](../../../scripts/WorktreeLifecycle.md).
- Never enable stale or scratch cleanup (`-StaleDays`) during a survey.

## Maintain PRs

Survey with `gh pr list --author @me --state open --json number,title,headRefName,headRefOid,mergeable,mergeStateStatus,reviewDecision,statusCheckRollup,isDraft`. Include other authors only when the user's queue scope does. Paginate or raise limits so an incomplete first page cannot make the queue look empty. Include drafts; skip PRs labelled `needs-human` or `on-hold`.

| State | Action |
| --- | --- |
| Conflicts | Rebase onto fresh `origin/main`, inspect the diff for lost changes at shared insertion points, validate, and push with `--force-with-lease`. Abort and defer conflicts you cannot resolve confidently. |
| CI failure | Read the failed logs and fix the cause; investigate timing and synchronization failures. At most one rerun, for a demonstrated infrastructure problem or a diagnosed flake where repository rules permit; never rerun for green, skip or delete failing tests, or mask a failure. |
| Stale CI | When failed checks ran on a base far behind `origin/main`, or the rerun allowance for an infrastructure failure is spent, rebase onto fresh `origin/main` and push for a fresh run. This push is not a rerun; an unchanged head never leaves deferral. A failure that repeats on the fresh base is real. |
| Feedback | Run the review skill's full cycle (fetch every surface, triage, fix, one push, reply, resolve) under the PR lock. Only blocking findings get a push; a follow-up or nit with a disposition does not block the merge. After resolving, re-fetch reviews, threads, checks, and head SHA. |
| Draft | Follow [Draft review lifecycle](#draft-review-lifecycle), including requesting missing reviews and marking ready once approved. |
| Pending | Take another item; reassess in a later iteration. |
| Merge candidate | Follow [Merge gate](#merge-gate). |

### Draft review lifecycle

Open every PR created by this loop as a draft (`gh pr create --draft`), including recovery and split PRs. Keep it in draft while reviewing and fixing findings. This lifecycle applies when the loop is invoked; it does not change PR defaults for unrelated work.

1. After creation and each draft push, re-fetch `isDraft` and `headRefOid`. Claude's workflow reviews drafts automatically. Request Codex manually only while `isDraft` is true: post `@codex review` using `gh pr comment <N> --body-file <path>`, starting with `<!-- issue-pr-loop -->` and a separate `<!-- codex-review:<full-head-SHA> -->` marker. Fetch all issue comments first; do not duplicate a pending request or request a review when this head already has a positive Codex verdict. A request is pending for 15 minutes after its `createdAt`; if Codex has posted no review, comment, or thumbs-up for this head by then, the request was dropped. An eyes reaction does not count: Codex often adds one and then never reviews. Post a fresh request with the same markers and keep waiting. Codex silence never ends the unit or the loop; report the PR as waiting (or take another item) and re-check it on the next visit. If a disposition needs reviewer confirmation without a new head, request a follow-up linking that disposition, then wait for its response. Never tag Codex on a ready PR; rely on its automatic review there.
2. Read every review surface using the review skill: reviews, inline review threads, and PR conversation comments. Claude often posts its review as a conversation comment, so a missing `claude[bot]` review does not mean Claude has not reviewed. A successful check, a request, an eyes reaction, silence, or an approval for an older head is not approval. Require completed, positive verdicts from both Claude and Codex for the current head. A clean `COMMENTED` review with an explicit no-blockers verdict or a clean Codex review receipt identifying this head counts; so does a Codex thumbs-up on this head's request, after verifying the reaction author through the comment reactions API. Honor any formal approval required by repository rules; do not approve your own PR or manufacture reviewer approval.
3. Dispose of findings through the review skill, leaving no blocking or undecided findings and no unresolved threads. After a fix changes the head, keep the draft, wait for fresh Claude review, and request Codex once for the new head, re-requesting only after the 15-minute timeout in step 1. Do not reuse the earlier head's approval. Pending review is a wait, not a reason to park or abandon the PR.
4. Under the PR lock, re-fetch the head, draft state, verdicts, threads, and checks. [Repository guidance](references/repository.md) says whether CI runs on drafts; where it does not, skipped or absent draft CI is expected, not a blocker to promotion. When both reviewers approve the same current head and findings are dispositioned, run `gh pr ready <N>` and verify `isDraft` is false and the head is unchanged. Do not mark ready while known CI failures or conflicts remain. If the head changes during promotion, restore draft with `gh pr ready <N> --undo` and review the new head.
5. Arm auto-merge as soon as the PR is ready; CI proves the build. Run `pwsh scripts/Enable-AutoMerge.ps1 -Pr <N>` without waiting for CI and take another item. When marking ready re-runs Claude review ([repository guidance](references/repository.md)), the gate waits for that run to pass; read its findings, then arm. Never arm auto-merge while draft. If CI then fails, auto-merge holds; fix it as a CI failure, and re-arm after the new head's review.

### Rework budget

Keep working a PR until it merges, but a PR that keeps getting new heads may never converge, since each push needs a new review and disposition. Before working a PR, count its automated review rounds (the counting command is in [repository guidance](references/repository.md)) and its force-pushes (`head_ref_force_pushed` events in `issues/<N>/timeline`). It is over budget when either holds:

- 20 or more automated review rounds, and the latest review still has a blocking finding.
- 10 or more force-pushes after it first had conflicts.

Do not push more fixes to an over-budget PR: split it per [references/rework-budget.md](references/rework-budget.md). Keep new PRs small enough to converge: when an issue's change spans separate subsystems, split it into sub-issues ([Pick an issue](#pick-an-issue)) instead of opening one large PR.

### Merge gate

The merge protocol (fresh mechanical gate, reading every review body and comment, the wrapper, auto-merge arming and its tier interaction) has one home: the change-control skill in [repository guidance](references/repository.md). Loop-specific rules:

- Run `pwsh scripts/Merge-Pr.ps1 -Pr <N> -Worktree $worktree` from the shared checkout; omit `-Worktree` only when none exists. The script is necessary but not sufficient: judge review dispositions yourself, including any reviewer reply newer than your disposition on a resolved thread.
- Arm auto-merge with `Enable-AutoMerge.ps1` rather than waiting for CI. Use `Merge-Pr.ps1` only when every check has already passed on the current head.
- A nonzero exit means inspect or defer, never retry blindly. Cleanup warnings after a successful merge do not justify a retry; reconcile an ambiguous result with GitHub state. Never delete a fork's branch through the base repository's `origin`.
- Never infer that approval is optional from historical unapproved merges, or that no protection exists from an inaccessible protection API; read the current ruleset.

## Pick an issue

Ensure the `in-progress` label exists, then list open, unassigned candidates without it:

```powershell
gh issue list --state open --search 'no:assignee -label:in-progress' --json number,title,labels,body --limit 1000
```

Skip `wontfix`, `duplicate`, `question`, `needs-human`, `on-hold`, claimed items, issues with open native sub-issues or open `blocked_by` dependencies, and issues with an open closing PR. Take the remaining issues strictly by priority tier: `priority:critical`, then `priority:high`, then `priority:medium` (and unlabelled issues), then `priority:low`; never take a lower tier while a higher one has a claimable issue. Within a tier, take `ci` (pipeline-health) issues first, since every later PR pays for them; then issues that block other issues; then clear, smaller tasks with fewer discussion comments; size alone is not a reason to stop when larger coherent work remains. `scripts/Start-IssuePrLoop.ps1` applies the same order when it assigns units.

Under the issue lock, re-fetch state, assignees, labels, native dependencies, and linked PRs. Skip closed, assigned, or actively claimed items. Work an open blocker instead of a blocked item, and close an already implemented item with evidence ([Resolve, do not defer](#resolve-do-not-defer)). An `in-progress` claim with no open PR and no lock holder is stale: find its branch or worktree and publish that work, or remove the claim and implement the issue. Add `in-progress` and verify the claim before branching; keep it while the PR is open. Remove only your own claim if abandoning before PR creation; after lost ownership, leave shared claim state untouched and report the loss.

Establish full scope from source, tests, and docs; a missing API may be the requested work. Deliver a coherent issue in one PR.

Split into native sub-issues and `blocked_by` relationships only for independent deliverables, after inspecting existing children (closed ones too). Never track by checklist or `blocked` label. Each part must merge on its own as one PR, and a child is never split again: when the issue is already a child, deliver it in one PR or move what is left into sibling issues of the same parent. Do not file a child whose only work is to wait for, re-run, or re-validate another child's output. Give children acceptance criteria, leave the parent open, remove your parent claim, and implement the first unclaimed child whose prerequisites are closed. Relationship write commands (numeric REST `.id` trap): the change-control skill in [repository guidance](references/repository.md). Verify each write before proceeding; if native links are unavailable, document the blocker in the issue.

## Implement and ship

Follow the repository's validation, TDD, review, commit, and PR rules from the root instructions. Loop-specific rules:

- Document validation you could not run; never silently raise guard limits or claim unrun tests passed. Use CI for environment-specific validation, with the limitation stated; avoid arbitrary local retry counts.
- Never let git open an editor: commit with `-m`, `--file`, or `--amend --no-edit`, and run rebases as `git -c core.editor=true -c sequence.editor=true rebase ...`.
- Reference the issue in commits. Open draft PRs and follow [Draft review lifecycle](#draft-review-lifecycle); use `--body-file` for multiline bodies. Put `Closes #<N>` on its own line only when the full issue is delivered.
- Push new commits normally; use `git push --force-with-lease` only to rewrite an owned PR branch. Never bare `--force`, never rewrite `main`.
- Completion requires commit, successful push, PR creation or update, and `gh pr view <N> --json headRefOid` matching local `git rev-parse HEAD`. On a push or ownership failure, preserve the work, report the failure, and continue. Keep committed work on its `issue-<N>-*` branch: the driver finds unpublished branches and assigns them for publishing. Release the lock in every outcome.
- File out-of-scope findings per the repository's filing rule, then finish the original scope.
