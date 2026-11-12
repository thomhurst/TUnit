# ModularPipelines v4 consumer validation

This executable references the real `TUnit.Pipeline` project and runs its package-project enumeration module. It also exercises Git information, a generated DotNet command, dependency ordering, skip propagation, continued execution after a failure, logging, and summary results. The intentional failure must appear in the summary; the validation executable exits successfully only when every expected outcome matches. It never registers the publishing, tagging, release, packing, or browser-installation modules.

The production entry point accepts `--validate-only` to validate its complete module graph without executing modules. Unknown category selectors must return exit code 1.

The migration targets ModularPipelines 4.0.0. Until that package set is published, build the four required packages from source into a local feed and override `ModularPipelinesVersion`. The reproducible validation script lives in [ModularPipelines](https://github.com/thomhurst/ModularPipelines/blob/main/scripts/Validate-TUnitMigration.ps1):

```powershell
# Run from a ModularPipelines checkout, using its pinned SDK.
& ./scripts/Validate-TUnitMigration.ps1 `
    -ConsumerPath C:/path/to/TUnit `
    -EvidenceDirectory C:/path/to/migration-evidence
```

The matching `TUnit v4 migration` workflow checks out a pinned revision of this branch and supplies local packages. Keep the consumer upgrade unmerged until ModularPipelines 4.0.0 is available; ordinary TUnit CI will then restore the default version without a local feed.

Migration findings and validation evidence are tracked in [ModularPipelines #7321](https://github.com/thomhurst/ModularPipelines/issues/7321). Findings: [MP0013 assembly-registration false positives](https://github.com/thomhurst/ModularPipelines/issues/7352) and [the missing FileSystem import in the guide](https://github.com/thomhurst/ModularPipelines/issues/7353).
