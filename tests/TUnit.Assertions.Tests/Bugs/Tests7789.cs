using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Assertions.Strings;
using TUnit.Assertions.Core;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7789:
/// collection checks were also assertion sources, so <c>Assert.That(list).IsEmpty().Contains(1)</c>
/// compiled and silently dropped <c>IsEmpty()</c>. A check must not be a source, so the next
/// assertion can only be reached through <c>And</c> or <c>Or</c>.
/// </summary>
public class Tests7789
{
    // Changes the value under test rather than checking it, so chaining from it drops nothing.
    private static readonly Type[] AllowedAssertionSources = [typeof(WhenParsedIntoAssertion<>)];

    [Test]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test scans the assertions assembly.")]
    public async Task No_Assertion_Is_Also_An_Assertion_Source()
    {
        var offenders = typeof(Assertion<>).Assembly.GetTypes()
            .Where(t => IsAssertion(t) && IsAssertionSource(t))
            .Where(t => !AllowedAssertionSources.Contains(t.IsGenericType ? t.GetGenericTypeDefinition() : t))
            .Select(t => t.FullName)
            .ToList();

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task And_Still_Checks_Both_Assertions()
    {
        List<int> list = [1];

        await Assert.That(async () => await Assert.That(list).IsEmpty().And.Contains(1))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Or_Still_Passes_When_Second_Assertion_Passes()
    {
        List<int> list = [1];

        await Assert.That(list).IsEmpty().Or.Contains(1);
        await Assert.That(list).Contains(1).Or.IsEmpty();
    }

    [Test]
    public async Task And_Fails_When_Second_Assertion_Fails()
    {
        List<int> list = [1];

        await Assert.That(async () => await Assert.That(list).Contains(1).And.IsEmpty())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Because_Keeps_Typed_Continuations()
    {
        List<int> list = [1];
        var dictionary = new Dictionary<string, int> { ["a"] = 1 };

        await Assert.That(list).IsNotEmpty().Because("why").And.HasCount(1);
        await Assert.That(list).IsNotNull().Because("why").And.HasItemAt(0, 1);
        await Assert.That(dictionary).IsNotEmpty().Because("why").And.ContainsKey("a");
    }

    [Test]
    public async Task Because_Message_Is_Reported()
    {
        List<int> list = [1];

        var exception = await Assert.That(async () => await Assert.That(list).IsEmpty().Because("it was cleared").And.HasCount(1))
            .Throws<AssertionException>();

        await Assert.That(exception!.Message).Contains("because it was cleared");
    }

    private static bool IsAssertion(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Assertion<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAssertionSource(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAssertionSource<>));
}
