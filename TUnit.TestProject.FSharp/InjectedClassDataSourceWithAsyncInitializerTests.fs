namespace TUnit.TestProject.FSharp

open System
open System.Threading.Tasks
open TUnit.Core
open TUnit.Core.Interfaces

[<ClassDataSource(typeof<InjectedClassDataSourceWithAsyncInitializerTests.MyClass>, Shared=SharedType.Keyed, Key="MyKey")>]
type InjectedClassDataSourceWithAsyncInitializerTests(myClass: InjectedClassDataSourceWithAsyncInitializerTests.MyClass) =
    [<Before(HookType.Test)>]
    member _.BeforeTest() : Task =
        Console.WriteLine("BeforeTest")
        Task.CompletedTask

    [<Test>]
    member _.Test1() : Task =
        Console.WriteLine("Test")
        Task.CompletedTask

    [<Test>]
    member _.Test2() : Task =
        Console.WriteLine("Test")
        Task.CompletedTask

    [<Test>]
    member _.Test3() : Task =
        Console.WriteLine("Test")
        Task.CompletedTask

    type MyClass() =
        interface IAsyncInitializer with
            member _.InitializeAsync() =
                Console.WriteLine("IAsyncInitializer.InitializeAsync")
                Task.CompletedTask
