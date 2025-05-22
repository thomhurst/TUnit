namespace TUnit.TestProject.FSharp

open System.Collections.Generic
open System.Threading.Tasks
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Core

// F# equivalent of EnumerableDataSourceDrivenTests.cs

type BaseValue() = class end

type ConcreteValue() =
    inherit BaseValue()

type ConcreteValue2() =
    inherit BaseValue()

type EnumerableDataSourceDrivenTests() =
    [<Test>]
    [<MethodDataSource("SomeMethod")>]
    member _.DataSource_Method(value: int) : Task =
        Assert.That(value).IsEqualTo(1)

    [<Test>]
    [<MethodDataSource("SomeMethod")>]
    member _.DataSource_Method2(value: int) : Task =
        Assert.That(value).IsEqualTo(1)

    [<Test>]
    [<MethodDataSource("MethodWithBaseReturn")>]
    member _.DataSource_WithBaseReturn(value: BaseValue) =
        ()

    static member SomeMethod() = seq { 1; 2; 3; 4; 5 }

    static member MethodWithBaseReturn() =
        [ fun () -> ConcreteValue() :> BaseValue
          fun () -> ConcreteValue2() :> BaseValue ]
