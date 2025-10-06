namespace TUnit.Assertions.FSharp

open TUnit.Assertions.AssertionBuilders
open TUnit.Assertions.Extensions

module Operations =
    [<CustomOperation(MaintainsVariableSpaceUsingBind = true)>]
    let check (assertion: 'T) =
        Async.FromContinuations(fun (cont, econt, ccont) ->
            match box assertion with
            | :? IInvokableAssertionBuilder as invokable ->
                let awaiter = invokable.GetAwaiter()
                awaiter.OnCompleted(fun () ->
                    try
                        if awaiter.IsCompleted then
                            cont(awaiter.GetResult())
                        else
                            ccont (System.OperationCanceledException())
                    with ex ->
                        econt ex)
            | :? ThrowsException<obj, exn> as throwsExn ->
                let awaiter = throwsExn.GetAwaiter()
                awaiter.OnCompleted(fun () ->
                    try
                        if awaiter.IsCompleted then
                            let _ = awaiter.GetResult() // ignore the exn result
                            cont ()
                        else
                            ccont (System.OperationCanceledException())
                    with ex ->
                        econt ex)
            | _ ->
                invalidOp $"Unsupported assertion type: We currently don't support Assertion Type {assertion.GetType()}"
        )
