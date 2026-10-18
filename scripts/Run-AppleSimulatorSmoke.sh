#!/usr/bin/env bash
#
# Run-AppleSimulatorSmoke.sh — run the built TUnit.PlatformSmoke app in a booted Apple simulator
# and check its TRX report. Used by .github/workflows/platform-smoke.yml.
#
# Usage: Run-AppleSimulatorSmoke.sh <simulator-udid> <ios|tvos>
# Expects SmokePlatform (iOS or tvOS) to name the artifacts folder.

set -euo pipefail

device="$1"
platform="$2"
artifacts="artifacts/platform-smoke/$SmokePlatform"
app="tests/TUnit.PlatformSmoke/bin/Debug/net11.0-$platform/${platform}simulator-arm64/TUnit.PlatformSmoke.app"

mkdir -p "$artifacts"

collect_reports() {
  if container=$(xcrun simctl get_app_container "$device" net.dot.TUnitSmoke data 2>/dev/null); then
    if [[ -d "$container/Documents/TestResults" ]]; then
      cp -R "$container/Documents/TestResults" "$artifacts/"
    fi
  fi
}
trap collect_reports EXIT

xcrun simctl bootstatus "$device" -b
xcrun simctl install "$device" "$app"
container=$(xcrun simctl get_app_container "$device" net.dot.TUnitSmoke data)
xcrun simctl launch --console "$device" net.dot.TUnitSmoke --connection-mode=none \
  --app-arg=--minimum-expected-tests --app-arg=1 \
  --app-arg=--report-trx --app-arg=--report-trx-filename --app-arg=smoke.trx \
  --app-arg=--results-directory "--app-arg=$container/Documents/TestResults" \
  2>&1 | tee "$artifacts/test.log"
test "$(xmllint --xpath 'boolean(/*[local-name()="TestRun"]/*[local-name()="ResultSummary"]/*[local-name()="Counters"][@total="1" and @passed="1" and @failed="0"])' "$container/Documents/TestResults/smoke.trx")" = true
