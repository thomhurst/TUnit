---
sidebar_position: 10
---

# FSharp

As awaiting doesn't work quite the same in F#, the syntax instead looks like this:

```csharp
do! check Assert.That(...).IsSomething()
```

So a test could look like:

```csharp
member this.CheckPositive() = async {
            let result = 1 + 1
            do! check (Assert.That(result).IsPositive())
        }
```