using System.ComponentModel;

namespace TUnit.Mocks;

public sealed partial class MockEngine<T> where T : class
{
    // Maps each additional interface of a multi-type mock (Mock.Of<T, T2, ...>) to an array
    // translating the interface's standalone member ordinals (baked into the generated
    // secondary setup extensions, which are shared across all combos containing the pair)
    // to this impl's union member IDs. Registered once during factory construction, before
    // the mock is published — read-only afterwards, so no locking is needed.
    private Dictionary<Type, int[]>? _secondaryMemberIdMaps;

    /// <summary>
    /// Registers the member-ID translation map for one additional interface of a multi-type mock.
    /// Called by generated factories only.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterSecondaryInterface(Type interfaceType, int[] memberIdMap)
    {
        (_secondaryMemberIdMaps ??= new Dictionary<Type, int[]>())[interfaceType] = memberIdMap;
    }

    /// <summary>
    /// Resolves an additional interface's standalone member ordinal to this mock's member ID.
    /// Returns false when the mock was not created with <paramref name="interfaceType"/> as a
    /// secondary interface (or the member has no mapping). Called by generated code only.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetSecondaryMemberId(Type interfaceType, int localMemberId, out int memberId)
    {
        var maps = _secondaryMemberIdMaps;
        if (maps is not null
            && maps.TryGetValue(interfaceType, out var map)
            && (uint)localMemberId < (uint)map.Length
            && map[localMemberId] >= 0)
        {
            memberId = map[localMemberId];
            return true;
        }

        memberId = -1;
        return false;
    }

    /// <summary>
    /// True when this mock was created with <paramref name="interfaceType"/> as a secondary
    /// interface. Called by generated code only.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool HasSecondaryInterface(Type interfaceType)
        => _secondaryMemberIdMaps?.ContainsKey(interfaceType) == true;
}
