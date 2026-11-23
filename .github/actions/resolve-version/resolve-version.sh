#!/usr/bin/env bash
# Resolves the build version; see action.yml. Inputs come from the environment (GITHUB_EVENT_NAME,
# RELEASE_TAG, GITHUB_SHA, GV_*), outputs go to GITHUB_ENV and GITHUB_OUTPUT.
set -euo pipefail
if [ "$GITHUB_EVENT_NAME" = "release" ]; then
  # SemVer 2.0 without build metadata: no leading zeros in numbers, no empty prerelease
  # identifiers. NuGet would normalise 1.02.3 to 1.2.3 and alias an existing version.
  num='(0|[1-9][0-9]*)'
  id='(0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)'
  if [[ ! "$RELEASE_TAG" =~ ^v?$num\.$num\.$num(-$id(\.$id)*)?$ ]]; then
    echo "::error::Release tag '$RELEASE_TAG' is not a version. Use a tag such as v1.2.3 or v1.2.3-beta.1."
    exit 1
  fi
  major="${BASH_REMATCH[1]}" minor="${BASH_REMATCH[2]}" patch="${BASH_REMATCH[3]}"
  # Each part also becomes an AssemblyVersion component, which is limited to 65534.
  for part in "$major" "$minor" "$patch"; do
    if [ "${#part}" -gt 5 ] || [ "$part" -gt 65534 ]; then
      echo "::error::Release tag '$RELEASE_TAG' has a version part above 65534, which AssemblyVersion cannot hold."
      exit 1
    fi
  done
  assembly="$major.$minor.$patch.0"
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
