namespace TUnit.TestProject.FSharp

open TUnit.Core
open System.Diagnostics.CodeAnalysis

// Equivalent of DeepNestedDependencyConflict.cs

[<SuppressMessage("Usage", "TUnit0033:Conflicting DependsOn attributes")>]
type DeepNestedDependencyConflict() =
    [<Test>]
    [<DependsOn("Test2")>]
    member _.Test1() = ()

    [<Test>]
    [<DependsOn("Test3")>]
    member _.Test2() = ()

    [<Test>]
    [<DependsOn("Test4")>]
    member _.Test3() = ()

    [<Test>]
    [<DependsOn("Test5")>]
    member _.Test4() = ()

    [<Test>]
    [<DependsOn("Test6")>]
    member _.Test5() = ()

    [<Test>]
    [<DependsOn("Test7")>]
    member _.Test6() = ()

    [<Test>]
    [<DependsOn("Test8")>]
    member _.Test7() = ()

    [<Test>]
    [<DependsOn("Test9")>]
    member _.Test8() = ()

    [<Test>]
    [<DependsOn("Test10")>]
    member _.Test9() = ()

    [<Test>]
    [<DependsOn("Test9")>]
    member _.Test10() = ()
