using System.ComponentModel;

namespace TUnit.Mocks;

public sealed partial class MockEngine<T> where T : class
{
    // Maps each additional interface of a multi-type mock (Mock.Of<T, T2, ...>) to an array
    // translating the interface's standalone member ordinals (baked into the generated
    // secondary setup extensions, which are shared across all combos containing the pair)
    // to this impl's union member IDs. Registered once during factory construction, before
    // the mock is published — read-only afterwards, so no locking is needed.
    // A flat pair array beats a Dictionary here: at most 3 entries, probed by reference
    // equality on cached typeof() instances, and one small allocation per mock creation.
    private (Type InterfaceType, int[] Map)[]? _secondaryMemberIdMaps;

    /// <summary>
    /// Registers the member-ID translation map for one additional interface of a multi-type mock.
    /// Called by generated factories only.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterSecondaryInterface(Type interfaceType, int[] memberIdMap)
    {
        var existing = _secondaryMemberIdMaps;
        if (existing is null)
        {
            _secondaryMemberIdMaps = new[] { (interfaceType, memberIdMap) };
            return;
        }

        var expanded = new (Type, int[])[existing.Length + 1];
        existing.CopyTo(expanded, 0);
        expanded[existing.Length] = (interfaceType, memberIdMap);
        _secondaryMemberIdMaps = expanded;
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
        if (maps is not null)
        {
            foreach (var (type, map) in maps)
            {
                if (ReferenceEquals(type, interfaceType))
                {
                    if ((uint)localMemberId < (uint)map.Length && map[localMemberId] >= 0)
                    {
                        memberId = map[localMemberId];
                        return true;
                    }
                    break;
                }
            }
        }

        memberId = -1;
        return false;
    }
}
