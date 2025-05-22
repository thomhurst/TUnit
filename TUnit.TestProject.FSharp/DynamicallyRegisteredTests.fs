namespace TUnit.TestProject.FSharp

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open TUnit.Core
open TUnit.Core.Enums
open TUnit.Core.Interfaces

type DynamicDataGenerator() =
    inherit DataSourceGeneratorAttribute<int>()
    let mutable count = 0
    let cts = new CancellationTokenSource()

    override _.GenerateDataSources(dataGeneratorMetadata) =
        seq { yield Func<int>(fun () -> (Random()).Next()) }

    interface ITestStartEventReceiver with
        member _.OnTestStart(beforeTestContext: BeforeTestContext) =
            if not (DynamicDataGenerator.IsReregisteredTest(beforeTestContext.TestContext)) then
                beforeTestContext.AddLinkedCancellationToken(cts.Token)
            ValueTask()

    interface ITestEndEventReceiver with
        member _.OnTestEnd(afterTestContext: AfterTestContext) =
            task {
                let testContext = afterTestContext.TestContext
                if testContext.Result <> null && testContext.Result.Status = Status.Failed then
                    cts.Cancel()
                    count <- count + 1
                    if count > 5 then
                        raise (Exception())
                    if DynamicDataGenerator.IsReregisteredTest(testContext) then
                        () // Optionally suppress reporting
                    do! testContext.ReregisterTestWithArguments([|(Random()).Next()|],
                        dict [ "DynamicDataGeneratorRetry", box true ])
            } |> ValueTask
        member _.Order = 0

    interface IEventReceiver with
        member _.Order = 0

    static member private IsReregisteredTest(testContext: TestContext) =
        testContext.ObjectBag.ContainsKey("DynamicDataGeneratorRetry")

[<DynamicCodeOnly>]
type DynamicallyRegisteredTests() =
    [<Test>]
    [<DynamicDataGenerator>]
    member _.MyTest(value: int) =
        raise (Exception($"Value {value} !"))


