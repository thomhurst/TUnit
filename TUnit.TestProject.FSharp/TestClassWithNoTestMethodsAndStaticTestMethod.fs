namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithNoTestMethodsAndStaticTestMethod() =
    [<Test>]
    static member Helper() = ()
