# FSharp

As awaiting doesn't work quite the same in F#, the syntax instead looks like this:

```
do! check Assert.That(...).IsSomething()
```

So a test could look like:

```
member this.CheckPositive() = async {

    let result = 1 + 1

    do! check (Assert.That(result).IsPositive())

}
```

F# is a lot more strict with type resolution when it comes to extension methods and method overloads. Because of that you may need to annotate the type for the assertions.

For example,

```
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

## taskAssert expression[​](#taskassert-expression "Direct link to taskAssert expression")

For Task-based tests you can also use the taskAssert computation expression to avoid wrapping every assertion in check.

To use it, open the namespace:

```
open TUnit.Assertions.FSharp.TaskAssert
```

Inside a taskAssert block you can directly do! the assertion:

```
[<Test>]

[<Category("Pass")>]

member _.Test3() = taskAssert {

    let value = "1"

    do! Assert.That(value).IsEqualTo("1")

}
```
