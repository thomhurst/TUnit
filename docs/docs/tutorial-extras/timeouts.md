---
sidebar_position: 4
---

# Timeouts

If you want to stop a test after a specified amount of time, add a `[TimeoutAttribute]` onto your test method or class. This takes an `int` of how many milliseconds a test can execute for.

This can be used on base classes and inherited to affect all tests in sub-classes.