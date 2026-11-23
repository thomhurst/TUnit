#!/usr/bin/env bash
# Checks which release tags resolve-version.sh accepts, the versions it derives from them, and
# version_gt's SemVer precedence.
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

# shellcheck source=semver.sh
source "$(dirname "$0")/semver.sh"

# Each pair is ascending: the first is lower than the second.
ascending=(
  1.10.1 1.11.0   1.9.0 1.10.0   1.2.3 2.0.0   0.9.9 1.0.0   1.2.3 v1.2.4
  2.0.0-rc.9 2.0.0-rc.10   2.0.0-rc.1 2.0.0   1.0.0-alpha 1.0.0-alpha.1
  1.0.0-alpha.1 1.0.0-alpha.beta   1.0.0-alpha.beta 1.0.0-beta   1.0.0-beta 1.0.0-beta.2
  1.0.0-beta.2 1.0.0-beta.11   1.0.0-beta.11 1.0.0-rc.1   1.0.0-rc.1 1.0.0
  1.0.0-1 1.0.0-alpha   1.0.0-Z 1.0.0-a   2.0.0-rc.2001 2.0.0-rc.2010
)
for ((i = 0; i < ${#ascending[@]}; i += 2)); do
  low="${ascending[i]}" high="${ascending[i + 1]}"
  if ! version_gt "$high" "$low"; then
    echo "FAIL: $high should be higher than $low"; failures=$((failures + 1))
  fi
  if version_gt "$low" "$high"; then
    echo "FAIL: $low should not be higher than $high"; failures=$((failures + 1))
  fi
done
for same in 1.2.3 v1.2.3 1.0.0-beta.1; do
  if version_gt "$same" "${same#v}"; then
    echo "FAIL: $same should not be higher than itself"; failures=$((failures + 1))
  fi
done

rm -f "$out"
if [ "$failures" -ne 0 ]; then
  echo "$failures release version case(s) failed."
  exit 1
fi
echo "All release version cases passed."
