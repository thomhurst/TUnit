---
sidebar_position: 7
---

# Assertion Groups

Mixing 'Or' & 'And' conditions within `Assert.That(...)` statements may result in unexpected logic. 

```csharp
var value = "CD";

await Assert.That(value)
    .Contains('C').And.Contains('D')
    .Or
    .Contains('A').And.Contains('B');
```

It might look like `(C && D) || (A && B)` but it's actually `C && (D || A) && B`

Instead, if you want to combine complex assertion logic into a single assertion, assertion groups can be used to more clearly show a group of logic.

For example:

```csharp
var value = "CD";

var cd = AssertionGroup.For(value)
    .WithAssertion(assert => assert.Contains('C'))
    .And(assert => assert.Contains('D'));

var ab = AssertionGroup.ForSameValueAs(cd)
    .WithAssertion(assert => assert.Contains('A'))
    .And(assert => assert.Contains('B'));

await AssertionGroup.Assert(cd).Or(ab);
```

While more verbose, it's clearer how the logic will evaluate.