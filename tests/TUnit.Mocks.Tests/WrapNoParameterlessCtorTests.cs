using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

// Regression coverage for issue #6253: wrapping a real object whose class has NO accessible
// parameterless constructor used to emit a wrapper that didn't compile (CS7036). The wrapper now
// chains to the fewest-parameter base constructor with default arguments. Because every member
// delegates to the wrapped instance, the wrapper's own (default-constructed) base sub-object is
// never observed — each test below asserts the call returns the REAL instance's data, proving the
// default base args don't leak into behaviour.
//
// Test ctor bodies are deliberately side-effect-free (they only store args). A base ctor that
// validates or dereferences its arguments would run with defaults during wrapper construction;
// that is an inherent limitation of source-generated wrappers (the base ctor cannot be skipped)
// and matches the existing partial-mock constructor-dispatch behaviour.

#region Wrap targets (none has a parameterless constructor)

public class WrapSingleRefParam
{
    private readonly string _name;
    public WrapSingleRefParam(string name) => _name = name;
    public virtual string Greet() => $"Hi {_name}";
}

public class WrapSingleValueParam
{
    private readonly int _seed;
    public WrapSingleValueParam(int seed) => _seed = seed;
    public virtual int Next() => _seed + 1;
}

public class WrapMultiParam
{
    private readonly string _label;
    private readonly int _count;
    private readonly double _factor;
    public WrapMultiParam(string label, int count, double factor)
    {
        _label = label;
        _count = count;
        _factor = factor;
    }
    public virtual string Compute() => $"{_label}:{_count}:{_factor}";
}

public class WrapParamsOnly
{
    private readonly int[] _nums;
    public WrapParamsOnly(params int[] nums) => _nums = nums;
    public virtual int Sum()
    {
        var total = 0;
        foreach (var n in _nums) total += n;
        return total;
    }
}

public class WrapLeadingThenParams
{
    private readonly string _prefix;
    private readonly string[] _rest;
    public WrapLeadingThenParams(string prefix, params string[] rest)
    {
        _prefix = prefix;
        _rest = rest;
    }
    public virtual string Join() => _prefix + ":" + string.Join(",", _rest);
}

public class WrapMultipleOverloads
{
    private readonly string _tag;
    private readonly int _n;
    public WrapMultipleOverloads(string tag) : this(tag, 0) { }
    public WrapMultipleOverloads(string tag, int n)
    {
        _tag = tag;
        _n = n;
    }
    public virtual string Tag() => $"{_tag}#{_n}";
}

public class WrapOptionalParam
{
    private readonly int _x;
    public WrapOptionalParam(int x = 5) => _x = x;
    public virtual int Value() => _x;
}

public class WrapNullableParams
{
    private readonly string? _a;
    private readonly int? _b;
    public WrapNullableParams(string? a, int? b)
    {
        _a = a;
        _b = b;
    }
    public virtual string Desc() => $"{_a ?? "null"}/{(_b.HasValue ? _b.Value.ToString() : "null")}";
}

public class WrapInParam
{
    private readonly int _x;
    public WrapInParam(in int x) => _x = x;
    public virtual int Get() => _x;
}

public sealed class WrapDependency
{
    public WrapDependency(string name) => Name = name;
    public string Name { get; }
}

public class WrapRefTypeParam
{
    private readonly WrapDependency _dep;
    public WrapRefTypeParam(WrapDependency dep) => _dep = dep;
    public virtual string DepName() => _dep.Name;
}

public readonly struct WrapPoint
{
    public WrapPoint(int x, int y)
    {
        X = x;
        Y = y;
    }
    public int X { get; }
    public int Y { get; }
}

public class WrapStructParam
{
    private readonly WrapPoint _p;
    public WrapStructParam(WrapPoint p) => _p = p;
    public virtual string Coords() => $"{_p.X},{_p.Y}";
}

public class WrapGeneric<T>
{
    private readonly T _value;
    public WrapGeneric(T value) => _value = value;
    public virtual T GetValue() => _value;
}

#endregion

public class WrapNoParameterlessCtorTests
{
    [Test]
    public async Task Single_Reference_Param()
    {
        var mock = Mock.Wrap(new WrapSingleRefParam("world"));
        await Assert.That(mock.Object.Greet()).IsEqualTo("Hi world");
    }

    [Test]
    public async Task Single_Reference_Param_Setup_Overrides()
    {
        var mock = Mock.Wrap(new WrapSingleRefParam("world"));
        mock.Greet().Returns("mocked");
        await Assert.That(mock.Object.Greet()).IsEqualTo("mocked");
    }

    [Test]
    public async Task Single_Value_Param()
    {
        var mock = Mock.Wrap(new WrapSingleValueParam(41));
        await Assert.That(mock.Object.Next()).IsEqualTo(42);
    }

    [Test]
    public async Task Multiple_Params()
    {
        var mock = Mock.Wrap(new WrapMultiParam("widget", 3, 1.5));
        await Assert.That(mock.Object.Compute()).IsEqualTo("widget:3:1.5");
    }

    [Test]
    public async Task Params_Keyword_Only()
    {
        var mock = Mock.Wrap(new WrapParamsOnly(1, 2, 3, 4));
        await Assert.That(mock.Object.Sum()).IsEqualTo(10);
    }

    [Test]
    public async Task Leading_Param_Then_Params_Keyword()
    {
        var mock = Mock.Wrap(new WrapLeadingThenParams("p", "a", "b"));
        await Assert.That(mock.Object.Join()).IsEqualTo("p:a,b");
    }

    [Test]
    public async Task Multiple_Constructor_Overloads_None_Parameterless()
    {
        // Real instance built via the 2-arg overload; wrapper chains to the fewest-param ctor.
        var mock = Mock.Wrap(new WrapMultipleOverloads("t", 7));
        await Assert.That(mock.Object.Tag()).IsEqualTo("t#7");
    }

    [Test]
    public async Task Optional_Param_Constructor()
    {
        var mock = Mock.Wrap(new WrapOptionalParam(9));
        await Assert.That(mock.Object.Value()).IsEqualTo(9);
    }

    [Test]
    public async Task Nullable_Params()
    {
        var mock = Mock.Wrap(new WrapNullableParams("x", 3));
        await Assert.That(mock.Object.Desc()).IsEqualTo("x/3");
    }

    [Test]
    public async Task In_Param_Constructor()
    {
        var mock = Mock.Wrap(new WrapInParam(123));
        await Assert.That(mock.Object.Get()).IsEqualTo(123);
    }

    [Test]
    public async Task Reference_Type_Param()
    {
        var mock = Mock.Wrap(new WrapRefTypeParam(new WrapDependency("dep")));
        await Assert.That(mock.Object.DepName()).IsEqualTo("dep");
    }

    [Test]
    public async Task Struct_Param()
    {
        var mock = Mock.Wrap(new WrapStructParam(new WrapPoint(2, 5)));
        await Assert.That(mock.Object.Coords()).IsEqualTo("2,5");
    }

    [Test]
    public async Task Closed_Generic_Reference_Type()
    {
        var mock = Mock.Wrap(new WrapGeneric<string>("g"));
        await Assert.That(mock.Object.GetValue()).IsEqualTo("g");
    }

    [Test]
    public async Task Closed_Generic_Value_Type()
    {
        var mock = Mock.Wrap(new WrapGeneric<int>(77));
        await Assert.That(mock.Object.GetValue()).IsEqualTo(77);
    }

    [Test]
    public void Verification_Records_Calls_When_Delegating()
    {
        var mock = Mock.Wrap(new WrapSingleValueParam(1));
        _ = mock.Object.Next();
        _ = mock.Object.Next();
        mock.Next().WasCalled(Times.Exactly(2));
    }
}
