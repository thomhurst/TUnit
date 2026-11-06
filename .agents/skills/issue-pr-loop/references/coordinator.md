# Coordinator

Read when running the loop in a runtime that can start subagents.

`CLAUDECODE=1` means Claude Code: use the Agent tool with a `general-purpose` subagent. `CODEX_THREAD_ID` set means Codex: use its subagent or thread tool. The coordinator surveys, chooses one item, and hands over the item ID, this skill in single-unit mode for that item, the shared checkout, and the canonical lock script path. The subagent acquires the lock and does the unit. Require a confirmed value from the `RESULT:` grammar in SKILL.md § Single-unit mode, then continue surveying. Keep only queue state in the coordinator, never diffs, logs, or review bodies.
