using System.ComponentModel;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Wraps a generated mock implementation. Source-generated extension methods on <c>Mock&lt;T&gt;</c>
/// provide strongly-typed setup and verification members directly.
/// </summary>
public class Mock<T> : IMock, IMockEngineAccess<T> where T : class
{
    private readonly MockEngine<T> _engine;

    /// <summary>The mock object that implements T.</summary>
    public T Object { get; }

    /// <inheritdoc />
    object IMock.ObjectInstance => Object;

    /// <inheritdoc />
    MockEngine<T> IMockEngineAccess<T>.Engine => _engine;

    /// <summary>Creates a Mock wrapping the given object and engine. Used by generated code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Mock(T mockObject, MockEngine<T> engine)
    {
        _engine = engine;
        Object = mockObject;
        Mock.Register(mockObject, this);
    }

    /// <inheritdoc />
    void IMock.VerifyAll()
    {
        var setups = _engine.GetSetups();
        var uninvoked = new List<string>();
        foreach (var setup in setups)
        {
            if (setup.InvokeCount == 0)
            {
                var matchers = setup.GetMatcherDescriptions();
                var desc = matchers.Length > 0
                    ? $"{setup.MemberName}({string.Join(", ", matchers)})"
                    : $"{setup.MemberName}()";
                uninvoked.Add(desc);
            }
        }

        if (uninvoked.Count > 0)
        {
            var message = $"VerifyAll failed. The following setups were never invoked:\n" +
                          string.Join("\n", uninvoked.Select(s => $"  - {s}"));
            throw new MockVerificationException(message);
        }
    }

    /// <inheritdoc />
    void IMock.VerifyNoOtherCalls()
    {
        var unverified = _engine.GetUnverifiedCalls();
        if (unverified.Count > 0)
        {
            var message = $"VerifyNoOtherCalls failed. The following calls were not verified:\n" +
                          string.Join("\n", unverified.Select(c => $"  - {c.FormatCall()}"));
            throw new MockVerificationException(message);
        }
    }

    /// <inheritdoc />
    void IMock.Reset() => _engine.Reset();

    /// <summary>Implicit conversion to T -- no .Object needed.</summary>
    public static implicit operator T(Mock<T> mock) => mock.Object;
}
