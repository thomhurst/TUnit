using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7108;

/// <summary>
/// A table of [MethodDataSource] overload scenarios. Source-gen mode picks the overload with
/// <c>ScoreDataSourceOverload</c> in the generator, and reflection mode with the runtime binder and
/// <c>MethodDataSourceAttribute.ResolveDataSourceMethodWithNullArgument</c>. Each attribute is one row:
/// its first argument names the overload both modes must choose, and every overload yields
/// (expected, its own signature). Issue7108Tests runs the table in both modes, so a divergence between
/// the two resolvers fails the row in the mode that drifted. The test methods are not generic, so the
/// rows exercise the factory that every non-generic [MethodDataSource] test uses.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DataSourceOverloadParityTests
{
    public static IEnumerable<(string, string)> Count(string expected)
    {
        yield return (expected, "Count(string)");
    }

    public static IEnumerable<(string, string)> Count(string expected, int value)
    {
        yield return (expected, "Count(string, int)");
    }

    public static IEnumerable<(string, string)> Exact(string expected, long value)
    {
        yield return (expected, "Exact(string, long)");
    }

    public static IEnumerable<(string, string)> Exact(string expected, int value)
    {
        yield return (expected, "Exact(string, int)");
    }

    public static IEnumerable<(string, string)> Widening(string expected, string value)
    {
        yield return (expected, "Widening(string, string)");
    }

    public static IEnumerable<(string, string)> Widening(string expected, long value)
    {
        yield return (expected, "Widening(string, long)");
    }

    public static IEnumerable<(string, string)> Specific(string expected, object value)
    {
        yield return (expected, "Specific(string, object)");
    }

    public static IEnumerable<(string, string)> Specific(string expected, long value)
    {
        yield return (expected, "Specific(string, long)");
    }

    public static IEnumerable<(string, string)> FloatToDouble(string expected, string value)
    {
        yield return (expected, "FloatToDouble(string, string)");
    }

    public static IEnumerable<(string, string)> FloatToDouble(string expected, double value)
    {
        yield return (expected, "FloatToDouble(string, double)");
    }

    public static IEnumerable<(string, string)> LongExact(string expected, int value)
    {
        yield return (expected, "LongExact(string, int)");
    }

    public static IEnumerable<(string, string)> LongExact(string expected, long value)
    {
        yield return (expected, "LongExact(string, long)");
    }

    public static IEnumerable<(string, string)> NullableParameter(string expected, string value)
    {
        yield return (expected, "NullableParameter(string, string)");
    }

    public static IEnumerable<(string, string)> NullableParameter(string expected, int? value)
    {
        yield return (expected, "NullableParameter(string, int?)");
    }

    public static IEnumerable<(string, string)> EnumParameter(string expected, int value)
    {
        yield return (expected, "EnumParameter(string, int)");
    }

    public static IEnumerable<(string, string)> EnumParameter(string expected, DayOfWeek value)
    {
        yield return (expected, "EnumParameter(string, DayOfWeek)");
    }

    public static IEnumerable<(string, string)> Character(string expected, int value)
    {
        yield return (expected, "Character(string, int)");
    }

    public static IEnumerable<(string, string)> Character(string expected, char value)
    {
        yield return (expected, "Character(string, char)");
    }

    public static IEnumerable<(string, string)> ExactCount(string expected, object first, object second)
    {
        yield return (expected, "ExactCount(string, object, object)");
    }

    public static IEnumerable<(string, string)> ExactCount(string expected, string first, string second, int count = 1)
    {
        yield return (expected, "ExactCount(string, string, string, int)");
    }

    public static IEnumerable<(string, string)> Null(string expected, int value)
    {
        yield return (expected, "Null(string, int)");
    }

    public static IEnumerable<(string, string)> Null(string expected, string? value)
    {
        yield return (expected, "Null(string, string?)");
    }

    public static IEnumerable<(string, string)> NullTie(string expected, string? text, object value)
    {
        yield return (expected, "NullTie(string, string?, object)");
    }

    public static IEnumerable<(string, string)> NullTie(string expected, string? text, long value)
    {
        yield return (expected, "NullTie(string, string?, long)");
    }

    public static IEnumerable<(string, string)> NullOptional(string expected, int count)
    {
        yield return (expected, "NullOptional(string, int)");
    }

    public static IEnumerable<(string, string)> NullOptional(string expected, string? text, int count = 1)
    {
        yield return (expected, "NullOptional(string, string?, int)");
    }

    public static IEnumerable<(string, string)> Optional(string expected, string text, int count = 2)
    {
        yield return (expected, "Optional(string, string, int)");
    }

    public static IEnumerable<(string, string)> NullToNullable(string expected, int value)
    {
        yield return (expected, "NullToNullable(string, int)");
    }

    public static IEnumerable<(string, string)> NullToNullable(string expected, int? value)
    {
        yield return (expected, "NullToNullable(string, int?)");
    }

    // A generic overload is declared first: its T parameter cannot take an Arguments value in either mode
    public static IEnumerable<(string, string)> GenericOverload<T>(string expected, T value)
    {
        yield return (expected, "GenericOverload<T>(string, T)");
    }

    public static IEnumerable<(string, string)> GenericOverload(string expected, int value)
    {
        yield return (expected, "GenericOverload(string, int)");
    }

    public static IEnumerable<(string, string)> Params(string expected, params int[] values)
    {
        yield return (expected, "Params(string, params int[])");
    }

    public static IEnumerable<(string, string)> Params(string expected, int value)
    {
        yield return (expected, "Params(string, int)");
    }

    public static IEnumerable<(string, string)> Token(string expected, int value, CancellationToken cancellationToken)
    {
        yield return (expected, "Token(string, int, CancellationToken)");
    }

    // C# converts an int to decimal or long?, but reflection's binder and MethodInfo.Invoke cannot, so the object
    // overload declared first wins in both modes
    public static IEnumerable<(string, string)> ObjectOrDecimal(string expected, object value)
    {
        yield return (expected, "ObjectOrDecimal(string, object)");
    }

    public static IEnumerable<(string, string)> ObjectOrDecimal(string expected, decimal value)
    {
        yield return (expected, "ObjectOrDecimal(string, decimal)");
    }

    public static IEnumerable<(string, string)> ObjectOrNullableLong(string expected, object value)
    {
        yield return (expected, "ObjectOrNullableLong(string, object)");
    }

    public static IEnumerable<(string, string)> ObjectOrNullableLong(string expected, long? value)
    {
        yield return (expected, "ObjectOrNullableLong(string, long?)");
    }

    public static IEnumerable<(string, string)> NullObjectOrNullableLong(string expected, string? text, object value)
    {
        yield return (expected, "NullObjectOrNullableLong(string, string?, object)");
    }

    public static IEnumerable<(string, string)> NullObjectOrNullableLong(string expected, string? text, long? value)
    {
        yield return (expected, "NullObjectOrNullableLong(string, string?, long?)");
    }

    // A generic method definition declared first takes the same parameters, but reflection cannot invoke it
    // without type arguments, so both modes call the non-generic overload
    public static IEnumerable<(string, string)> GenericFirst<T>(string expected)
    {
        yield return (expected, "GenericFirst<T>(string)");
    }

    public static IEnumerable<(string, string)> GenericFirst(string expected)
    {
        yield return (expected, "GenericFirst(string)");
    }

    [Test]
    [MethodDataSource(nameof(Count), Arguments = new object[] { "Count(string)" })]
    [MethodDataSource(nameof(Count), Arguments = new object[] { "Count(string, int)", 1 })]
    [MethodDataSource(nameof(Exact), Arguments = new object[] { "Exact(string, int)", 1 })]
    [MethodDataSource(nameof(Widening), Arguments = new object[] { "Widening(string, long)", 1 })]
    [MethodDataSource(nameof(Specific), Arguments = new object[] { "Specific(string, long)", 1 })]
    [MethodDataSource(nameof(FloatToDouble), Arguments = new object[] { "FloatToDouble(string, double)", 1.5f })]
    [MethodDataSource(nameof(LongExact), Arguments = new object[] { "LongExact(string, long)", 1L })]
    [MethodDataSource(nameof(NullableParameter), Arguments = new object[] { "NullableParameter(string, int?)", 1 })]
    [MethodDataSource(nameof(EnumParameter), Arguments = new object[] { "EnumParameter(string, DayOfWeek)", DayOfWeek.Friday })]
    [MethodDataSource(nameof(Character), Arguments = new object[] { "Character(string, char)", 'a' })]
    [MethodDataSource(nameof(ExactCount), Arguments = new object[] { "ExactCount(string, object, object)", "a", "b" })]
    [MethodDataSource(nameof(GenericOverload), Arguments = new object[] { "GenericOverload(string, int)", 1 })]
    // A static overload hidden with `new` on a derived provider: both modes call the derived one
    [MethodDataSource(typeof(DataSourceOverloadParityDerivedProvider), nameof(DataSourceOverloadParityDerivedProvider.Hidden), Arguments = new object[] { "Derived.Hidden(string, int)", 1 })]
    // A better overload declared on a base type wins in both modes, and is called through the base type (#7202)
    [MethodDataSource(typeof(DataSourceOverloadParityDerivedProvider), nameof(DataSourceOverloadParityDerivedProvider.Split), Arguments = new object[] { "Base.Split(string, int)", 1 })]
    [MethodDataSource(typeof(DataSourceOverloadParityDerivedProvider), nameof(DataSourceOverloadParityDerivedProvider.InstanceSplit), Arguments = new object[] { "Base.InstanceSplit(string, int)", 1 })]
    // A private base overload is invisible to both modes, and an overridden one runs its override
    [MethodDataSource(typeof(DataSourceOverloadParityDerivedProvider), nameof(DataSourceOverloadParityDerivedProvider.PrivateSplit), Arguments = new object[] { "Derived.PrivateSplit(string, long)", 1 })]
    [MethodDataSource(typeof(DataSourceOverloadParityDerivedProvider), nameof(DataSourceOverloadParityDerivedProvider.Overridden), Arguments = new object[] { "Derived.Overridden(string, int)", 1 })]
    // An override called through its base declaration keeps its narrower (covariant) return type
    [MethodDataSource(typeof(DataSourceOverloadParityDerivedProvider), nameof(DataSourceOverloadParityDerivedProvider.Covariant), Arguments = new object[] { "Derived.Covariant(string, int)", 1 })]
    // Loose values for a params parameter are rejected by TUnit0001, so the array is passed as one argument
    [MethodDataSource(nameof(Params), Arguments = new object[] { "Params(string, params int[])", new[] { 1, 2 } })]
    [MethodDataSource(nameof(Params), Arguments = new object[] { "Params(string, int)", 1 })]
    [MethodDataSource(nameof(ObjectOrDecimal), Arguments = new object[] { "ObjectOrDecimal(string, object)", 1 })]
    [MethodDataSource(nameof(ObjectOrNullableLong), Arguments = new object[] { "ObjectOrNullableLong(string, object)", 1 })]
    [MethodDataSource(nameof(GenericFirst), Arguments = new object[] { "GenericFirst(string)" })]
    public async Task Chosen_Overload(string expected, string chosen)
    {
        await Assert.That(chosen).IsEqualTo(expected);
    }

    // TUnit0001 requires one argument per parameter and does not yet match a null argument (#7168).
    // With it suppressed, both modes still bind these rows at runtime.
#pragma warning disable TUnit0001
    [Test]
    [MethodDataSource(nameof(Null), Arguments = new object?[] { "Null(string, string?)", null })]
    [MethodDataSource(nameof(NullToNullable), Arguments = new object?[] { "NullToNullable(string, int?)", null })]
    [MethodDataSource(nameof(NullTie), Arguments = new object?[] { "NullTie(string, string?, long)", null, 1 })]
    [MethodDataSource(nameof(NullObjectOrNullableLong), Arguments = new object?[] { "NullObjectOrNullableLong(string, string?, object)", null, 1 })]
    [MethodDataSource(nameof(NullOptional), Arguments = new object?[] { "NullOptional(string, string?, int)", null })]
    [MethodDataSource(nameof(Optional), Arguments = new object[] { "Optional(string, string, int)", "a" })]
    [MethodDataSource(nameof(Token), Arguments = new object[] { "Token(string, int, CancellationToken)", 1 })]
    // An override called through its base declaration passes its own default, as reflection does, not the base's
    [MethodDataSource(typeof(DataSourceOverloadParityDerivedProvider), nameof(DataSourceOverloadParityDerivedProvider.OverriddenDefault), Arguments = new object?[] { "Derived.OverriddenDefault(string, string?, int = 2)", null })]
    public async Task Chosen_Overload_With_Missing_Or_Null_Arguments(string expected, string chosen)
    {
        await Assert.That(chosen).IsEqualTo(expected);
    }
#pragma warning restore TUnit0001
}

public class DataSourceOverloadParityBaseProvider
{
    public static IEnumerable<(string, string)> Hidden(string expected, int value)
    {
        yield return (expected, "Base.Hidden(string, int)");
    }

    public static IEnumerable<(string, string)> Split(string expected, int value)
    {
        yield return (expected, "Base.Split(string, int)");
    }

    public IEnumerable<(string, string)> InstanceSplit(string expected, int value)
    {
        yield return (expected, "Base.InstanceSplit(string, int)");
    }

    private static IEnumerable<(string, string)> PrivateSplit(string expected, int value)
    {
        yield return (expected, "Base.PrivateSplit(string, int)");
    }

    public virtual IEnumerable<(string, string)> Overridden(string expected, int value)
    {
        yield return (expected, "Base.Overridden(string, int)");
    }

    public virtual IEnumerable<(string, string)> OverriddenDefault(string expected, string? key, int count = 1)
    {
        yield return (expected, $"Base.OverriddenDefault(string, string?, int = {count})");
    }

    public virtual object Covariant(string expected, int value)
    {
        return Task.FromResult<IEnumerable<(string, string)>>([(expected, "Base.Covariant(string, int)")]);
    }
}

public class DataSourceOverloadParityDerivedProvider : DataSourceOverloadParityBaseProvider
{
    public new static IEnumerable<(string, string)> Hidden(string expected, int value)
    {
        yield return (expected, "Derived.Hidden(string, int)");
    }

    public static IEnumerable<(string, string)> Split(string expected, long value)
    {
        yield return (expected, "Derived.Split(string, long)");
    }

    public IEnumerable<(string, string)> InstanceSplit(string expected, long value)
    {
        yield return (expected, "Derived.InstanceSplit(string, long)");
    }

    public static IEnumerable<(string, string)> PrivateSplit(string expected, long value)
    {
        yield return (expected, "Derived.PrivateSplit(string, long)");
    }

    public override IEnumerable<(string, string)> Overridden(string expected, int value)
    {
        yield return (expected, "Derived.Overridden(string, int)");
    }

    public IEnumerable<(string, string)> Overridden(string expected, long value)
    {
        yield return (expected, "Derived.Overridden(string, long)");
    }

    // Renames and re-defaults the parameter: reflection passes this override's default, 2
    public override IEnumerable<(string, string)> OverriddenDefault(string expected, string? name, int amount = 2)
    {
        yield return (expected, $"Derived.OverriddenDefault(string, string?, int = {amount})");
    }

    // Not applicable to a string, but shadows the base declaration so the override is called through the base type
    public IEnumerable<(string, string)> OverriddenDefault(int value)
    {
        yield return ("OverriddenDefault(int)", "Derived.OverriddenDefault(int)");
    }

    // A covariant return: the factory awaits the Task, which a call typed as the base's object could not supply
    public override Task<IEnumerable<(string, string)>> Covariant(string expected, int value)
    {
        return Task.FromResult<IEnumerable<(string, string)>>([(expected, "Derived.Covariant(string, int)")]);
    }

    public Task<IEnumerable<(string, string)>> Covariant(string expected, long value)
    {
        return Task.FromResult<IEnumerable<(string, string)>>([(expected, "Derived.Covariant(string, long)")]);
    }
}
