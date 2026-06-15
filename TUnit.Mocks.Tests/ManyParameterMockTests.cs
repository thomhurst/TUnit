using System.Threading.Tasks;
using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// Comprehensive coverage for mocking methods with large / boundary parameter counts (issue #6254).
//
// The generator's typed Returns/ReturnsAsync/Callback/Throws convenience overloads build
// System.Func<>/System.Action<> from the parameter list. Those BCL delegates accept at most 16
// input type arguments, so the generator only emits the parameter-typed overloads when the count is
// <= MaxDelegateParams (16). Methods past the limit still get the wrapper and its UNTYPED surface.
//
// These tests assert real behaviour (argument routing, matching, verification) — not just that the
// code compiles — across parameter count, parameter type, the `params` keyword, and method shape.
// Note: the very fact each interface below compiles is itself a regression assertion (typed
// overloads emitted past arity 16 would be CS0305; a 17-param method that compiles proves they were
// correctly omitted).

#region Supporting types

public enum ManyParamColor { Red, Green, Blue }

public readonly struct ManyParamMoney
{
    public ManyParamMoney(decimal amount) => Amount = amount;
    public decimal Amount { get; }
}

public sealed class ManyParamWidget
{
    public ManyParamWidget(string name) => Name = name;
    public string Name { get; }
}

#endregion

#region Mock targets

public interface IBoundarySixteen
{
    // Exactly 16 params — the maximum for which Func<...,TResult>/Action<...> exist, so the typed
    // overloads MUST still be emitted.
    Task<int> Sum(int a, int b, int c, int d, int e, int f, int g, int h,
        int i, int j, int k, int l, int m, int n, int o, int p);

    Task Capture(int a, int b, int c, int d, int e, int f, int g, int h,
        int i, int j, int k, int l, int m, int n, int o, int p);
}

public interface IBoundarySeventeen
{
    // 17 params — one past the limit. Typed overloads must be omitted (else CS0305).
    Task<int> Sum(int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9,
        int _10, int _11, int _12, int _13, int _14, int _15, int _16, int _17);
}

public interface IExtremeThirtyTwo
{
    Task<int> Big(int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8,
        int _9, int _10, int _11, int _12, int _13, int _14, int _15, int _16,
        int _17, int _18, int _19, int _20, int _21, int _22, int _23, int _24,
        int _25, int _26, int _27, int _28, int _29, int _30, int _31, int _32);
}

public interface IMixedTypedTwelve
{
    // 12 params (<= 16): typed overloads emitted. Exercises the typed-delegate cast path
    // ((T)args[i] with/without the null-forgiving operator) across reference, value, nullable,
    // enum, struct and custom types.
    Task<string> Build(string s, int i, bool b, double d, long l, char c,
        ManyParamColor col, ManyParamWidget w, ManyParamMoney m, string? ns, int? ni, byte bt);
}

public interface IMixedBigEighteen
{
    // 18 params (> 16): no typed overloads. Exercises the object?[] forwarding path with mixed
    // (incl. nullable / enum / struct / custom) types.
    Task<string> Build(string s1, int i1, bool b1, double d1, long l1, char c1,
        ManyParamColor col1, ManyParamWidget w1, ManyParamMoney m1, string? ns1, int? ni1, byte bt1,
        string s2, int i2, bool b2, ManyParamColor col2, string? ns2, int? ni2);
}

public interface INullableValueEighteen
{
    Task<int> CountSet(int? _1, int? _2, int? _3, int? _4, int? _5, int? _6,
        int? _7, int? _8, int? _9, int? _10, int? _11, int? _12,
        int? _13, int? _14, int? _15, int? _16, int? _17, int? _18);
}

public interface INullableRefEighteen
{
    Task<int> CountNonNull(string? _1, string? _2, string? _3, string? _4, string? _5, string? _6,
        string? _7, string? _8, string? _9, string? _10, string? _11, string? _12,
        string? _13, string? _14, string? _15, string? _16, string? _17, string? _18);
}

public interface IParamsUnderLimit
{
    // 2 params (the params array counts as one): typed overloads present.
    Task<int> Concat(string head, params int[] rest);
}

public interface IParamsOverLimit
{
    // 16 fixed params + a params array = 17 parameters: no typed overloads.
    Task Record(string a1, string a2, string a3, string a4, string a5, string a6, string a7, string a8,
        string a9, string a10, string a11, string a12, string a13, string a14, string a15, string a16,
        params int[] tail);
}

public interface IVoidSyncTwenty
{
    // Non-async void with 20 params: falls back to VoidMockMethodCall (no typed wrapper).
    void Run(int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9, int _10,
        int _11, int _12, int _13, int _14, int _15, int _16, int _17, int _18, int _19, int _20);
}

public interface ISyncReturningTwenty
{
    // Non-async returning value with 20 params: falls back to MockMethodCall<int>.
    int Compute(int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9, int _10,
        int _11, int _12, int _13, int _14, int _15, int _16, int _17, int _18, int _19, int _20);
}

public interface IValueTaskTwenty
{
    ValueTask<int> Calc(int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9, int _10,
        int _11, int _12, int _13, int _14, int _15, int _16, int _17, int _18, int _19, int _20);
}

public interface IGenericSeventeen
{
    // Generic method with 17 params: a typed overload would be Func<T,...,T,Task<T>> with 18 type
    // args — also CS0305. The fix must protect generic methods too.
    Task<T> Pick<T>(T _1, T _2, T _3, T _4, T _5, T _6, T _7, T _8, T _9,
        T _10, T _11, T _12, T _13, T _14, T _15, T _16, T _17);
}

#endregion

public class ManyParameterMockTests
{
    #region Boundary: typed overloads present at exactly 16

    [Test]
    public async Task SixteenParams_TypedReturns_ReceivesAllArguments()
    {
        var mock = IBoundarySixteen.Mock();

        // Typed Returns(Func<int,...,int,int>) exists only at <= 16 params. Distinct powers of two
        // sum to 65535 iff every one of the 16 arguments reached the delegate.
        mock.Sum(AnyArgs()).Returns((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
            a + b + c + d + e + f + g + h + i + j + k + l + m + n + o + p);

        var result = await mock.Object.Sum(1, 2, 4, 8, 16, 32, 64, 128,
            256, 512, 1024, 2048, 4096, 8192, 16384, 32768);

        await Assert.That(result).IsEqualTo(65535);
    }

    [Test]
    public async Task SixteenParams_TypedCallback_RoutesArgumentsInOrder()
    {
        var mock = IBoundarySixteen.Mock();

        int[] captured = null!;
        mock.Capture(AnyArgs()).Callback((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
            captured = new[] { a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p });

        await mock.Object.Capture(10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25);

        await Assert.That(captured).IsEquivalentTo(
            new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 });
    }

    [Test]
    public async Task SixteenParams_TypedThrows_Works()
    {
        var mock = IBoundarySixteen.Mock();

        // Typed Throws(Func<int,...,int,Exception>) — Func with 16 inputs + Exception result.
        mock.Sum(AnyArgs()).Throws((a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
            new InvalidOperationException($"sum={a + p}"));

        await Assert.That(async () => await mock.Object.Sum(
                3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4))
            .Throws<InvalidOperationException>()
            .WithMessage("sum=7");
    }

    #endregion

    #region Boundary: typed overloads omitted at 17 (compiles => proof)

    [Test]
    public async Task SeventeenParams_UntypedSurface_Works()
    {
        var mock = IBoundarySeventeen.Mock();
        mock.Sum(AnyArgs()).ReturnsAsync(Task.FromResult(123));

        var result = await mock.Object.Sum(1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13, 14, 15, 16, 17);

        await Assert.That(result).IsEqualTo(123);
        mock.Sum(AnyArgs()).WasCalled(Times.Once);
    }

    #endregion

    #region Count: well past the limit

    [Test]
    public async Task ThirtyTwoParams_Works()
    {
        var mock = IExtremeThirtyTwo.Mock();
        mock.Big(AnyArgs()).ReturnsAsync(Task.FromResult(99));

        var result = await mock.Object.Big(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
            21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);

        await Assert.That(result).IsEqualTo(99);
        mock.Big(AnyArgs()).WasCalled(Times.Once);
    }

    #endregion

    #region Parameter types

    [Test]
    public async Task MixedTypes_Twelve_TypedCallback_CastsEachParameterCorrectly()
    {
        var mock = IMixedTypedTwelve.Mock();
        var widget = new ManyParamWidget("gadget");

        string? capturedString = "unset";
        int capturedInt = 0;
        bool capturedBool = false;
        ManyParamColor capturedColor = default;
        ManyParamWidget capturedWidget = null!;
        ManyParamMoney capturedMoney = default;
        string? capturedNullableString = "unset";
        int? capturedNullableInt = -1;

        // Typed Callback(Action<string,int,bool,...,string?,int?,byte>). The generator casts each
        // object?[] slot back to its declared type — non-nullable slots use the null-forgiving
        // operator, nullable slots must NOT (so an actual null flows through).
        mock.Build(AnyArgs()).Callback((s, i, b, d, l, c, col, w, m, ns, ni, bt) =>
        {
            capturedString = s;
            capturedInt = i;
            capturedBool = b;
            capturedColor = col;
            capturedWidget = w;
            capturedMoney = m;
            capturedNullableString = ns;
            capturedNullableInt = ni;
        });

        await mock.Object.Build("hello", 42, true, 3.14, 99L, 'z',
            ManyParamColor.Blue, widget, new ManyParamMoney(12.5m), null, null, (byte)7);

        await Assert.That(capturedString).IsEqualTo("hello");
        await Assert.That(capturedInt).IsEqualTo(42);
        await Assert.That(capturedBool).IsTrue();
        await Assert.That(capturedColor).IsEqualTo(ManyParamColor.Blue);
        await Assert.That(capturedWidget).IsSameReferenceAs(widget);
        await Assert.That(capturedMoney.Amount).IsEqualTo(12.5m);
        await Assert.That(capturedNullableString).IsNull();
        await Assert.That(capturedNullableInt).IsNull();
    }

    [Test]
    public async Task MixedTypes_Eighteen_Forwarding_Works()
    {
        var mock = IMixedBigEighteen.Mock();
        mock.Build(AnyArgs()).ReturnsAsync(Task.FromResult("done"));

        var result = await mock.Object.Build("a", 1, true, 1.0, 2L, 'x',
            ManyParamColor.Red, new ManyParamWidget("w"), new ManyParamMoney(1m), null, null, (byte)3,
            "b", 2, false, ManyParamColor.Green, "tail", 7);

        await Assert.That(result).IsEqualTo("done");
        mock.Build(AnyArgs()).WasCalled(Times.Once);
    }

    [Test]
    public async Task NullableValueTypes_Eighteen_Works()
    {
        var mock = INullableValueEighteen.Mock();
        mock.CountSet(AnyArgs()).ReturnsAsync(Task.FromResult(5));

        var result = await mock.Object.CountSet(1, null, 3, null, 5, null,
            7, null, 9, null, 11, null, 13, null, 15, null, 17, null);

        await Assert.That(result).IsEqualTo(5);
        mock.CountSet(AnyArgs()).WasCalled(Times.Once);
    }

    [Test]
    public async Task NullableRefTypes_Eighteen_Works()
    {
        var mock = INullableRefEighteen.Mock();
        mock.CountNonNull(AnyArgs()).ReturnsAsync(Task.FromResult(2));

        var result = await mock.Object.CountNonNull("x", null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, "y");

        await Assert.That(result).IsEqualTo(2);
    }

    #endregion

    #region params keyword

    [Test]
    public async Task ParamsKeyword_UnderLimit_TypedCallbackReceivesArray()
    {
        var mock = IParamsUnderLimit.Mock();

        string capturedHead = null!;
        int[] capturedRest = null!;
        // Typed Returns(Func<string, int[], int>) for a params method: the params array surfaces as
        // a single int[] delegate parameter. The factory both captures and computes the result.
        mock.Concat(Any(), Any()).Returns((head, rest) =>
        {
            capturedHead = head;
            capturedRest = rest;
            return rest.Length;
        });

        var length = await mock.Object.Concat("prefix", 1, 2, 3);

        await Assert.That(capturedHead).IsEqualTo("prefix");
        await Assert.That(capturedRest).IsEquivalentTo(new[] { 1, 2, 3 });
        await Assert.That(length).IsEqualTo(3);
    }

    [Test]
    public async Task ParamsKeyword_OverLimit_Works()
    {
        var mock = IParamsOverLimit.Mock();

        await mock.Object.Record("1", "2", "3", "4", "5", "6", "7", "8",
            "9", "10", "11", "12", "13", "14", "15", "16", 100, 200, 300);

        mock.Record(AnyArgs()).WasCalled(Times.Once);
    }

    #endregion

    #region Method shapes

    [Test]
    public async Task VoidSync_TwentyParams_Works()
    {
        var mock = IVoidSyncTwenty.Mock();
        var fired = false;
        mock.Run(AnyArgs()).Callback(() => fired = true);

        mock.Object.Run(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        await Assert.That(fired).IsTrue();
        mock.Run(AnyArgs()).WasCalled(Times.Once);
    }

    [Test]
    public async Task SyncReturning_TwentyParams_Works()
    {
        var mock = ISyncReturningTwenty.Mock();
        mock.Compute(AnyArgs()).Returns(777);

        var result = mock.Object.Compute(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        await Assert.That(result).IsEqualTo(777);
        mock.Compute(AnyArgs()).WasCalled(Times.Once);
    }

    [Test]
    public async Task ValueTask_TwentyParams_Works()
    {
        var mock = IValueTaskTwenty.Mock();
        mock.Calc(AnyArgs()).Returns(55);

        var result = await mock.Object.Calc(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        await Assert.That(result).IsEqualTo(55);
    }

    [Test]
    public async Task GenericMethod_SeventeenParams_Works()
    {
        var mock = IGenericSeventeen.Mock();
        mock.Pick<int>(Any(), Any(), Any(), Any(), Any(), Any(), Any(), Any(), Any(),
            Any(), Any(), Any(), Any(), Any(), Any(), Any(), Any())
            .ReturnsAsync(Task.FromResult(7));

        var result = await mock.Object.Pick(1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13, 14, 15, 16, 17);

        await Assert.That(result).IsEqualTo(7);
    }

    #endregion

    #region Behaviour depth: matching & verification across many params

    [Test]
    public async Task ManyParams_PerSlotMatching_RoutesEveryArgument()
    {
        var mock = IBoundarySeventeen.Mock();

        // A specific matcher per slot. The configured value is returned ONLY when all 17 arguments
        // match — proving the matcher array is wired slot-for-slot with no off-by-one.
        mock.Sum(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17)
            .ReturnsAsync(Task.FromResult(900));

        var matched = await mock.Object.Sum(1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13, 14, 15, 16, 17);
        // Differs only in the last slot — must NOT match the specific setup.
        var unmatched = await mock.Object.Sum(1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13, 14, 15, 16, 99);

        await Assert.That(matched).IsEqualTo(900);
        await Assert.That(unmatched).IsEqualTo(0);
    }

    [Test]
    public async Task ManyParams_Verification_CountsAndNever()
    {
        var mock = IBoundarySeventeen.Mock();
        var svc = mock.Object;

        await svc.Sum(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17);
        await svc.Sum(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17);

        mock.Sum(AnyArgs()).WasCalled(Times.Exactly(2));
        mock.Sum(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).WasNeverCalled();
    }

    [Test]
    public async Task ManyParams_DefaultReturn_WhenNoSetup()
    {
        var mock = IBoundarySeventeen.Mock();

        // No setup: an async method still returns a completed task with the default result.
        var result = await mock.Object.Sum(1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13, 14, 15, 16, 17);

        await Assert.That(result).IsEqualTo(0);
    }

    #endregion
}
