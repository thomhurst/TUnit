#!/usr/bin/env bash
# Resolves the build version; see action.yml. Inputs come from the environment (GITHUB_EVENT_NAME,
# RELEASE_TAG, GITHUB_SHA, GV_*), outputs go to GITHUB_ENV and GITHUB_OUTPUT.
set -euo pipefail
# shellcheck source=semver.sh
source "$(dirname "${BASH_SOURCE[0]}")/semver.sh"
if [ "$GITHUB_EVENT_NAME" = "release" ]; then
  if ! is_release_tag "$RELEASE_TAG"; then
    echo "::error::Release tag '$RELEASE_TAG' is not a version. Use a tag such as v1.2.3 or v1.2.3-beta.1, with no leading zeros and each number at most 65534 (the AssemblyVersion limit)."
    exit 1
  fi
  assembly="$VERSION_MAJOR.$VERSION_MINOR.$VERSION_PATCH.0"
  semver="${RELEASE_TAG#v}"
  full="$semver"
  file="$assembly"
  informational="$semver+Sha.$GITHUB_SHA"
else
  if [ -z "$GV_SEMVER" ]; then
    echo "::error::GitVersion produced no semVer; Version would be empty."
    exit 1
  fi
  semver="$GV_SEMVER"
  full="$GV_FULLSEMVER"
  assembly="$GV_ASSEMBLYSEMVER"
  file="$GV_ASSEMBLYSEMFILEVER"
  informational="$GV_INFORMATIONALVERSION"
fi
echo "Version: $semver"
{
  echo "Version=$semver"
  echo "AssemblyVersion=$assembly"
  echo "FileVersion=$file"
  echo "InformationalVersion=$informational"
  echo "PackageVersion=$full"
} >> "$GITHUB_ENV"
{
  echo "semVer=$semver"
  echo "fullSemVer=$full"
  echo "assemblySemVer=$assembly"
  echo "assemblySemFileVer=$file"
  echo "informationalVersion=$informational"
} >> "$GITHUB_OUTPUT"
