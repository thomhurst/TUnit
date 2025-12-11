## Description

<!-- Provide a brief description of the changes in this PR -->

## Related Issue

<!-- Link to the issue this PR addresses (use "Fixes #123" or "Closes #123" to auto-close) -->

Fixes #

## Type of Change

<!-- Mark the appropriate option with an "x" -->

- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to change)
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Refactoring (no functional changes)

## Checklist

### Required

- [ ] I have read the [Contributing Guidelines](https://github.com/thomhurst/TUnit/blob/main/.github/CONTRIBUTING.md)
- [ ] If this is a new feature, I started a [discussion](https://github.com/thomhurst/TUnit/discussions) first and received agreement
- [ ] My code follows the project's code style (modern C# syntax, proper naming conventions)
- [ ] I have written tests that prove my fix is effective or my feature works

### TUnit-Specific Requirements

<!-- These are critical for TUnit contributions - see CLAUDE.md for details -->

- [ ] **Dual-Mode Implementation**: If this change affects test discovery/execution, I have implemented it in BOTH:
  - [ ] Source Generator path (`TUnit.Core.SourceGenerator`)
  - [ ] Reflection path (`TUnit.Engine`)
- [ ] **Snapshot Tests**: If I changed source generator output or public APIs:
  - [ ] I ran `TUnit.Core.SourceGenerator.Tests` and/or `TUnit.PublicAPI` tests
  - [ ] I reviewed the `.received.txt` files and accepted them as `.verified.txt`
  - [ ] I committed the updated `.verified.txt` files
- [ ] **Performance**: If this change affects hot paths (test discovery, execution, assertions):
  - [ ] I minimized allocations and avoided LINQ in hot paths
  - [ ] I cached reflection results where appropriate
- [ ] **AOT Compatibility**: If this change uses reflection:
  - [ ] I added appropriate `[DynamicallyAccessedMembers]` annotations
  - [ ] I verified the change works with `dotnet publish -p:PublishAot=true`

### Testing

- [ ] All existing tests pass (`dotnet test`)
- [ ] I have added tests that cover my changes
- [ ] I have tested both source-generated and reflection modes (if applicable)

## Additional Notes

<!-- Any additional information that reviewers should know -->
