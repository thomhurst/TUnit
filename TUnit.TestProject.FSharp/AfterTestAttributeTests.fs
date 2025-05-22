namespace TUnit.TestProject.FSharp

open System
open System.Threading.Tasks
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Core.Interfaces
open Polyfills

// Equivalent of AfterTestAttributeTests.cs

type AfterTestAttributeTests() =
    static let filename = sprintf "%s-AfterTestAttributeTests.txt" (Guid.NewGuid().ToString("N"))

    [<Test>]
    [<WriteFileAfterTest>]
    member _.Test() : Task =
        Assert.That(File.Exists(filename)).IsFalse()

and WriteFileAfterTestAttribute() =
    inherit System.Attribute()
    interface ITestEndEventReceiver with
        member _.OnTestEnd(testContext: AfterTestContext) =
            task {
                Console.WriteLine("Writing file inside WriteFileAfterTestAttribute!")
                do! FilePolyfill.WriteAllTextAsync(filename, "Foo!")
            } :> Task
        member _.Order = 0
