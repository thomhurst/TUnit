namespace TUnit.TestProject.FSharp

open System
open System.Collections.Generic
open TUnit.Core

// F# equivalent of GenericMethodTests.cs

type GenericMethodTests() =
    [<Test>]
    [<MethodDataSource("AggregateBy_Numeric_TestData")>]
    [<MethodDataSource("AggregateBy_String_TestData")>]
    member _.AggregateBy_HasExpectedOutput
        (source: seq<'TSource>,
         keySelector: 'TSource -> 'TKey,
         seedSelector: 'TKey -> 'TAccumulate,
         func: 'TAccumulate -> 'TSource -> 'TAccumulate,
         comparer: IEqualityComparer<'TKey>,
         expected: seq<KeyValuePair<'TKey, 'TAccumulate>>) =
        let enumerable = source |> Seq.toArray
        Console.WriteLine(String.Join(", ", enumerable))
        Console.WriteLine(String.Join(", ", enumerable |> Seq.map keySelector))
        Console.WriteLine(String.Join(", ", enumerable |> Seq.map (fun x -> seedSelector (keySelector x))))

    static member AggregateBy_Numeric_TestData() =
        seq {
            fun () ->
                let source = seq { 0 .. 9 }
                let keySelector = id
                let seedSelector = fun _ -> 0
                let func = fun x y -> x + y
                let comparer = null
                let expected =
                    seq { for x in 0 .. 9 -> KeyValuePair(x, x) }
                (source, keySelector, seedSelector, func, comparer, expected)
        }

    static member AggregateBy_String_TestData() =
        seq {
            fun () ->
                let source = [ "Bob"; "bob"; "tim"; "Bob"; "Tim" ]
                let keySelector = id
                let seedSelector = fun _ -> ""
                let func = fun x y -> x + y
                let comparer = null
                let expected =
                    [ KeyValuePair("Bob", "BobBob"); KeyValuePair("bob", "bob"); KeyValuePair("tim", "tim"); KeyValuePair("Tim", "Tim") ]
                (source, keySelector, seedSelector, func, comparer, expected)
        }
