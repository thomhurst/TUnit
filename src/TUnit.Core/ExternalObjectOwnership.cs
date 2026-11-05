using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Indexes object identities without retaining either the objects or their row owner handles.
/// Ownership disappears when no row, static lease or tracker retains the handle.
/// </summary>
internal static class ExternalObjectOwnership
{
    private static readonly ConditionalWeakTable<object, OwnerSet> Owners = new();
    private static volatile bool s_hasOwners;

    internal static void Register(object instance, DataRowScopeCollection.ScopeOwner owner)
    {
        var owners = Owners.GetValue(instance, static _ => new OwnerSet());
        s_hasOwners = true;
        lock (owners)
        {
            for (var i = owners.Values.Count - 1; i >= 0; i--)
            {
                if (!owners.Values[i].TryGetTarget(out var existing))
                {
                    owners.Values.RemoveAt(i);
                }
                else if (ReferenceEquals(existing, owner))
                {
                    return;
                }
            }
            owners.Values.Add(new(owner));
        }
    }

    internal static bool IsOwned(object instance) => FindOwner(instance) is not null;

    // Any live owner suffices to suppress tracker disposal; ownership need not be unique.
    internal static DataRowScopeCollection.ScopeOwner? FindOwner(object instance)
    {
        if (!s_hasOwners || !Owners.TryGetValue(instance, out var owners))
        {
            return null;
        }
        lock (owners)
        {
            for (var i = owners.Values.Count - 1; i >= 0; i--)
            {
                if (owners.Values[i].TryGetTarget(out var owner))
                {
                    return owner;
                }
                owners.Values.RemoveAt(i);
            }
        }
        return null;
    }

    private sealed class OwnerSet
    {
        internal readonly List<WeakReference<DataRowScopeCollection.ScopeOwner>> Values = [];
    }
}
