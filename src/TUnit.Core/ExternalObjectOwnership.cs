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
            // Amortize stale-entry cleanup: inspect one old handle per registration, rather
            // than scanning every live row that shares a singleton.
            if (owners.Values.First is { } oldest)
            {
                owners.Values.RemoveFirst();
                if (oldest.Value.TryGetTarget(out _))
                {
                    owners.Values.AddLast(oldest);
                }
            }
            if (!owners.Membership.TryGetValue(owner, out _))
            {
                var node = owners.Values.AddLast(new WeakReference<DataRowScopeCollection.ScopeOwner>(owner));
                owners.Membership.Add(owner, node);
            }
        }
    }

    internal static void Unregister(object instance, DataRowScopeCollection.ScopeOwner owner)
    {
        if (!Owners.TryGetValue(instance, out var owners))
        {
            return;
        }
        lock (owners)
        {
            if (owners.Membership.TryGetValue(owner, out var node))
            {
                owners.Membership.Remove(owner);
                owners.Values.Remove(node);
            }
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
            while (owners.Values.Last is { } node)
            {
                if (node.Value.TryGetTarget(out var owner))
                {
                    return owner;
                }
                owners.Values.RemoveLast();
            }
        }
        return null;
    }

    // Refresh callers need every live owner, rather than any owner that suppresses disposal.
    // Return strong handles before invoking discovery: user getters must not run under this lock.
    internal static IReadOnlyList<DataRowScopeCollection.ScopeOwner> FindOwners(object instance)
    {
        if (!s_hasOwners || !Owners.TryGetValue(instance, out var owners))
        {
            return [];
        }
        lock (owners)
        {
            var snapshot = new List<DataRowScopeCollection.ScopeOwner>();
            var node = owners.Values.First;
            while (node is not null)
            {
                var next = node.Next;
                if (node.Value.TryGetTarget(out var owner))
                {
                    snapshot.Add(owner);
                }
                else
                {
                    owners.Values.Remove(node);
                }
                node = next;
            }
            return snapshot;
        }
    }

    private sealed class OwnerSet
    {
        // Membership uses weak identity keys, so adding another owner never scans existing rows.
        // The linked list permits lookup and constant-time revocation without retaining handles.
        internal readonly ConditionalWeakTable<DataRowScopeCollection.ScopeOwner,
            LinkedListNode<WeakReference<DataRowScopeCollection.ScopeOwner>>> Membership = new();
        internal readonly LinkedList<WeakReference<DataRowScopeCollection.ScopeOwner>> Values = new();
    }
}
