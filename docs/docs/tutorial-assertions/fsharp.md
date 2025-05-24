# FSharp

As awaiting doesn't work quite the same in F#, the syntax instead looks like this:

```fsharp
do! check Assert.That(...).IsSomething()
```

So a test could look like:

```fsharp
member this.CheckPositive() = async {
            let result = 1 + 1
            do! check (Assert.That(result).IsPositive())
        }
```

F# is a lot more strict with type resolution when it comes to extension methods and method overloads. Because of that you may need to annotate the type for the assertions.

For example,

```fsharp
    [<Test>]
    [<Category("Pass")>]
    member _.Test3() = async {
        let value = "1"
        do! check (Assert.That<string>(value).IsEqualTo("1"))
    }

    [<Test>]
    [<Category("Fail")>]
    member _.Throws1() = async {
        do! check (Assert.That<string>(fun () -> task { return new string([||]) }).ThrowsException())
    }

    [<Test>]
    [<Category("Pass")>]
    member _.Throws4() = async {
        do! check (Assert.That<bool>(fun () -> Task.FromResult(true)).ThrowsNothing())
    }

```    
