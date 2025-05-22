namespace TUnit.TestProject.FSharp

open System
open TUnit.Core

// F# equivalent of DataDrivenTests.cs

type TestEnum =
    | One = 0
    | Two = 1

type DataDrivenTests() =
    [<Test>]
    [<Arguments(1)>]
    [<Arguments(2)>]
    [<Arguments(3)>]
    member _.DataSource_Method(value: int) =
        ()

    [<Test>]
    [<Arguments(1, "String")>]
    [<Arguments(2, "String2")>]
    [<Arguments(3, "String3")>]
    member _.DataSource_Method2(value: int, value2: string) =
        ()

    [<Test>]
    [<Arguments(TestEnum.One)>]
    [<Arguments(TestEnum.Two)>]
    [<Arguments(-1)>]
    member _.EnumValue(testEnum: TestEnum) =
        ()

    [<Test>]
    [<Arguments(null)>]
    member _.NullValue(value: string option) =
        ()

    [<Test>]
    [<Arguments("")>]
    member _.EmptyString(value: string) =
        ()

    [<Test>]
    [<Arguments("Foo bar!")>]
    member _.NonEmptyString(value: string) =
        ()

    [<Test>]
    [<Arguments(null)>]
    [<Arguments(false)>]
    [<Arguments(true)>]
    member _.BooleanString(value: bool option) =
        ()

    [<Test>]
    [<Arguments(typeof<obj>)>]
    member _.Type(value: Type) =
        ()

    [<Test>]
    [<Arguments([| 1; 2; 3 |])>]
    member _.IntegerArray(values: int array) =
        ()

    [<Test>]
    [<Arguments(System.Int32.MaxValue)>]
    member _.IntMaxValue(value: int) =
        ()
