module TUnit.Assertions.FSharp.TaskAssert

open TUnit.Assertions.Core

[<AutoOpen>]
module TaskAssertBuilder =
    let taskAssert = task

[<AutoOpen>]
module TaskAssertCEExtensions =
    type TaskBuilderBase with
        #nowarn "FS1204"
        member inline x.Bind(assertion: IAssertion, continuation: Unit -> TaskCode<'TOverall, 'TResult2>) : TaskCode<'TOverall, 'TResult2> =
            let task = assertion.AssertAsync()
            x.Bind(task, continuation)
