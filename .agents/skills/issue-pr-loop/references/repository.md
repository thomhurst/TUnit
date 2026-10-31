# TUnit

Read [AGENTS.md](../../../../AGENTS.md) first; it is canon for validation, snapshots, AOT, and git workflow. Build and test commands: [workflows](../../../../.claude/docs/workflows.md). The mechanics this loop defers to:

| Need | Owner |
|---|---|
| One PR's review cycle: fetch every surface, verify findings, fix, one push, reply, resolve and unresolve threads | `AGENTS.md` § Git workflow |
| Merge protocol | [Merge gate](#merge-gate) below |
| Out-of-scope filing bar and procedure | `AGENTS.md` § Autonomous self-improvement |
| Native sub-issue and `blocked_by` writes | [Issue relationships](#issue-relationships) below |
| Worktree location, lock registration, evidence directory, cleanup | `AGENTS.md` § Local worktree lifecycle and [WorktreeLifecycle.md](../../../../scripts/WorktreeLifecycle.md) |
| Validation, snapshots, `TUnit.TestProject` filters | `AGENTS.md` § Validation |

Lock owner variable: `TUNIT_AGENT_LOCK_OWNER_ID` (read by `scripts/AgentLocks.ps1` after `-OwnerId`, before `CODEX_THREAD_ID`).

## Owner-reserved decisions

TUnit ships public NuGet packages. Defer as `[human]` with `needs-human`: a breaking change to a shipped public API (anything in the `tests/TUnit.PublicAPI` snapshots) outside a planned major release, dropping a target framework, giving up Native AOT or trimming compatibility, adding a new package, or a new dependency in a shipped package. Code that does not ship (tests, scripts, benchmarks, docs tooling) has no compatibility contract.

## Draft review lifecycle

`AGENTS.md` opens PRs ready for review by default. Inside the loop, the skill's draft review lifecycle applies to every PR the loop opens. Outside the loop, follow `AGENTS.md`.

Required reviewers are Claude Code Review and Codex. CodeRabbit and Greptile are not approvers, but their findings are review surfaces like any other.

- **Claude:** `.github/workflows/claude-code-review.yml` (job `claude-review`) runs on `opened`, `synchronize`, `ready_for_review`, and `reopened`, drafts included. It posts its review as a `github-actions[bot]` PR conversation comment with no head marker; the `claude-review` check on the current head ties the review to that head. Marking ready re-runs it, so after `gh pr ready` wait for that run to pass and read its comment before arming. When a head has no review (a failed or cancelled run), dispatch it once: `gh workflow run claude-code-review.yml -f pr_number=<N>`. The workflow runs on `pull_request_target`, so it always uses `main`'s workflow file.
- **Codex:** `chatgpt-codex-connector[bot]` reviews automatically when a PR opens ready or leaves draft, and on drafts only on request. Its mutable `<!-- codex-pull-request-review-summary -->` conversation comment carries `<!-- codex-review-state:v1 {...} -->`; a positive verdict has `code_review.commit_sha` equal to the full current head, `status: completed`, and `has_any_findings: false`. Its `<!-- codex-review-receipt:v1 -->` reviews name the reviewed commit. A thumbs-up from `chatgpt-codex-connector[bot]` on this head's request (verify the author through `issues/comments/<id>/reactions`) also counts. Re-read the summary on each visit; never infer the reviewed commit from timing.

Automated review rounds (rework budget): `gh api --paginate "repos/{owner}/{repo}/issues/<N>/comments" --jq '.[] | select(.user.login == "github-actions[bot]") | .id' | wc -l`.

Draft CI: `.NET`, `AOT`, and the other PR workflows run on drafts as well as ready PRs, so a draft head already has CI results when it is promoted.

## Merge gate

`main` branch protection requires `modularpipeline (ubuntu-latest)`, `modularpipeline (macos-latest)`, and resolved conversations, with no required approvals. It cannot read review bodies, so the scripts and you cover the rest:

- `scripts/Assert-PrGreen.ps1 -Pr <N>` is the read-only gate. It requires an open, ready, `CLEAN` PR, every current check green (not only the required ones), a passing `claude-review` check on the head, and no unresolved threads. `-ForAutoMerge` lets CI checks be pending but still requires `claude-review` to have passed.
- `scripts/Enable-AutoMerge.ps1 -Pr <N>` runs the `-ForAutoMerge` gate and arms squash auto-merge pinned to the head. GitHub merges once the required checks pass.
- `scripts/Merge-Pr.ps1 -Pr <N> [-Worktree <path>]` runs the full gate, squash-merges pinned to the head, removes the PR's worktree, and deletes its local branch. GitHub deletes the remote branch on merge.
- Before either script, read every review body, inline thread, and conversation comment on the current head, and disposition each finding per `AGENTS.md` § Git workflow. The scripts are necessary but not sufficient.
- Renovate PRs (`renovate/*` branches) and Dependabot PRs get no Claude review by design, so both scripts deny them. Leave them to Renovate; act on one only to fix its failing CI.
- GitHub keeps auto-merge armed when the owner pushes. Before pushing to an armed PR, run `gh pr merge <N> --disable-auto`; re-arm only after the new head's review.

## Issue relationships

Native relationships take the REST numeric issue `.id`, not the issue number. Pass it with `-F` so it is sent as an integer:

```powershell
$childId = gh api "repos/{owner}/{repo}/issues/<child>" --jq .id
gh api "repos/{owner}/{repo}/issues/<parent>/sub_issues" -X POST -F "sub_issue_id=$childId"

$blockerId = gh api "repos/{owner}/{repo}/issues/<blocker>" --jq .id
gh api "repos/{owner}/{repo}/issues/<blocked>/dependencies/blocked_by" -X POST -F "issue_id=$blockerId"
```

Verify each write with `gh api "repos/{owner}/{repo}/issues/<parent>/sub_issues"` or `gh api "repos/{owner}/{repo}/issues/<blocked>/dependencies/blocked_by"`.
