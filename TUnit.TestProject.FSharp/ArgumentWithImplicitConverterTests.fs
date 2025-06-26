namespace TUnit.TestProject.FSharp

open System
open TUnit.Core

// F# equivalents for ArgumentWithImplicitConverterTests.cs

type ExplicitInteger =
    | ExplicitInteger of int
    static member op_Explicit(i: int) = ExplicitInteger i
    override this.ToString() =
        let (ExplicitInteger i) = this in i.ToString()

type ImplicitInteger =
    | ImplicitInteger of int
    static member op_Implicit(i: int) = ImplicitInteger i
    override this.ToString() =
        let (ImplicitInteger i) = this in i.ToString()

type ArgumentWithImplicitConverterTests() =
    [<Test>]
    [<Arguments(1)>]
    [<Arguments(2)>]
    [<Arguments(3)>]
    member _.Explicit(integer: ExplicitInteger) =
        Console.WriteLine(integer)

    [<Test>]
    [<Arguments(1)>]
    [<Arguments(2)>]
    [<Arguments(3)>]
    member _.Implicit(integer: ImplicitInteger) =
        Console.WriteLine(integer)
