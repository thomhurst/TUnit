# TUnit0075: Use the hook cancellation token for setup

**Severity:** Warning

Setup hooks receive a `CancellationToken` that includes their own timeout. This applies both to an explicit `[Timeout]` and to `TUnitSettings.Default.Timeouts.DefaultHookTimeout`. The token exposed by `TestContext.Execution.CancellationToken` represents test cancellation; it does not include the setup hook's timeout.

TUnit0075 reports a direct use of the injected test context's token as a cancellation argument to an operation that a `[Before(Test)]` or `[BeforeEvery(Test)]` hook awaits or returns. For example, `await InitializeAsync(context.Execution.CancellationToken)` and `return InitializeAsync(context.Execution.CancellationToken)` both trigger the warning. Expression-bodied hooks are checked too.

## How to fix it

Add a `CancellationToken` parameter at the end of the hook's parameter list if it does not already have one, then pass that parameter to the setup operation. For example, change `InitializeAsync(context.Execution.CancellationToken)` to `InitializeAsync(cancellationToken)` in a hook declared as `public Task Setup(TestContext context, CancellationToken cancellationToken)`.

The operation must cooperate with cancellation. Passing the correct token lets it observe the hook timeout; it does not forcibly stop work that ignores cancellation.

## Scope

The rule checks the direct `context.Execution.CancellationToken` property chain, where `context` is the hook's `TestContext` parameter. It recognizes direct `await`, `await` with `ConfigureAwait`, and returns, including branches of conditional expressions such as `return useCache ? Task.CompletedTask : InitializeAsync(context.Execution.CancellationToken)`.

Overrides of virtual setup hooks are checked even when the override does not repeat `[Before(Test)]`. The rule follows the override chain; a method that hides a base hook with `new` does not inherit that hook's role.

It does not follow tokens through local variables or helper methods, inspect nested lambdas or local functions, or check tokens from other contexts. It does not apply to ordinary tests or methods used only for cleanup. Reading cancellation state, such as `context.Execution.CancellationToken.IsCancellationRequested`, is not reported.

See [Hook Parameters](../writing-tests/hooks.md#hook-parameters) for the supported hook signatures.
