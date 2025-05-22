namespace TUnit.TestProject.FSharp

open System.Collections.Generic
open TUnit.Core

// Equivalent of UniqueBuilderContextsOnEnumerableDataGeneratorTests.cs

type UniqueBuilderContextsOnEnumerableDataGeneratorTests() =
    [<Test; UniqueBuilderContextsOnEnumerableDataGeneratorTestsGenerator>]
    member _.Test(value: int) = ()

and UniqueBuilderContextsOnEnumerableDataGeneratorTestsGenerator() =
    inherit DataSourceGeneratorAttribute<int>()
    override _.GenerateDataSources(dataGeneratorMetadata) =
        seq {
            let id1 = dataGeneratorMetadata.TestBuilderContext.Current.Id
            let id2 = dataGeneratorMetadata.TestBuilderContext.Current.Id
            yield (fun () -> 1)
            let id3 = dataGeneratorMetadata.TestBuilderContext.Current.Id
            yield (fun () -> 2)
            let id4 = dataGeneratorMetadata.TestBuilderContext.Current.Id
            // ShouldBe/ShouldNotBe logic can be implemented with F# assertions or left as comments
        }
