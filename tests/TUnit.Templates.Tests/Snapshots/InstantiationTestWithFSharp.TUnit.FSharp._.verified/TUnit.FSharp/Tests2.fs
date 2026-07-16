namespace TUnit.FSharp

open System
open TUnit.Core

[<Arguments("Hello")>]
[<Arguments("World")>]
type MoreTests(title: string) =

    [<Test>]
    member _.ClassLevelDataRow() =
        Console.WriteLine(title)
        Console.WriteLine("Did I forget that data injection works on classes too?")

    [<ClassDataSource(typeof<DataClass>, Shared = [|SharedType.PerTestSession|])>]
    member val DataClass: DataClass = Unchecked.defaultof<_> with get, set

    [<Test>]
    [<MatrixDataSource>]
    member _.Matrices
        (
            [<Matrix(1, 2, 3)>] a: int,
            [<Matrix(true, false)>] b: bool,
            [<Matrix("A", "B", "C")>] c: string
        ) =
        Console.WriteLine("A new test will be created for each data row, whether it's on the class or method level!")
        Console.WriteLine("Oh and this is a matrix test. That means all combinations of inputs are attempted.")