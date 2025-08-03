# Task Completion Checklist

When completing a coding task in TUnit:

## Code Quality
- [ ] Follow async best practices (no `.Result`, `.Wait()`, `GetAwaiter().GetResult()`)
- [ ] Write self-descriptive code without redundant comments
- [ ] Maintain clean architecture separation (source gen vs runtime)
- [ ] Use proper error handling and logging
- [ ] Do not over-engineer
- [ ] Follow C# best practices, as well as principles such as DRY, KISS, SRP and SOLID

## Testing
- [ ] Run relevant tests: `dotnet run -c Release` in test project directory
- [ ] Add/update unit tests in `TUnit.UnitTests` for framework changes
- [ ] Add/update integration tests in `TUnit.TestProject` for end-to-end scenarios and add the [EngineTest(ExpectedResult.Pass)] attribute so the pipeline knows to run these tests and they must pass
- [ ] Verify no hanging processes or infinite loops

## Architecture Compliance
- [ ] Source generators only emit data, not execution logic
- [ ] Runtime components handle all complex logic
- [ ] Maintain Microsoft.Testing.Platform compatibility
- [ ] Ensure proper async support throughout
- [ ] Ensure feature parity between source generation mode and reflection mode

## Performance & Reliability
- [ ] Check for potential deadlocks or blocking operations
- [ ] Verify proper resource disposal
- [ ] Test cancellation token handling
- [ ] Validate timeout mechanisms work correctly
- [ ] Ensure code is performant

## Documentation (if applicable)
- [ ] Update relevant documentation in `docs/` if behavior changes
- [ ] If building a new feature, add new documentation in `docs/` in the relevant location with clear, easy to read language and code examples
- [ ] Update CLAUDE.md if architectural patterns change

## Final Verification
- [ ] Build solution: `dotnet build -c Release`
- [ ] Run full test suite
- [ ] Check for memory leaks or hanging processes
