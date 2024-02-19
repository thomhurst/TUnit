---
sidebar_position: 1
---

# Retrying

Unfortunately sometimes our tests hit issues. It could be a blip on the network, but that could cause our entire test suite to fail which is frustrating.

If you want to retry a test, add a `[RetryAttribute]` onto your test method or class. This takes an `int` of how many times you'd like to retry.

This can be used on base classes and inherited to affect all tests in sub-classes.