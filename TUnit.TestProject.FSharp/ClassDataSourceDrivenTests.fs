namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Assertions.FSharp.Operations
open TUnit.Core
open TUnit.TestProject.Library.Models

type ClassDataSourceDrivenTests() =

    [<Test>]
    [<ClassDataSource(typeof<SomeAsyncDisposableClass>)>]
    member _.DataSource_Class(value: SomeAsyncDisposableClass) : unit =
        // Dummy method
        ()

    [<Test>]
    [<ClassDataSource(typeof<SomeAsyncDisposableClass>)>]
    member _.DataSource_Class_Generic(value: SomeAsyncDisposableClass) : unit =
        // Dummy method
        ()

    [<Test>]
    [<ClassDataSource(typeof<InitializableClass>)>]
    member _.IsInitialized_With_1_ClassDataSource(class1: InitializableClass) : Task =
        task {
            do! check (Assert.That(class1.IsInitialized).IsTrue())
        }

    [<Test>]
    [<ClassDataSource(typeof<InitializableClass>, typeof<InitializableClass>)>]
    member _.IsInitialized_With_2_ClassDataSources(class1: InitializableClass, class2: InitializableClass) : Task =
        task {
            do! check (Assert.That(class1.IsInitialized).IsTrue())
            do! check (Assert.That(class2.IsInitialized).IsTrue())
        }

    [<Test>]
    [<ClassDataSource(typeof<InitializableClass>, typeof<InitializableClass>, typeof<InitializableClass>)>]
    member _.IsInitialized_With_3_ClassDataSources(class1: InitializableClass, class2: InitializableClass, class3: InitializableClass) : Task =
        task {
            do! check (Assert.That(class1.IsInitialized).IsTrue())
            do! check (Assert.That(class2.IsInitialized).IsTrue())
            do! check (Assert.That(class3.IsInitialized).IsTrue())
        }

    [<Test>]
    [<ClassDataSource(typeof<InitializableClass>, typeof<InitializableClass>, typeof<InitializableClass>, typeof<InitializableClass>)>]
    member _.IsInitialized_With_4_ClassDataSources(class1: InitializableClass, class2: InitializableClass, class3: InitializableClass, class4: InitializableClass) : Task =
        task {
            do! check (Assert.That(class1.IsInitialized).IsTrue())
            do! check (Assert.That(class2.IsInitialized).IsTrue())
            do! check (Assert.That(class3.IsInitialized).IsTrue())
            do! check (Assert.That(class4.IsInitialized).IsTrue())
        }

    [<Test>]
    [<ClassDataSource(typeof<InitializableClass>, typeof<InitializableClass>, typeof<InitializableClass>, typeof<InitializableClass>, typeof<InitializableClass>)>]
    member _.IsInitialized_With_5_ClassDataSources(class1: InitializableClass, class2: InitializableClass, class3: InitializableClass, class4: InitializableClass, class5: InitializableClass) : Task =
        task {
            do! check (Assert.That(class1.IsInitialized).IsTrue())
            do! check (Assert.That(class2.IsInitialized).IsTrue())
            do! check (Assert.That(class3.IsInitialized).IsTrue())
            do! check (Assert.That(class4.IsInitialized).IsTrue())
            do! check (Assert.That(class5.IsInitialized).IsTrue())
        }
