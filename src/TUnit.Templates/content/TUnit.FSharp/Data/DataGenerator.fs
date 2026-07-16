namespace TestProject.Data

open System
open System.Collections.Generic
open TUnit.Core

type DataGenerator() =
    inherit DataSourceGeneratorAttribute<int, int, int>()

    override _.GenerateDataSources(_: DataGeneratorMetadata) : IEnumerable<Func<struct (int * int * int)>> =
        seq {
            yield Func<struct (int * int * int)>(fun () -> struct (1, 1, 2))
            yield Func<struct (int * int * int)>(fun () -> struct (1, 2, 3))
            yield Func<struct (int * int * int)>(fun () -> struct (4, 5, 9))
        }

