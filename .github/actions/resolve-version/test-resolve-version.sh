#!/usr/bin/env bash
# Checks which release tags resolve-version.sh accepts, and the versions it derives from them.
set -uo pipefail
script="$(dirname "$0")/resolve-version.sh"
out=$(mktemp)
failures=0

run() {
  : > "$out"
  GITHUB_EVENT_NAME=release RELEASE_TAG="$1" GITHUB_SHA=0123abc GITHUB_ENV=/dev/null GITHUB_OUTPUT="$out" \
    bash "$script" > /dev/null 2>&1
}

accept() {
  if ! run "$1"; then
    echo "FAIL: '$1' was rejected"; failures=$((failures + 1)); return
  fi
  local semver assembly
  semver=$(sed -n 's/^semVer=//p' "$out")
  assembly=$(sed -n 's/^assemblySemVer=//p' "$out")
  if [ "$semver" != "$2" ] || [ "$assembly" != "$3" ]; then
    echo "FAIL: '$1' gave $semver / $assembly, expected $2 / $3"; failures=$((failures + 1))
  fi
}

reject() {
  if run "$1"; then
    echo "FAIL: '$1' was accepted"; failures=$((failures + 1))
  fi
}

accept v1.2.3 1.2.3 1.2.3.0
accept 1.2.3 1.2.3 1.2.3.0
accept v0.0.0 0.0.0 0.0.0.0
accept v1.2.3-beta.1 1.2.3-beta.1 1.2.3.0
accept v1.2.3-beta01 1.2.3-beta01 1.2.3.0
accept v1.2.3-0 1.2.3-0 1.2.3.0
accept v1.2.3-x-y.z 1.2.3-x-y.z 1.2.3.0
accept v65534.65534.65534 65534.65534.65534 65534.65534.65534.0

for tag in v1.02.3 v01.2.3 v1.2.03 v1.2.3-beta..1 v1.2.3-beta.01 v1.2.3-01 v1.2.3- v1.2.3-. \
  v1.2 v65535.0.0 v1.123456.0 release-1 v1.2.3+build V1.2.3 "v1.2.3 " ""; do
  reject "$tag"
done

rm -f "$out"
if [ "$failures" -ne 0 ]; then
  echo "$failures release tag case(s) failed."
  exit 1
fi
echo "All release tag cases passed."
