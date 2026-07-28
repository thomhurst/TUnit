using TUnit.Mocks.SourceGenerator.Builders;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Discovery;

/// <summary>
/// Flags mocked types whose generated names collide.
/// <para>
/// Generated type and <c>AddSource</c> hint names come from the mocked type's fully qualified name
/// with separators mapped to <c>_</c>. <see cref="IdentifierEscaping.SanitizeIdentifier"/> doubles
/// literal underscores so the realistic cases stay distinct, but no mapping onto
/// <c>[A-Za-z0-9_]</c> can be injective while a separator and an underscore both render as runs of
/// <c>_</c>: a run of three cannot say whether it was underscore-then-separator or the reverse, so
/// <c>A_.B.IFoo</c> and <c>A._B.IFoo</c> still meet at <c>A___B_IFoo</c>.
/// </para>
/// <para>
/// A duplicate hint name aborts the whole generator, which costs the user every mock in the
/// compilation and reports only a CS8785 warning pointing at the generator rather than at either
/// type. Skipping the colliding types and reporting TM008 keeps the rest of the compilation's
/// mocks and names both culprits. See issue #6505.
/// </para>
/// </summary>
internal static class GeneratedNameCollisionDetector
{
    /// <summary>
    /// Returns <paramref name="models"/> in input order, with <see cref="MockTypeModel.CollidesWith"/>
    /// set on every model that shares its generated name with another.
    /// </summary>
    internal static List<MockTypeModel> Annotate(IEnumerable<MockTypeModel> models)
    {
        var ordered = models.ToList();

        // The name alone is not the key: a multi-interface combo and the secondary setup surface
        // for the same (primary, interface) pair intentionally share a composite name and are told
        // apart by the hint-name suffix, so they must not be flagged.
        var groups = new Dictionary<(bool IsSecondaryMemberSurface, string Name), List<MockTypeModel>>();

        foreach (var model in ordered)
        {
            var key = (model.IsSecondaryMemberSurface, MockImplBuilder.GetCompositeSafeName(model));

            if (!groups.TryGetValue(key, out var group))
            {
                groups[key] = group = new List<MockTypeModel>();
            }

            group.Add(model);
        }

        if (groups.Count == ordered.Count)
        {
            return ordered;
        }

        var annotated = new List<MockTypeModel>(ordered.Count);

        foreach (var model in ordered)
        {
            var group = groups[(model.IsSecondaryMemberSurface, MockImplBuilder.GetCompositeSafeName(model))];

            // Same target mocked in more than one mode (Mock.Of and Mock.Wrap of one type, say)
            // reaches this point as separate models sharing an identity. Only distinct targets
            // meeting at one name are a #6505 collision.
            var others = group
                .Where(other => Identity(other) != Identity(model))
                .Select(other => other.FullyQualifiedName)
                .Distinct()
                .ToList();

            annotated.Add(others.Count == 0
                ? model
                : model with { CollidesWith = string.Join(", ", others) });
        }

        return annotated;
    }

    private static string Identity(MockTypeModel model)
        => model.AdditionalInterfaceNames.Length == 0
            ? model.FullyQualifiedName
            : model.FullyQualifiedName + "|" + string.Join("|", model.AdditionalInterfaceNames);
}
