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
    /// <summary>All resolved compile-time references (@(ReferencePath)).</summary>
    [Required]
    public ITaskItem[] ReferencePaths { get; set; } = [];

    /// <summary>Simple assembly names to publicize (@(TUnitMocksInternalsAccess)).</summary>
    [Required]
    public ITaskItem[] AssembliesToPublicize { get; set; } = [];

    /// <summary>Directory for the rewritten compile-time copies.</summary>
    [Required]
    public string OutputDirectory { get; set; } = "";

    /// <summary>Path of the generated IgnoresAccessChecksTo source file.</summary>
    [Required]
    public string GeneratedSourceFile { get; set; } = "";

    /// <summary>
    /// Publicized references. ItemSpec = rewritten copy; %(Original) = the ReferencePath item it
    /// replaces, for the targets file to Remove.
    /// </summary>
    [Output]
    public ITaskItem[] PublicizedReferences { get; set; } = [];

    public override bool Execute()
    {
        Directory.CreateDirectory(OutputDirectory);

        var outputs = new List<ITaskItem>();
        var publicizedNames = new List<string>();

        foreach (var requested in AssembliesToPublicize)
        {
            var name = requested.ItemSpec;
            var reference = ReferencePaths.FirstOrDefault(r =>
                string.Equals(Path.GetFileNameWithoutExtension(r.ItemSpec), name, StringComparison.OrdinalIgnoreCase));

            if (reference is null)
            {
                Log.LogError($"TUnitMocksInternalsAccess: no resolved reference named '{name}' was found. " +
                             "The value must be the simple assembly name of a direct or transitive reference.");
                return false;
            }

            // The compiler consumes the reference assembly when one exists (project references
            // produce one under obj/ref; packages may ship ref/ assemblies) — that is the file
            // that must be publicized.
            var referenceAssembly = reference.GetMetadata("ReferenceAssembly");
            var source = string.IsNullOrEmpty(referenceAssembly) ? reference.ItemSpec : referenceAssembly;
            var destination = Path.Combine(OutputDirectory, Path.GetFileName(source));

            if (!File.Exists(destination) || File.GetLastWriteTimeUtc(destination) < File.GetLastWriteTimeUtc(source))
            {
                Publicize(source, destination);
                Log.LogMessage(MessageImportance.Normal, $"TUnitMocksInternalsAccess: publicized '{source}' -> '{destination}'.");
            }

            var item = new TaskItem(destination);
            item.SetMetadata("Original", reference.ItemSpec);
            // Compile-time only: never copy the rewritten assembly to the output directory.
            item.SetMetadata("Private", "false");
            item.SetMetadata("CopyLocal", "false");
            outputs.Add(item);
            publicizedNames.Add(name);
        }

        WriteIgnoresAccessChecksToSource(publicizedNames);
        PublicizedReferences = outputs.ToArray();
        return !Log.HasLoggedErrors;
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

            type.Attributes = type.IsNested
                ? (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic
                : (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.Public;

            foreach (var method in type.Methods)
            {
                // Explicit interface implementations stay private — IL requires it, and making
                // them public would surface dotted names the compiler cannot bind anyway.
                if (method.Overrides.Count > 0 && method.IsPrivate)
                {
                    continue;
                }

                method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
            }
        }

        // Identity (name/version/public key) is preserved so compiled IL binds to the original
        // assembly at runtime; only the signature is invalidated, which nothing validates for a
        // compile-time reference. Clear the signed flag so nothing is tempted to try.
        module.Attributes &= ~ModuleAttributes.StrongNameSigned;
        module.Write(destination);
    }

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

        var content = writer.ToString();
        Directory.CreateDirectory(Path.GetDirectoryName(GeneratedSourceFile)!);
        if (!File.Exists(GeneratedSourceFile) || File.ReadAllText(GeneratedSourceFile) != content)
        {
            File.WriteAllText(GeneratedSourceFile, content);
        }
    }
}
