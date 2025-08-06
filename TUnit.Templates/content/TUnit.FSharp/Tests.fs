namespace TestProject

open System
open System.Collections.Generic
open System.Threading.Tasks
open TestProject.Data
open TUnit
open TUnit.Core
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Assertions.FSharp.Operations

type Tests() =

    [<Test>]
    member _.Basic() =
        Console.WriteLine("This is a basic test")

    [<Test>]
    [<Arguments(1, 2, 3)>]
    [<Arguments(2, 3, 5)>]
    member _.DataDrivenArguments(a: int, b: int, c: int) =
        async {
            Console.WriteLine("This one can accept arguments from an attribute")
            let result = a + b
            do! check(Assert.That(result).IsEqualTo(c))
        }


    [<Test>]
    [<MethodDataSource("DataSource")>]
    member _.MethodDataSource(a: int, b: int, c: int) =
        async {
            Console.WriteLine("This one can accept arguments from a method")
            let result = a + b
            do! check(Assert.That(result).IsEqualTo(c))
        }

    [<Test>]
    [<ClassDataSource(typeof<DataClass>)>]
    [<ClassDataSource(typeof<DataClass>, Shared = [|SharedType.PerClass|])>]
    [<ClassDataSource(typeof<DataClass>, Shared = [|SharedType.PerAssembly|])>]
    [<ClassDataSource(typeof<DataClass>, Shared = [|SharedType.PerTestSession|])>]
    member _.ClassDataSource(dataClass: DataClass) =
        Console.WriteLine("This test can accept a class, which can also be pre-initialised before being injected in")
        Console.WriteLine("These can also be shared among other tests, or new'd up each time, by using the `Shared` property on the attribute")

    [<Test>]
    [<DataGenerator>]
    member _.CustomDataGenerator(a: int, b: int, c: int) =
        async {
            Console.WriteLine("You can even define your own custom data generators")
            let result = a + b
            do! check(Assert.That(result).IsEqualTo(c))
        }

    static member DataSource() : IEnumerable<struct (int * int * int)> =
        seq {
            yield struct (1, 1, 2)
            yield struct (2, 1, 3)
            yield struct (3, 1, 4)
        }
