namespace TUnit.Mocks.Arguments;

/// <summary>
/// Sentinel type returned by <see cref="Arg.AnyArgs"/>. Passed to a generated mock setup or
/// verification overload, it stands in for a complete argument list of <see cref="Arg.Any{T}"/>
/// matchers — one per parameter — so callers do not have to repeat <c>Any()</c> for each slot.
/// <para>
/// The shortcut overload is only generated when the method's name is unique on the mocked type;
/// for overloaded names it would be ambiguous. In that case use the explicit per-parameter
/// <see cref="Arg.Any{T}"/> form.
/// </para>
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class AnyArgs
{
    internal static readonly AnyArgs Instance = new();

    private AnyArgs() { }
}
