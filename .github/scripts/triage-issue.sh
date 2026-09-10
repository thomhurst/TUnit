#!/usr/bin/env bash
# Label-and-comment helper for the Issue Triage workflow.
#
# Triage runs on issues opened by anyone, so the issue text reaching the model is
# untrusted. This script is the only write path exposed to it: the issue number is
# pinned from the environment (never an argument), so an injected instruction cannot
# retarget another issue, and only --add-label / a comment body are reachable -
# `gh issue edit --body/--title/--add-assignee` are not.
#
# Usage:
#   triage-issue.sh label "bug,priority:high"
#   triage-issue.sh comment "Looks like a duplicate of #123"
set -euo pipefail

: "${ISSUE_NUMBER:?ISSUE_NUMBER must be set by the workflow}"
: "${GH_REPO:?GH_REPO must be set by the workflow}"

action=${1:-}
value=${2:-}

if [[ -z $value ]]; then
  echo "usage: $0 {label|comment} <value>" >&2
  exit 2
fi

case $action in
  label)
    if [[ ! $value =~ ^[A-Za-z0-9][A-Za-z0-9\ ._:/-]*(,[A-Za-z0-9][A-Za-z0-9\ ._:/-]*)*$ ]]; then
      echo "refusing label list with unexpected characters: $value" >&2
      exit 2
    fi
    gh issue edit "$ISSUE_NUMBER" --repo "$GH_REPO" --add-label "$value"
    ;;
  comment)
    gh issue comment "$ISSUE_NUMBER" --repo "$GH_REPO" --body "$value"
    ;;
  *)
    echo "unknown action: $action" >&2
    exit 2
    ;;
esac
