namespace TestProject

open System
open TUnit.Core

[<ClassDataSource(typeof<DataClass>)>]
[<ClassConstructor(typeof<DependencyInjectionClassConstructor>)>]
type AndEvenMoreTests(dataClass: DataClass) =

    [<Test>]
    member _.HaveFun() =
        Console.WriteLine(dataClass)
        Console.WriteLine("For more information, check out the documentation")
        Console.WriteLine("https://tunit.dev/")

