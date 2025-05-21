namespace TUnit.TestProject.FSharp

type TestDataSources() =
    static member One() = 1
    static member Two() = 2

    static member OneEnumerable() = [| 1; 1; 1; 1; 1; 1; 1; 1; 1; 1 |]
    static member OneFailingEnumerable() = [| 1; 2; 3; 4; 5; 6; 7; 8; 9 |]

