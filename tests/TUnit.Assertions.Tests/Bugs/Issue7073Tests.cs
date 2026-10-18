using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #7073: <c>IsEqualTo</c> with an argument of a different
/// numeric type compiled but threw at runtime.
///
/// <list type="bullet">
/// <item><c>Assert.That(3).IsEqualTo(3.0)</c> binds the cross-type <c>IsEqualTo&lt;TValue, TOther&gt;</c>
/// on every language version, which only looked for user-defined <c>op_Implicit</c> methods and so
/// missed the built-in <c>int</c> to <c>double</c> conversion.</item>
/// <item><c>Assert.That(3m).IsEqualTo(3)</c> and <c>Assert.That((decimal?)3m).IsEqualTo(3m)</c> bind the
/// same-type overload on C# 13+ via <c>[OverloadResolutionPriority]</c>, but C# 12 ignores ORP and binds
/// the cross-type overload, which tried to convert the actual value to the expected type (an explicit
/// conversion) and threw.</item>
/// </list>
///
/// This project builds with the latest language version, so the C# 12 binding is reproduced by calling
/// the cross-type overload explicitly (<c>EqualsAssertionExtensions.IsEqualTo&lt;decimal, int&gt;</c>).
/// </summary>
public class Issue7073Tests
{
    [Test]
    public async Task Int_IsEqualTo_Double_Passes()
    {
        await Assert.That(3).IsEqualTo(3.0);
    }

    [Test]
    public async Task Int_IsEqualTo_Double_Fails_When_Different()
    {
        await Assert.That(async () => await Assert.That(3).IsEqualTo(3.5))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Int_IsNotEqualTo_Double_Passes_When_Different()
    {
        await Assert.That(3).IsNotEqualTo(3.5);
    }

    [Test]
    public async Task Int_IsEqualTo_NullableLong_Passes()
    {
        await Assert.That(3).IsEqualTo((long?)3);
    }

    [Test]
    public async Task Char_IsEqualTo_Int_Passes()
    {
        await Assert.That('A').IsEqualTo(65);
    }

    [Test]
    public async Task String_IsEqualTo_Object_Passes()
    {
        await Assert.That("value").IsEqualTo((object)"value");
    }

    [Test]
    public async Task Decimal_IsEqualTo_Int_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<decimal, int>(Assert.That(3m), 3);
    }

    [Test]
    public async Task Decimal_IsEqualTo_Int_CrossTypeOverload_Fails_Without_Truncating()
    {
        var exception = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<decimal, int>(Assert.That(3.5m), 3))
            .Throws<AssertionException>();

        // Formatted like the failure message, so this holds in any culture (3,5 in de-DE).
        await Assert.That(exception!.Message).Contains($"received {3.5m}");
    }

    [Test]
    public async Task Decimal_IsEqualTo_Int_CrossTypeOverload_Or_Chain_Compares_Exact_Value()
    {
        await EqualsAssertionExtensions.IsEqualTo<decimal, int>(Assert.That(4m), 3).Or.IsEqualTo(4);
    }

    [Test]
    public async Task Decimal_IsEqualTo_Int_CrossTypeOverload_And_Chain_Reports_The_Equality_Failure()
    {
        // 3.5m has no int equivalent; the equality failure must be reported, not the mapping error.
        var exception = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<decimal, int>(Assert.That(3.5m), 3).And.IsGreaterThan(1))
            .Throws<AssertionException>();

        await Assert.That(exception!.Message).Contains($"received {3.5m}");
    }

    [Test]
    public async Task Decimal_IsNotEqualTo_Int_CrossTypeOverload_Passes_When_Different()
    {
        await NotEqualsAssertionExtensions.IsNotEqualTo<decimal, int>(Assert.That(3.5m), 3);
    }

    [Test]
    public async Task Decimal_IsNotEqualTo_Int_CrossTypeOverload_Fails_When_Equal()
    {
        await Assert.That(async () =>
                await NotEqualsAssertionExtensions.IsNotEqualTo<decimal, int>(Assert.That(3m), 3))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task NullableDecimal_IsEqualTo_Decimal_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<decimal?, decimal>(Assert.That((decimal?)3m), 3m);
    }

    [Test]
    public async Task NullableDecimal_IsEqualTo_Decimal_CrossTypeOverload_Fails_When_Null()
    {
        var exception = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<decimal?, decimal>(Assert.That((decimal?)null), 3m))
            .Throws<AssertionException>();

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task NullableDecimal_IsNotEqualTo_Decimal_CrossTypeOverload_Passes_When_Null()
    {
        await NotEqualsAssertionExtensions.IsNotEqualTo<decimal?, decimal>(Assert.That((decimal?)null), 3m);
    }

    [Test]
    public async Task Object_IsEqualTo_String_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<object, string>(Assert.That((object)"value"), "value");
    }

    [Test]
    public async Task Object_IsEqualTo_String_CrossTypeOverload_Chains_The_Actual_Reference()
    {
        // Equal contents, distinct instances: the chained assertion must see the actual object, not the expected one.
        var actual = new string('x', 3);
        var expected = new string('x', 3);

        await EqualsAssertionExtensions.IsEqualTo<object, string>(Assert.That((object)actual), expected)
            .And.IsSameReferenceAs(actual);
    }

    [Test]
    public async Task Object_IsEqualTo_String_CrossTypeOverload_Returns_The_Actual_Reference()
    {
        var actual = new string('x', 3);
        var expected = new string('x', 3);

        var result = await EqualsAssertionExtensions.IsEqualTo<object, string>(Assert.That((object)actual), expected);

        await Assert.That(ReferenceEquals(result, actual)).IsTrue();
    }

    [Test]
    public async Task Looser_Equality_CrossTypeOverload_Passes_And_Chains_The_Actual_Value()
    {
        // CaseInsensitiveName equality ignores case, so the comparison as the source type passes even
        // though the string forms differ. The chained assertion sees the actual value's string, "ABC".
        await EqualsAssertionExtensions.IsEqualTo<CaseInsensitiveName, string>(Assert.That(new CaseInsensitiveName("ABC")), "abc")
            .And.IsEqualTo("ABC");
    }

    [Test]
    public async Task Looser_Equality_CrossTypeOverload_IsNotEqualTo_Fails_When_Equal_As_Source_Type()
    {
        await Assert.That(async () =>
                await NotEqualsAssertionExtensions.IsNotEqualTo<CaseInsensitiveName, string>(Assert.That(new CaseInsensitiveName("ABC")), "abc"))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Null_Expected_CrossTypeOverload_Invokes_User_Conversion()
    {
        // The same-type overload would run `implicit operator NullSentinelWrapper(string?)` on null,
        // which maps it to the None sentinel, so this passes on C# 13+.
        await EqualsAssertionExtensions.IsEqualTo<NullSentinelWrapper, string>(Assert.That(NullSentinelWrapper.None), (string?)null);
    }

    [Test]
    public async Task Null_Expected_CrossTypeOverload_IsNotEqualTo_Invokes_User_Conversion()
    {
        await Assert.That(async () =>
                await NotEqualsAssertionExtensions.IsNotEqualTo<NullSentinelWrapper, string>(Assert.That(NullSentinelWrapper.None), (string?)null))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Null_Object_IsEqualTo_Null_String_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<object, string>(Assert.That((object?)null), (string?)null);
    }

    [Test]
    public async Task Failure_Message_Matches_The_Same_Type_Overload()
    {
        // The C# 12 binding reports what the generic same-type IsEqualTo<decimal> reports, in any culture.
        var crossType = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<decimal, int>(Assert.That(3.5m), 3))
            .Throws<AssertionException>();
        var sameType = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<decimal>(Assert.That(3.5m), 3))
            .Throws<AssertionException>();

        await Assert.That(crossType!.Message).IsEqualTo(sameType!.Message);
    }

    [Test]
    public async Task Null_Failure_Message_Matches_The_Same_Type_Overload()
    {
        var crossType = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<decimal?, decimal>(Assert.That((decimal?)null), 3m))
            .Throws<AssertionException>();
        var sameType = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<decimal?>(Assert.That((decimal?)null), 3m))
            .Throws<AssertionException>();

        await Assert.That(crossType!.Message).IsEqualTo(sameType!.Message);
    }

    [Test]
    public async Task Lifted_User_Defined_Conversion_CrossTypeOverload_Passes()
    {
        // Dollars? -> Cents? is the lifted form of `implicit operator Cents(Dollars)`, so C# 13+ binds the
        // same-type Cents? overload.
        await EqualsAssertionExtensions.IsEqualTo<Cents?, Dollars?>(Assert.That((Cents?)new Cents(300)), new Dollars(3));
    }

    [Test]
    public async Task Lifted_User_Defined_Conversion_CrossTypeOverload_Fails_When_Different()
    {
        await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<Cents?, Dollars?>(Assert.That((Cents?)new Cents(301)), new Dollars(3)))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Lifted_User_Defined_Conversion_Of_The_Actual_Value_Passes()
    {
        // No conversion from Cents? to Dollars?, so the actual Dollars? is converted to Cents? (lifted).
        await EqualsAssertionExtensions.IsEqualTo<Dollars?, Cents?>(Assert.That((Dollars?)new Dollars(3)), new Cents(300));
    }

    [Test]
    public async Task Lifted_User_Defined_Conversion_Of_Null_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<Cents?, Dollars?>(Assert.That((Cents?)null), (Dollars?)null);
    }

    [Test]
    public async Task Nullable_Boxing_To_Interface_CrossTypeOverload_Passes()
    {
        // int? -> IComparable is a boxing conversion, although Nullable<int> itself does not implement it.
        await EqualsAssertionExtensions.IsEqualTo<int?, IComparable>(Assert.That((int?)3), (IComparable)3);
    }

    [Test]
    public async Task Double_IsEqualTo_Char_CrossTypeOverload_Chains_The_Char()
    {
        // char -> double is implicit, so the values compare as double; the chain sees 65.0 as 'A'.
        var result = await EqualsAssertionExtensions.IsEqualTo<double, char>(Assert.That(65.0), 'A')
            .And.IsEqualTo('A');

        await Assert.That(result).IsEqualTo('A');
    }

    [Test]
    public async Task Int_IsEqualTo_Char_CrossTypeOverload_Chains_The_Char()
    {
        var result = await EqualsAssertionExtensions.IsEqualTo<int, char>(Assert.That(65), 'A')
            .And.IsEqualTo('A');

        await Assert.That(result).IsEqualTo('A');
    }

    [Test]
    public async Task IgnoringType_Applies_To_The_Source_Type_Comparison()
    {
        // C# 13+ binds the same-type `object` overload here, which honors IgnoringType.
        var actual = new Stamped { Name = "a", At = new DateTime(2000, 1, 1) };
        var expected = new Stamped { Name = "a", At = new DateTime(2001, 1, 1) };

        await EqualsAssertionExtensions.IsEqualTo<object, Stamped>(Assert.That((object)actual), expected)
            .IgnoringType<DateTime>();
    }

    [Test]
    public async Task IgnoringType_Applies_To_The_Source_Type_Inequality()
    {
        var actual = new Stamped { Name = "a", At = new DateTime(2000, 1, 1) };
        var expected = new Stamped { Name = "a", At = new DateTime(2001, 1, 1) };

        await Assert.That(async () =>
                await NotEqualsAssertionExtensions.IsNotEqualTo<object, Stamped>(Assert.That((object)actual), expected)
                    .IgnoringType<DateTime>())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Failing_Reverse_Conversion_Does_Not_Block_The_Source_Type_Comparison()
    {
        // Converting the sentinel back to string throws, but the comparison as the source type passes.
        await EqualsAssertionExtensions.IsEqualTo<OneWaySentinelWrapper, string>(Assert.That(OneWaySentinelWrapper.None), (string?)null);
    }

    [Test]
    public async Task Failing_Reverse_Conversion_Does_Not_Block_The_Source_Type_Inequality()
    {
        await NotEqualsAssertionExtensions.IsNotEqualTo<OneWaySentinelWrapper, string>(Assert.That(OneWaySentinelWrapper.None), "other");
    }

    [Test]
    public async Task Long_IsEqualTo_NativeInt_CrossTypeOverload_Passes()
    {
        // nint -> long is implicit, so C# 13+ binds the same-type long overload.
        await EqualsAssertionExtensions.IsEqualTo<long, nint>(Assert.That(3L), (nint)3);
    }

    [Test]
    public async Task Long_IsEqualTo_NativeInt_CrossTypeOverload_Fails_When_Different()
    {
        await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<long, nint>(Assert.That(4L), (nint)3))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task NativeInt_IsEqualTo_Long_CrossTypeOverload_Chains_The_Long()
    {
        // No implicit long -> nint, so the actual nint converts to long.
        var result = await EqualsAssertionExtensions.IsEqualTo<nint, long>(Assert.That((nint)3), 3L);

        await Assert.That(result).IsEqualTo(3L);
    }

    [Test]
    public async Task Long_IsEqualTo_NativeInt_CrossTypeOverload_Chains_The_NativeInt()
    {
        var result = await EqualsAssertionExtensions.IsEqualTo<long, nint>(Assert.That(3L), (nint)3)
            .And.IsEqualTo((nint)3);

        await Assert.That(result).IsEqualTo((nint)3);
    }

    [Test]
    public async Task NativeUInt_IsEqualTo_ULong_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<ulong, nuint>(Assert.That(3UL), (nuint)3);
    }

    [Test]
    public async Task Int_IsEqualTo_NativeInt_Passes()
    {
        // int -> nint is implicit, so this binds the cross-type overload on every language version.
        await EqualsAssertionExtensions.IsEqualTo<int, nint>(Assert.That(3), (nint)3);
    }

    [Test]
    public async Task UInt_IsEqualTo_Int_Constant_CrossTypeOverload_Passes()
    {
        // C# 13+ binds the same-type uint overload via the constant conversion 0 -> uint; C# 12 binds this one.
        await EqualsAssertionExtensions.IsEqualTo<uint, int>(Assert.That(0U), 0);
    }

    [Test]
    public async Task UInt_IsEqualTo_Int_Constant_CrossTypeOverload_Fails_When_Different()
    {
        await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<uint, int>(Assert.That(1U), 0))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task UInt_IsNotEqualTo_Int_Constant_CrossTypeOverload_Passes_When_Different()
    {
        await NotEqualsAssertionExtensions.IsNotEqualTo<uint, int>(Assert.That(1U), 0);
    }

    [Test]
    public async Task UInt_IsNotEqualTo_Int_Constant_CrossTypeOverload_Fails_When_Equal()
    {
        await Assert.That(async () =>
                await NotEqualsAssertionExtensions.IsNotEqualTo<uint, int>(Assert.That(0U), 0))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task NullableByte_IsEqualTo_Int_Constant_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<byte?, int>(Assert.That((byte?)7), 7);
    }

    [Test]
    public async Task ULong_IsEqualTo_Long_Constant_CrossTypeOverload_Chains_The_Long()
    {
        var result = await EqualsAssertionExtensions.IsEqualTo<ulong, long>(Assert.That(5UL), 5L)
            .And.IsEqualTo(5L);

        await Assert.That(result).IsEqualTo(5L);
    }

    [Test]
    public async Task UInt_IsEqualTo_Out_Of_Range_Int_CrossTypeOverload_Still_Reports_No_Conversion()
    {
        // -1 has no uint equivalent, so C# 13+ binds this overload too and there is no conversion either way.
        var exception = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<uint, int>(Assert.That(0U), -1))
            .Throws<AssertionException>();

        await Assert.That(exception!.InnerException).IsTypeOf<InvalidOperationException>();
    }

    public readonly record struct Cents(int Value);

    public readonly record struct Dollars(int Value)
    {
        public static implicit operator Cents(Dollars dollars) => new(dollars.Value * 100);
    }

    public sealed class Stamped
    {
        public string? Name { get; set; }

        public DateTime At { get; set; }
    }

    public sealed record OneWaySentinelWrapper(string Value)
    {
        public static readonly OneWaySentinelWrapper None = new("<none>");

        public static implicit operator OneWaySentinelWrapper(string? value) => value is null ? None : new(value);

        public static implicit operator string(OneWaySentinelWrapper wrapper)
            => ReferenceEquals(wrapper, None) ? throw new InvalidOperationException("The sentinel has no string value.") : wrapper.Value;
    }

    public sealed class CaseInsensitiveName(string value) : IEquatable<CaseInsensitiveName>
    {
        public string Value { get; } = value;

        public static implicit operator CaseInsensitiveName(string value) => new(value);

        public static implicit operator string(CaseInsensitiveName name) => name.Value;

        public bool Equals(CaseInsensitiveName? other)
            => other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => Equals(obj as CaseInsensitiveName);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public override string ToString() => Value;
    }

    public sealed record NullSentinelWrapper(string Value)
    {
        public static readonly NullSentinelWrapper None = new("<none>");

        public static implicit operator NullSentinelWrapper(string? value) => value is null ? None : new(value);
    }

    // Zero-to-enum (C# §10.2.4): C# 13+ binds Assert.That(Level.None).IsEqualTo(0) to the same-type overload,
    // C# 12 to the cross-type one, so the cross-type overload must compare in the enum type.
    [Test]
    public async Task Enum_IsEqualTo_Zero_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<Level, int>(Assert.That(Level.None), 0);
    }

    [Test]
    public async Task Enum_IsEqualTo_Zero_CrossTypeOverload_Fails_When_Not_Zero()
    {
        await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<Level, int>(Assert.That(Level.High), 0))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Enum_IsNotEqualTo_Zero_CrossTypeOverload_Passes_When_Not_Zero()
    {
        await NotEqualsAssertionExtensions.IsNotEqualTo<Level, int>(Assert.That(Level.High), 0);
    }

    [Test]
    public async Task NullableEnum_IsEqualTo_LongZero_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<Level?, long>(Assert.That((Level?)Level.None), 0L);
    }

    [Test]
    public async Task NullableEnum_IsEqualTo_Zero_CrossTypeOverload_Fails_When_Null()
    {
        await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<Level?, int>(Assert.That((Level?)null), 0))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Enum_IsEqualTo_DoubleZero_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<Level, double>(Assert.That(Level.None), 0.0);
    }

    [Test]
    public async Task Enum_IsEqualTo_Zero_CrossTypeOverload_And_Chain_Sees_Underlying_Value()
    {
        await EqualsAssertionExtensions.IsEqualTo<Level, int>(Assert.That(Level.None), 0).And.IsLessThan(1);
    }

    // C# applies a user-defined operator declared on a base class of the source or target type.
    [Test]
    public async Task Derived_IsEqualTo_String_Uses_Base_Class_Operator()
    {
        await Assert.That(new DerivedCode("x")).IsEqualTo("x");
    }

    [Test]
    public async Task String_IsEqualTo_Derived_Uses_Base_Class_Operator()
    {
        await Assert.That("x").IsEqualTo(new DerivedCode("x"));
    }

    [Test]
    public async Task Leaf_IsEqualTo_String_Uses_Most_Specific_Base_Class_Operator()
    {
        await Assert.That(new LeafCode("x")).IsEqualTo("mid:x");
    }

    public enum Level
    {
        None,
        High,
    }

    public class BaseCode(string value)
    {
        public string Value { get; } = value;

        public static implicit operator string(BaseCode code) => code.Value;
    }

    public sealed class DerivedCode(string value) : BaseCode(value);

    public class MidCode(string value) : BaseCode(value)
    {
        public static implicit operator string(MidCode code) => "mid:" + code.Value;
    }

    public sealed class LeafCode(string value) : MidCode(value);
}
