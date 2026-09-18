using System.Collections.Generic;
using System.Linq;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Discovery;

/// <summary>
/// Decides which model emits a type's shared setup/verification surface.
/// <para>
/// A type reached through more than one mocking mode — <c>Mock.Of&lt;T&gt;</c> or <c>T.Mock()</c>
/// alongside <c>Mock.Wrap(instance)</c> — produces two models that differ only in
/// <see cref="MockTypeModel.IsWrapMock"/>. Model equality includes that flag, so both survive dedup,
/// which is correct: each needs its own impl and factory. Their member surface is not per mode
/// though. It describes the mocked type, is byte-identical between the two, and lives in an
/// extension class that is not <c>file</c>-scoped, so emitting it twice is a duplicate hint name —
/// which aborts the generator and takes every mock in the compilation with it — and a duplicate
/// type on top. The regular model owns the surface, since it also emits the static <c>Mock()</c>
/// entry point; a wrap model owns it only when it is the type's only model. See issue #6834.
/// </para>
/// </summary>
internal static class SharedMemberSurfaceResolver
{
    /// <summary>
    /// Returns <paramref name="requests"/> in input order, with
    /// <see cref="MockTypeModel.EmitsSharedMemberSurface"/> cleared on wrap models whose type is
    /// also mocked regularly.
    /// </summary>
    internal static List<MockGenerationRequest> Resolve(List<MockGenerationRequest> requests)
    {
        var regularlyMocked = new HashSet<string>();

        foreach (var request in requests)
        {
            if (!request.Model.IsWrapMock && !request.Model.IsSecondaryMemberSurface)
            {
                regularlyMocked.Add(MockTypeIdentity.Of(request.Model));
            }
        }

        if (regularlyMocked.Count == 0 || !requests.Any(r => r.Model.IsWrapMock))
        {
            return requests;
        }

        var resolved = new List<MockGenerationRequest>(requests.Count);

        foreach (var request in requests)
        {
            var model = request.Model;
            resolved.Add(model.IsWrapMock && regularlyMocked.Contains(MockTypeIdentity.Of(model))
                ? request with { Model = model with { EmitsSharedMemberSurface = false } }
                : request);
        }

        return resolved;
    }
}
