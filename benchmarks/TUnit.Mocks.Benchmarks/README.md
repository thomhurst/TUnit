# Mock benchmarks

Run these benchmarks in Release with the SDK selected by `global.json`.

## Invocation history

`InvocationBenchmarks` recreates all mocks and matchers before every timed iteration.
History grows during the iteration, so the measurement includes recording calls and
capturing arguments. Creation and setup are outside the timed region. Resetting only
call history is insufficient because `Arg<T>` retains captured values independently.

Single-call methods use 16,384 invocations per iteration. The 100-call methods use
256 invocations (25,600 calls) and `OperationsPerInvoke = 100`; their results are also
per call. All frameworks use the same lifecycle and counts. Unroll factor is one.
These explicit counts prevent pilot calibration from growing history without a bound.

Compare history sizes by overriding the invocation count for single-call methods:

```sh
dotnet run -c Release -f net11.0 -- --filter '*InvocationBenchmarks.*_Invoke' --invocationCount 1024
dotnet run -c Release -f net11.0 -- --filter '*InvocationBenchmarks.*_Invoke' --invocationCount 16384
dotnet run -c Release -f net11.0 -- --filter '*InvocationBenchmarks.*_Invoke' --invocationCount 262144
```

Small batches can have short iteration times. Inspect error and standard deviation,
and increase iteration or launch counts before drawing timing conclusions.
Do not compare these bounded results directly with older unbounded invocation runs.

## Workflow scope

Creation and setup costs remain in `MockCreationBenchmarks` and `SetupBenchmarks`.
`CallbackBenchmarks` includes fresh mock creation, setup and callback invocation.
`VerificationBenchmarks` measures verification workflows: mock creation, any setup,
any invocation and verification all occur inside the timed method. It does not
isolate the cost of verification. `CombinedWorkflowBenchmarks` measures the complete
create, setup, invoke and verify workflow.
