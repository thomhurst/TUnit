#!/usr/bin/env bash
# The SDK image ships a single runtime, but the test projects target net8.0, net9.0 and
# net10.0 (eng/TestProject.props). Install every runtime they need next to the image's SDK.
set -euo pipefail

dotnet_root="$(dirname "$(readlink -f "$(command -v dotnet)")")"
installer="$(mktemp)"
trap 'rm -f "$installer"' EXIT

curl -sSL https://dot.net/v1/dotnet-install.sh -o "$installer"

for channel in 8.0 9.0 10.0; do
    for runtime in dotnet aspnetcore; do
        bash "$installer" --channel "$channel" --runtime "$runtime" --install-dir "$dotnet_root" --no-path
    done
done

dotnet --list-runtimes
