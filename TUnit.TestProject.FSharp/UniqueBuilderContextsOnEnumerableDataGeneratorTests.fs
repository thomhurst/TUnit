namespace TUnit.TestProject.FSharp

open System
open System.Collections.Generic
open TUnit.Core
open Shouldly

// F# equivalent of UniqueBuilderContextsOnEnumerableDataGeneratorTests.cs

type UniqueBuilderContextsOnEnumerableDataGeneratorTests() =
    [<Test>]
    [<UniqueBuilderContextsOnEnumerableDataGeneratorTestsGenerator>]
    member _.Test(value: int) = ()

type UniqueBuilderContextsOnEnumerableDataGeneratorTestsGenerator() =
    inherit DataSourceGeneratorAttribute<int>()
    override _.GenerateDataSources(dataGeneratorMetadata) =
        let id1 = dataGeneratorMetadata.TestBuilderContext.Current.Id
        let id2 = dataGeneratorMetadata.TestBuilderContext.Current.Id
        seq {
            yield (fun () -> 1)
            let id3 = dataGeneratorMetadata.TestBuilderContext.Current.Id
            yield (fun () -> 2)
            let id4 = dataGeneratorMetadata.TestBuilderContext.Current.Id
            id1.ShouldBe(id2)
            id3.ShouldNotBe(id1)
            id4.ShouldNotBe(id1)
            id4.ShouldNotBe(id3)
        }
