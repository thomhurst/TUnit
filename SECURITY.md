# Security Policy

## Supported versions

Security fixes are released for the latest published version of TUnit. Please upgrade to the latest release before reporting.

## Reporting a vulnerability

Please do **not** open a public issue for security vulnerabilities.

Report privately via [GitHub private vulnerability reporting](https://github.com/thomhurst/TUnit/security/advisories/new). Include:

- the affected package(s) and version
- a description of the issue and its impact
- a minimal reproducer (for example a test, a report sidecar file, or a request payload)

You should receive an acknowledgement within a few days. Once a fix is released, an advisory will be published crediting the reporter unless you prefer otherwise.

## Scope

TUnit runs test code, which is trusted by design. "A test can execute arbitrary code" is not a vulnerability. Issues are in scope when TUnit mishandles data the test author does not control, for example HTML report or CI summary injection, the OTLP receiver, report aggregation files, HTTP header propagation, or source-generator escaping. See [`.oss-scanner/threat_model.md`](.oss-scanner/threat_model.md) for details.
