namespace TUnit.TestProject.FSharp

open System
open TUnit.Core

// F# equivalent of ConsoleConcurrentTests.cs

type ConsoleConcurrentTests() =
    [<Test>]
    [<Repeat(25)>]
    member _.Test1() = Console.WriteLine("Test1")
    [<Test>]
    [<Repeat(25)>]
    member _.Test2() = Console.WriteLine("Test2")
    [<Test>]
    [<Repeat(25)>]
    member _.Test3() = Console.WriteLine("Test3")
    [<Test>]
    [<Repeat(25)>]
    member _.Test4() = Console.WriteLine("Test4")
    [<Test>]
    [<Repeat(25)>]
    member _.Test5() = Console.WriteLine("Test5")
    [<Test>]
    [<Repeat(25)>]
    member _.Test6() = Console.WriteLine("Test6")
    [<Test>]
    [<Repeat(25)>]
    member _.Test7() =
        Console.WriteLine("Test7")
        Console.WriteLine("Test7")
        Console.WriteLine("Test7")
        Console.WriteLine("Test7")
        Console.WriteLine("Test7")
    [<Test>]
    [<Repeat(25)>]
    member _.Test8() = Console.WriteLine("Test8")
    [<Test>]
    [<Repeat(25)>]
    member _.Test9() = Console.WriteLine("Test9")
    [<Test>]
    [<Repeat(25)>]
    member _.Test10() = Console.WriteLine("Test10")
