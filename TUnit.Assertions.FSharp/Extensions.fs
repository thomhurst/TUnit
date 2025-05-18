namespace TUnit.Assertions.FSharp

open TUnit.Assertions.AssertionBuilders

module Operations =
    [<CustomOperation(MaintainsVariableSpaceUsingBind = true)>]
    let check (assertion: IInvokableAssertionBuilder) =
        Async.FromContinuations(fun (cont, econt, ccont) ->
            let awaiter = assertion.GetAwaiter()

            awaiter.OnCompleted(fun () ->
                try
                    if awaiter.IsCompleted then
                        cont(awaiter.GetResult())
                    else
                        ccont (System.OperationCanceledException())
                with ex ->
                    econt ex))