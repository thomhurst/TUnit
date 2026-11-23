# Release version helpers, sourced by resolve-version.sh and release-draft.yml.

# Succeeds when $1 is a release tag: an optional "v", then a SemVer 2.0 version without build
# metadata, with no leading zeros and no empty prerelease identifiers. NuGet would normalise
# 1.02.3 to 1.2.3 and alias an existing version. Each core part also becomes an AssemblyVersion
# component, so it is limited to 65534. Sets VERSION_MAJOR, VERSION_MINOR and VERSION_PATCH.
is_release_tag() {
  local num='(0|[1-9][0-9]*)'
  local id='(0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)'
  [[ "$1" =~ ^v?$num\.$num\.$num(-$id(\.$id)*)?$ ]] || return 1
  VERSION_MAJOR="${BASH_REMATCH[1]}" VERSION_MINOR="${BASH_REMATCH[2]}" VERSION_PATCH="${BASH_REMATCH[3]}"
  local part
  for part in "$VERSION_MAJOR" "$VERSION_MINOR" "$VERSION_PATCH"; do
    if [ "${#part}" -gt 5 ] || [ "$part" -gt 65534 ]; then
      return 1
    fi
  done
}

# Compares two numeric strings without overflowing: prints -1, 0 or 1.
_compare_numbers() {
  local a="$1" b="$2"
  if [ "${#a}" -ne "${#b}" ]; then
    [ "${#a}" -lt "${#b}" ] && echo -1 || echo 1
  elif [[ "$a" < "$b" ]]; then echo -1
  elif [[ "$a" > "$b" ]]; then echo 1
  else echo 0
  fi
}

# Succeeds when version $1 has higher SemVer 2.0 precedence than version $2 (both valid, "v"
# optional).
version_gt() {
  local LC_ALL=C # alphanumeric identifiers compare in ASCII order
  local a="${1#v}" b="${2#v}"
  local a_core="${a%%-*}" b_core="${b%%-*}" a_pre="" b_pre=""
  [ "$a" != "$a_core" ] && a_pre="${a#*-}"
  [ "$b" != "$b_core" ] && b_pre="${b#*-}"

  local IFS=. i result
  local -a a_parts=($a_core) b_parts=($b_core)
  for i in 0 1 2; do
    result=$(_compare_numbers "${a_parts[i]}" "${b_parts[i]}")
    [ "$result" -ne 0 ] && { [ "$result" -gt 0 ]; return; }
  done

  # A release is higher than its prereleases.
  if [ -z "$a_pre" ] || [ -z "$b_pre" ]; then
    [ -z "$a_pre" ] && [ -n "$b_pre" ]
    return
  fi

  local -a a_ids=($a_pre) b_ids=($b_pre)
  local x y numeric='^(0|[1-9][0-9]*)$'
  for ((i = 0; i < ${#a_ids[@]} && i < ${#b_ids[@]}; i++)); do
    x="${a_ids[i]}" y="${b_ids[i]}"
    if [[ "$x" =~ $numeric ]] && [[ "$y" =~ $numeric ]]; then
      result=$(_compare_numbers "$x" "$y")
    elif [[ "$x" =~ $numeric ]]; then result=-1 # numeric identifiers are lower
    elif [[ "$y" =~ $numeric ]]; then result=1
    elif [[ "$x" < "$y" ]]; then result=-1
    elif [[ "$x" > "$y" ]]; then result=1
    else result=0
    fi
    [ "$result" -ne 0 ] && { [ "$result" -gt 0 ]; return; }
  done
  # All shared identifiers are equal: the longer list is higher.
  [ "${#a_ids[@]}" -gt "${#b_ids[@]}" ]
}
