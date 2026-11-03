# Build environment for Anthropic's OSS Scanner (https://github.com/anthropics/oss-scanner).
# The scanner builds this with network access, then runs the resulting image with no network.
# Everything needed to build and test TUnit must therefore be restored here.
FROM mcr.microsoft.com/dotnet/sdk:11.0

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 \
    NUGET_XMLDOC_MODE=skip \
    MSBUILDDISABLENODEREUSE=1 \
    DOTNET_ROOT=/usr/share/dotnet

SHELL ["/bin/bash", "-o", "pipefail", "-c"]

RUN apt-get update && apt-get install -y --no-install-recommends \
      bash ca-certificates curl git python3 \
    && rm -rf /var/lib/apt/lists/*

COPY . /src
WORKDIR /src

# Install the SDK pinned by global.json, plus the net8.0/net9.0 runtimes so every
# test target framework can run offline.
RUN curl -sSL --retry 5 https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --jsonfile /src/global.json --install-dir "$DOTNET_ROOT" \
    && for channel in 8.0 9.0; do \
         /tmp/dotnet-install.sh --channel "$channel" --runtime dotnet --install-dir "$DOTNET_ROOT" || exit 1; \
         /tmp/dotnet-install.sh --channel "$channel" --runtime aspnetcore --install-dir "$DOTNET_ROOT" || exit 1; \
       done \
    && rm /tmp/dotnet-install.sh \
    && dotnet --info

# Restore and build in Debug. TUnit.CI.slnx is the solution CI builds on Linux.
# TUnit.Reporting.Tool (the `tunit-report` dotnet tool) is only in TUnit.slnx, so build it separately.
# NuGetAudit is off so a newly published advisory cannot fail this build; Dependabot covers it.
RUN dotnet restore TUnit.CI.slnx -p:NuGetAudit=false \
    && dotnet restore src/TUnit.Reporting.Tool/TUnit.Reporting.Tool.csproj -p:NuGetAudit=false \
    && dotnet build TUnit.CI.slnx -c Debug --no-restore -graphBuild:True -m:4 \
    && dotnet build src/TUnit.Reporting.Tool/TUnit.Reporting.Tool.csproj -c Debug --no-restore \
    && dotnet build-server shutdown

# Helper that runs the offline-safe test suites covering the security-relevant areas
# listed in threat_model.md. Never run tests/TUnit.TestProject unfiltered: it contains
# tests that fail on purpose.
RUN cat > /usr/local/bin/tunit-oss-test <<'SCRIPT' && chmod +x /usr/local/bin/tunit-oss-test
#!/usr/bin/env bash
set -uo pipefail
cd /src
status=0
for project in \
    tests/TUnit.UnitTests \
    tests/TUnit.OpenTelemetry.Tests \
    tests/TUnit.AspNetCore.Tests \
    tests/TUnit.Assertions.Tests \
    tests/TUnit.Mocks.Tests; do
  echo "=== $project ==="
  # --no-restore (not --no-build) so edits made in the image are rebuilt offline before testing.
  (cd "$project" && timeout 900 dotnet run -c Debug --no-restore --framework net10.0) || status=1
done
exit "$status"
SCRIPT

# Smoke-check the image. Failures are reported but do not fail the build.
RUN (cd tests/TUnit.UnitTests && timeout 900 dotnet run -c Debug --no-build --framework net10.0) \
    || echo "WARNING: TUnit.UnitTests reported failures"; \
    dotnet build-server shutdown || true

CMD ["bash"]
