# Triage, discovery, and red-main units

Read when the driver assigns a triage, discovery, or red-main unit, or when the loop reaches one of these units on its own.

## Triage

A triage unit keeps the queue accurate, so other units spend their time on real work. The driver runs one when nothing is actionable, and also every few units. It names a batch of items that no unit audited recently, parked and blocked items first. Without the driver, pick the oldest open issues that carry `needs-human`, `on-hold`, or open `blocked_by` dependencies, then the rest.

For each item, acquire its lock (`issue-<N>` or `pr-<N>`) and skip it when `acquire` exits 3. Check it against fresh `origin/main` and its linked issues and PRs:

- Close an issue that `origin/main` already implements, or that another issue or PR supersedes. Name the commit, PR, or issue in the closing comment.
- Rewrite the body of an issue that no longer matches the code, so that it describes only the remaining work and has acceptance criteria.
- Add missing native sub-issue and `blocked_by` links; remove links to closed or unrelated items.
- Remove `on-hold` when its condition is met (the release shipped, the run completed). Remove `in-progress` when no open PR, lock holder, or branch backs it.
- For `needs-human`, check whether a comment since the label was added answers the question. When it does, apply the answer, remove the label, and say so.
- For `needs-human` or `question` with a recommended option that is reasonable and reversible, apply **Decide and document** (SKILL.md): take that option, remove the label, and say so. Exception: never remove `needs-human` from an issue the repository reserves for the owner ([repository guidance](repository.md)); only the owner approves it.
- Close a superseded, defective, or conflicting automated PR and link its replacement.
- Flatten issue chains. A sub-issue tree deeper than parent and child, or a parent with one open child, hides work from the queue: move the remaining acceptance criteria into one issue and close the rest as superseded. Close a child whose only work is to wait, re-run, or re-validate what another issue or an automated PR already delivers.
- For a parent whose open children are all parked or blocked, check each child's blocker. Unpark the children whose condition is met, and move work that does not need the blocker into an issue that can be claimed now.

Make only changes the evidence supports, and keep comments short. Do not implement an issue in a triage unit.

When the prompt says fewer than five issues can be claimed now, also file up to three new issues using the discovery sources below.

Report `RESULT: triaged: <summary>`, for example `triaged: closed #12 #15, updated #18, filed #40`, or `triaged: no changes` when every item was current.

## Discovery

The driver runs a discovery unit when nothing is actionable and every item was audited recently. Find real work and file it as issues a unit can claim now. Look, in order, at: failed or flaky jobs in recent CI runs on `main` and on PRs; open milestone issues whose acceptance criteria are not yet split into claimable work; analyzer and build warnings; `TODO`/`FIXME` comments that describe a defect; documentation that contradicts the code; public behavior without tests.

File at most five issues. Search open and closed issues first. Each issue states the evidence (file and line, run, or log line), the expected behavior, and acceptance criteria, and fits one PR. Apply the repository's filing bar; never file style preferences or speculative features. Report `RESULT: triaged: filed #A #B` or `triaged: no changes`.

## Red main

`main` is red when the latest completed push or dispatched run of any push workflow on `main` failed. A red `main` fails every PR for the same reason, so it comes before PR work. Work it under the `main-ci` lock.

1. Read the failed jobs' logs. Send them to a file and read only the failing lines.
2. Search open issues and PRs for this failure. When an open PR fixes it, release `main-ci` and work that PR. When an open issue tracks it, work that issue. When a merged commit already fixes it but the workflow's push path filter skipped that commit, dispatch the workflow on `main` with `gh workflow run <file> --ref main` and report `waiting #<fix PR>: run <id>`.
3. When the failure is infrastructure (a lost runner, a network error, a rate limit), rerun the whole run once with `gh run rerun <id>` and report `reran main run <id>`. Never rerun a run that was already rerun; treat a repeat failure as real.
4. Otherwise file an issue with the failing run, step, and error, labelled `bug`. Claim it, fix it in an `issue-<N>-*` worktree, and open a PR that closes it. Never push to `main` directly.
5. When no agent can fix the cause, file the issue with `needs-human` and report `deferred main [human]: reason` naming the issue. The driver re-checks `main` when that issue closes or a new run finishes.
