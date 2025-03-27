---
sidebar_position: 13
---

# Class Constructor Helpers

Some test suites might be more complex than others, and a user may want control over 'newing' up their test classes.
This control is given to you by the `[ClassConstructorAttribute<T>]` - Where `T` is a class that implements `IClassConstructor`.

This interface simply requires you to generate a `T` object - How you do that is up to you!

You can also add [event-subscribing interfaces](event-subscribing.md) to get notified for things like when the test has finished. This functionality can be used to dispose objects afterwards, etc.

These attributes are new'd up per test, so you can store state within them.