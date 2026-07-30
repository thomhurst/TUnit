using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace TUnit.Mocks.InternalsAccess.Tasks;

/// <summary>
/// Experimental (#6514 "Tier 2"). For each requested assembly reference, produces a
/// compile-time-only copy whose internal types and members are rewritten to public, and emits a
/// source file applying <c>IgnoresAccessChecksToAttribute</c> so the runtime skips accessibility
/// checks from the test assembly to those assemblies.
///
/// The publicized copy keeps the original assembly identity (name, version, public key), so IL
/// compiled against it binds to the ORIGINAL assembly at runtime — the rewritten copy never
/// ships and is never loaded. Compiler sees public; runtime sees the real thing and is told not
/// to check. This is the established "publicizer" pattern (Krafs.Publicizer,
/// IgnoresAccessChecksToGenerator), applied here so the TUnit.Mocks source generator can treat
/// another assembly's internal interfaces as first-class mockable types.
/// </summary>
public sealed class PublicizeAssemblyReferences : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// The compiler's resolved references (@(ReferencePathWithRefAssemblies)) — each ItemSpec is
    /// exactly the file the compiler would consume, reference assemblies included.
    /// </summary>
    [Required]
    public ITaskItem[] ReferencePaths { get; set; } = [];

    /// <summary>Simple assembly names to publicize (@(TUnitMocksInternalsAccess)).</summary>
    [Required]
    public ITaskItem[] AssembliesToPublicize { get; set; } = [];

    /// <summary>
    /// Runtime/copy-local assets (implementation assemblies), used to locate the implementation
    /// when the compiler reference is itself a metadata-only reference assembly with no
    /// %(OriginalPath) — e.g. a package whose compile asset comes straight from ref/&lt;tfm&gt;.
    /// </summary>
    public ITaskItem[] RuntimeAssemblies { get; set; } = [];

    /// <summary>Directory for the rewritten compile-time copies.</summary>
    [Required]
    public string OutputDirectory { get; set; } = "";

    /// <summary>Path of the generated IgnoresAccessChecksTo source file.</summary>
    [Required]
    public string GeneratedSourceFile { get; set; } = "";

    /// <summary>
    /// Whether the generated source also defines IgnoresAccessChecksToAttribute. Turn off when
    /// another package (IgnoresAccessChecksToGenerator, Fody, ...) already injects the same type
    /// into the compilation, which would otherwise be a duplicate-type compile error.
    /// </summary>
    public bool EmitAttributeDefinition { get; set; } = true;

    /// <summary>
    /// Publicized references. ItemSpec = rewritten copy; %(Original) = the reference item it
    /// replaces (the selected match).
    /// </summary>
    [Output]
    public ITaskItem[] PublicizedReferences { get; set; } = [];

    /// <summary>
    /// Every resolved reference superseded by a publicized copy — the selected match plus any
    /// same-simple-name duplicates. The targets file Removes all of these so an ambiguous match
    /// degrades to "first wins, cleanly" instead of the compiler seeing the same assembly
    /// identity twice (CS1703).
    /// </summary>
    [Output]
    public ITaskItem[] SupersededReferences { get; set; } = [];

    public override bool Execute()
    {
        Directory.CreateDirectory(OutputDirectory);

        var outputs = new List<ITaskItem>();
        var superseded = new List<ITaskItem>();
        var publicizedNames = new List<string>();

        // The same simple name requested twice (duplicate items, multi-import) must not produce
        // two publicized items — the targets would hand Csc the same rewritten assembly twice.
        var requestedNames = AssembliesToPublicize
            .Select(i => i.ItemSpec)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var name in requestedNames)
        {
            var matches = FindReferenceMatches(name);

            if (matches.Count == 0)
            {
                Log.LogError(
                    subcategory: null, errorCode: "TUMIA001", helpKeyword: null,
                    file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                    message: $"TUnitMocksInternalsAccess: no resolved reference named '{name}' was found. " +
                             "The value must be the simple assembly name of a direct or transitive reference.");
                continue;
            }

            // EVERY distinct assembly identity behind the requested name is publicized — a
            // same-file-name pair with different identities (version or public key, legal only
            // under extern aliases) gets one publicized copy each, since the simple-name request
            // has no way to single one out. Same-identity duplicates collapse onto one copy.
            var (identityGroups, unreadableMatches) = GroupMatchesByIdentity(matches);

            if (matches.Select(m => m.ItemSpec).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            {
                var unreadableNote = unreadableMatches.Count > 0
                    ? " References whose assembly identity cannot be read are left in place: " +
                      $"{string.Join(", ", unreadableMatches.Select(m => m.ItemSpec))}."
                    : "";
                Log.LogWarning(
                    subcategory: null, warningCode: "TUMIA004", helpKeyword: null,
                    file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                    message: $"TUnitMocksInternalsAccess: multiple resolved references match '{name}': " +
                             $"{string.Join(", ", matches.Select(m => m.ItemSpec))}. Each distinct " +
                             "assembly identity is publicized separately; same-identity duplicates " +
                             "collapse onto one copy (extern aliases and EmbedInteropTypes " +
                             $"preserved).{unreadableNote}");
            }

            foreach (var identityGroup in identityGroups)
            {
                PublicizeGroup(name, identityGroup, outputs, superseded, publicizedNames);
            }
        }

        if (!Log.HasLoggedErrors)
        {
            WriteIgnoresAccessChecksToSource(publicizedNames);
        }

        PublicizedReferences = outputs.ToArray();
        SupersededReferences = superseded.ToArray();
        return !Log.HasLoggedErrors;
    }

    /// <summary>
    /// Version stamp for the incrementality signature — a new TUnit.Mocks release re-publicizes
    /// even when the source assembly is unchanged.
    /// </summary>
    private static readonly string TaskVersion = GetTaskVersion();

    private static string GetTaskVersion()
    {
        var assembly = typeof(PublicizeAssemblyReferences).Assembly;
        var informational = (System.Reflection.AssemblyInformationalVersionAttribute?)Attribute.GetCustomAttribute(
            assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute));
        return informational?.InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "0";
    }

    /// <summary>
    /// Resolves the requested simple name against the compiler's references: by file name first
    /// (the common case), then by each reference's REAL assembly identity — a compile asset can
    /// be renamed relative to the assembly it contains (e.g. VendorAlias.dll holding identity
    /// Vendor.Core), and the documented contract is the simple ASSEMBLY name, not the file name.
    /// </summary>
    private List<ITaskItem> FindReferenceMatches(string name)
    {
        var byFileName = ReferencePaths.Where(r =>
            string.Equals(Path.GetFileNameWithoutExtension(r.ItemSpec), name, StringComparison.OrdinalIgnoreCase)).ToList();

        if (byFileName.Count > 0)
        {
            return byFileName;
        }

        return ReferencePaths.Where(r =>
            string.Equals(TryGetAssemblyIdentity(r.ItemSpec)?.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Publicizes one identity group: the group's first member is the source, every member is
    /// superseded (a leftover same-identity duplicate would collide with the publicized copy —
    /// CS1703), and compiler-significant metadata from all members survives on the replacement.
    /// </summary>
    private void PublicizeGroup(
        string name, List<ITaskItem> identityGroup,
        List<ITaskItem> outputs, List<ITaskItem> superseded, List<string> publicizedNames)
    {
        var reference = identityGroup[0];
        string source;
        string destination;
        string assemblyName;

        try
        {
            source = ResolveImplementationAssembly(name, reference);
            // The runtime matches IgnoresAccessChecksTo against the real assembly identity,
            // which can differ from the requested (file-derived) simple name.
            assemblyName = System.Reflection.AssemblyName.GetAssemblyName(source).Name!;
            // Each source gets its own subdirectory keyed by its path: two implementation
            // assemblies with the same file name in different directories (renamed assets, or
            // two identities behind one simple name) must not overwrite each other's copy.
            var destinationDirectory = Path.Combine(OutputDirectory, HashPathToken(source));
            Directory.CreateDirectory(destinationDirectory);
            destination = Path.Combine(destinationDirectory, Path.GetFileName(source));

            // Content-based incrementality: the signature records the resolved source path
            // and a hash of its bytes, so a replaced/downgraded assembly with an equal or
            // older timestamp still invalidates the publicized copy.
            // The task version is part of the signature: a TUnit.Mocks upgrade that changes
            // the rewrite rules must invalidate copies produced by the previous task, or the
            // compiler keeps seeing the stale publicized API until obj is cleaned.
            var signaturePath = destination + ".sig";
            var signature = TaskVersion + "\n" + source + "\n" + HashFile(source);

            if (!File.Exists(destination) || !File.Exists(signaturePath) || File.ReadAllText(signaturePath) != signature)
            {
                Publicize(source, destination);
                File.WriteAllText(signaturePath, signature);
                Log.LogMessage(MessageImportance.Normal, $"TUnitMocksInternalsAccess: publicized '{source}' -> '{destination}'.");
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, $"TUnitMocksInternalsAccess: '{destination}' is up to date.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError(
                subcategory: null, errorCode: "TUMIA005", helpKeyword: null,
                file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                message: $"TUnitMocksInternalsAccess: failed to publicize '{reference.ItemSpec}': {ex.Message}");
            Log.LogMessage(MessageImportance.Low, ex.ToString());
            return;
        }

        var item = new TaskItem(destination);
        // Preserve the original reference's metadata (Aliases, EmbedInteropTypes, ...) —
        // Csc reads compiler-significant options from item metadata, and an extern-aliased
        // reference must stay extern-aliased after the swap.
        reference.CopyMetadataTo(item);
        if (identityGroup.Count > 1)
        {
            MergeCompilerMetadataFromDuplicates(item, identityGroup);
        }
        // "Original" is what the targets file Removes — the reference item as the compiler
        // knew it (the ref assembly when one existed), not the implementation path.
        item.SetMetadata("Original", reference.ItemSpec);
        // Compile-time only: never copy the rewritten assembly to the output directory.
        item.SetMetadata("Private", "false");
        item.SetMetadata("CopyLocal", "false");
        outputs.Add(item);
        // Two identities behind one simple name produce one attribute application — the runtime
        // matches IgnoresAccessChecksTo by simple name only.
        if (!publicizedNames.Contains(assemblyName, StringComparer.OrdinalIgnoreCase))
        {
            publicizedNames.Add(assemblyName);
        }

        superseded.AddRange(identityGroup.Select(ITaskItem (m) => new TaskItem(m.ItemSpec)));
    }

    /// <summary>
    /// Groups the matches by exact assembly identity (name, version, public key), preserving
    /// order — each group is publicized once from its first member. A match whose identity
    /// cannot be read is never assumed to duplicate anything: the first match still anchors a
    /// group (so a single unreadable reference fails loudly in Publicize), later unreadable
    /// matches are left in the compiler's reference list untouched.
    /// </summary>
    private (List<List<ITaskItem>> Groups, List<ITaskItem> Unreadable) GroupMatchesByIdentity(List<ITaskItem> matches)
    {
        var groups = new List<List<ITaskItem>> { new() { matches[0] } };
        var groupIdentities = new List<System.Reflection.AssemblyName?> { TryGetAssemblyIdentity(matches[0].ItemSpec) };
        var unreadable = new List<ITaskItem>();

        foreach (var match in matches.Skip(1))
        {
            var identity = TryGetAssemblyIdentity(match.ItemSpec);
            if (identity is null)
            {
                unreadable.Add(match);
                continue;
            }

            var groupIndex = groupIdentities.FindIndex(g => g is not null && HasSameIdentity(g, identity));
            if (groupIndex >= 0)
            {
                groups[groupIndex].Add(match);
            }
            else
            {
                groups.Add([match]);
                groupIdentities.Add(identity);
            }
        }

        return (groups, unreadable);
    }

    private static bool HasSameIdentity(System.Reflection.AssemblyName left, System.Reflection.AssemblyName right)
        => string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
           && Equals(left.Version, right.Version)
           && (left.GetPublicKeyToken() ?? []).SequenceEqual(right.GetPublicKeyToken() ?? []);

    private static System.Reflection.AssemblyName? TryGetAssemblyIdentity(string path)
    {
        try
        {
            return System.Reflection.AssemblyName.GetAssemblyName(path);
        }
        catch (Exception)
        {
            // Native or otherwise unreadable dll.
            return null;
        }
    }

    /// <summary>
    /// Every superseded (same-identity) match is removed from the compiler's reference list, so
    /// compiler-significant metadata carried only by a non-selected match — an extern alias, an
    /// EmbedInteropTypes flag — must survive on the single replacement item: aliases are
    /// unioned, and interop embedding is kept if any match asked for it.
    /// </summary>
    private static void MergeCompilerMetadataFromDuplicates(TaskItem item, List<ITaskItem> matches)
    {
        var aliases = new List<string>();
        var anyGlobal = false;

        foreach (var match in matches)
        {
            var value = match.GetMetadata("Aliases");
            if (string.IsNullOrWhiteSpace(value))
            {
                // No aliases = visible through the global namespace.
                anyGlobal = true;
                continue;
            }

            foreach (var raw in value.Split(','))
            {
                var alias = raw.Trim();
                if (alias.Length == 0)
                {
                    continue;
                }

                if (alias == "global")
                {
                    anyGlobal = true;
                }
                else if (!aliases.Contains(alias, StringComparer.Ordinal))
                {
                    aliases.Add(alias);
                }
            }
        }

        if (aliases.Count > 0)
        {
            if (anyGlobal)
            {
                aliases.Insert(0, "global");
            }

            item.SetMetadata("Aliases", string.Join(",", aliases));
        }

        if (matches.Any(m => string.Equals(m.GetMetadata("EmbedInteropTypes"), "true", StringComparison.OrdinalIgnoreCase)))
        {
            item.SetMetadata("EmbedInteropTypes", "true");
        }
    }

    /// <summary>
    /// Picks the implementation assembly to publicize. Roslyn reference assemblies strip
    /// internal members when the assembly grants no InternalsVisibleTo (internal types survive
    /// as empty shells — e.g. an internal constructor would be gone), so a ref assembly is
    /// useless as a publicizer source. ReferencePathWithRefAssemblies items carry the
    /// implementation path as %(OriginalPath) when a ref assembly was substituted; when a
    /// package supplies its compile asset from ref/&lt;tfm&gt; directly there is no OriginalPath, so
    /// fall back to the runtime/copy-local assets to find the implementation.
    /// </summary>
    private string ResolveImplementationAssembly(string name, ITaskItem reference)
    {
        var originalPath = reference.GetMetadata("OriginalPath");
        if (!string.IsNullOrEmpty(originalPath))
        {
            return originalPath;
        }

        if (!IsReferenceAssembly(reference.ItemSpec))
        {
            return reference.ItemSpec;
        }

        var runtimeMatch = FindRuntimeImplementation(name, reference.ItemSpec);

        if (runtimeMatch is not null)
        {
            Log.LogMessage(MessageImportance.Normal,
                $"TUnitMocksInternalsAccess: '{reference.ItemSpec}' is a reference assembly; " +
                $"publicizing the implementation '{runtimeMatch}' instead.");
            return runtimeMatch;
        }

        Log.LogWarning(
            subcategory: null, warningCode: "TUMIA003", helpKeyword: null,
            file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
            message: $"TUnitMocksInternalsAccess: '{reference.ItemSpec}' is a metadata-only reference assembly " +
                     "and no implementation assembly was found among the runtime assets. Internal members may " +
                     "already be stripped from it, in which case internals access will be incomplete for " +
                     $"'{name}'.");
        return reference.ItemSpec;
    }

    /// <summary>
    /// Finds the implementation assembly among the runtime/copy-local assets by the reference's
    /// real assembly identity — a version-exact match wins over a name-only one. Identity, not
    /// file name, decides: two same-file-name references with distinct identities (extern-alias
    /// pairs) each search this list, and a filename-first shortcut would hand both the same
    /// implementation. The requested-name filename match survives only as a fallback for a
    /// reference whose own identity cannot be read.
    /// </summary>
    private string? FindRuntimeImplementation(string name, string referencePath)
    {
        var candidates = RuntimeAssemblies
            .Select(r => r.ItemSpec)
            .Where(p =>
                string.Equals(Path.GetExtension(p), ".dll", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(p) &&
                // Never hand back the reference assembly itself if it also appears as an asset.
                !string.Equals(Path.GetFullPath(p), Path.GetFullPath(referencePath), StringComparison.OrdinalIgnoreCase))
            .ToList();

        var identity = TryGetAssemblyIdentity(referencePath);
        if (identity is null)
        {
            return candidates.FirstOrDefault(p =>
                string.Equals(Path.GetFileNameWithoutExtension(p), name, StringComparison.OrdinalIgnoreCase));
        }

        string? nameOnlyMatch = null;
        foreach (var candidate in candidates)
        {
            var candidateIdentity = TryGetAssemblyIdentity(candidate);
            if (candidateIdentity is null)
            {
                // Native or otherwise unreadable dll among the copy-local assets.
                continue;
            }

            if (!string.Equals(candidateIdentity.Name, identity.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Equals(candidateIdentity.Version, identity.Version))
            {
                return candidate;
            }

            nameOnlyMatch ??= candidate;
        }

        return nameOnlyMatch;
    }

    private static bool IsReferenceAssembly(string path)
    {
        using var module = ModuleDefinition.ReadModule(path);
        return module.Assembly.HasCustomAttributes && module.Assembly.CustomAttributes.Any(a =>
            a.AttributeType.FullName == "System.Runtime.CompilerServices.ReferenceAssemblyAttribute");
    }

    /// <summary>
    /// Stable per-source-path directory token — derived from the path (not content) so the same
    /// source keeps the same output location across builds and incrementality still works.
    /// Case is folded only where paths are case-insensitive: on a case-sensitive host,
    /// /deps/A/Foo.dll and /deps/a/Foo.dll are DIFFERENT sources and folding would collapse
    /// their tokens (and, with equal file names, their destinations).
    /// </summary>
    private static string HashPathToken(string sourcePath)
    {
        var fullPath = Path.GetFullPath(sourcePath);
#if NET
        if (OperatingSystem.IsWindows())
        {
            fullPath = fullPath.ToUpperInvariant();
        }
#else
        // The net472 MSBuild host only runs on Windows.
        fullPath = fullPath.ToUpperInvariant();
#endif
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fullPath));
#if NET
        return Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
#else
        return BitConverter.ToString(hash, 0, 4).Replace("-", "").ToLowerInvariant();
#endif
    }

    private static string HashFile(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
#if NET
        return Convert.ToHexString(sha.ComputeHash(stream));
#else
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
#endif
    }

    private static void Publicize(string source, string destination)
    {
        using var module = ModuleDefinition.ReadModule(source);

        foreach (var type in module.GetTypes())
        {
            if (type.Name == "<Module>")
            {
                continue;
            }

            // Only assembly-level visibility is promoted. private/protected members stay as
            // they are: the feature promises INTERNALS access, and promoting private members
            // would inject them into overload resolution for consuming code (a formerly-private
            // M(string) beside a public M(object) silently rebinding M(null)).
            if (IsAssemblyVisibleType(type))
            {
                type.Attributes = type.IsNested
                    ? (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic
                    : (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.Public;
            }

            foreach (var method in type.Methods)
            {
                if (IsAssemblyVisibleMethod(method))
                {
                    method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
                }
            }
        }

        // Identity (name/version/public key) is preserved so compiled IL binds to the original
        // assembly at runtime; only the signature is invalidated, which nothing validates for a
        // compile-time reference. Clear the signed flag so nothing is tempted to try.
        module.Attributes &= ~ModuleAttributes.StrongNameSigned;
        module.Write(destination);
    }

    /// <summary>internal, protected internal, or private protected — never plain private.</summary>
    private static bool IsAssemblyVisibleType(TypeDefinition type)
        => type.IsNested
            ? type.IsNestedAssembly || type.IsNestedFamilyOrAssembly || type.IsNestedFamilyAndAssembly
            : type.IsNotPublic;

    private static bool IsAssemblyVisibleMethod(MethodDefinition method)
        => method.IsAssembly || method.IsFamilyOrAssembly || method.IsFamilyAndAssembly;

    private void WriteIgnoresAccessChecksToSource(List<string> assemblyNames)
    {
        var writer = new StringWriter();
        writer.WriteLine("// <auto-generated>");
        writer.WriteLine("// Generated by TUnit.Mocks internals access (experimental). The runtime honors");
        writer.WriteLine("// IgnoresAccessChecksToAttribute and skips accessibility checks from this assembly");
        writer.WriteLine("// to the assemblies named below, matching the publicized compile-time references.");
        writer.WriteLine("// </auto-generated>");
        foreach (var name in assemblyNames)
        {
            writer.WriteLine($"[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"{name}\")]");
        }

        if (EmitAttributeDefinition)
        {
            writer.WriteLine();
            writer.WriteLine("namespace System.Runtime.CompilerServices");
            writer.WriteLine("{");
            writer.WriteLine("    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]");
            writer.WriteLine("    internal sealed class IgnoresAccessChecksToAttribute : Attribute");
            writer.WriteLine("    {");
            writer.WriteLine("        public IgnoresAccessChecksToAttribute(string assemblyName) => AssemblyName = assemblyName;");
            writer.WriteLine();
            writer.WriteLine("        public string AssemblyName { get; }");
            writer.WriteLine("    }");
            writer.WriteLine("}");
        }

        var content = writer.ToString();
        Directory.CreateDirectory(Path.GetDirectoryName(GeneratedSourceFile)!);
        if (!File.Exists(GeneratedSourceFile) || File.ReadAllText(GeneratedSourceFile) != content)
        {
            File.WriteAllText(GeneratedSourceFile, content);
        }
    }
}
