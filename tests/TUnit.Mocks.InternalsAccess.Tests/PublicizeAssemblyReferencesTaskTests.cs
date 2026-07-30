using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using TUnit.Mocks.InternalsAccess.Tasks;
using Task = System.Threading.Tasks.Task;

namespace TUnit.Mocks.InternalsAccess.Tests;

// Unit tests for the publicizer task itself: rewriting, incrementality, the generated
// IgnoresAccessChecksTo source, and error behavior. The end-to-end pipeline (targets wiring,
// compiler swap, runtime behavior) is covered by InternalsAccessTests.

public class PublicizeAssemblyReferencesTaskTests
{
    private const string TargetLibName = "TUnit.Mocks.InternalsAccess.TargetLib";

    private static string TargetLibPath =>
        Path.Combine(AppContext.BaseDirectory, TargetLibName + ".dll");

    private static PublicizeAssemblyReferences CreateTask(string outputDirectory, params string[] names)
        => new()
        {
            BuildEngine = new StubBuildEngine(),
            ReferencePaths = [new TaskItem(TargetLibPath), new TaskItem(Path.Combine(AppContext.BaseDirectory, "TUnit.Mocks.dll"))],
            AssembliesToPublicize = names.Select(ITaskItem (n) => new TaskItem(n)).ToArray(),
            OutputDirectory = outputDirectory,
            GeneratedSourceFile = Path.Combine(outputDirectory, "iact.g.cs"),
        };

    private static string NewScratchDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "tunit-mocks-ia-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    [Test]
    public async Task Publicizes_Internal_Types_And_Preserves_Identity()
    {
        var dir = NewScratchDirectory();
        var task = CreateTask(dir, TargetLibName);

        await Assert.That(task.Execute()).IsTrue();

        var rewritten = task.PublicizedReferences[0].ItemSpec;
        await Assert.That(File.Exists(rewritten)).IsTrue();

        var context = new AssemblyLoadContext("publicized-probe", isCollectible: true);
        try
        {
            var assembly = context.LoadFromAssemblyPath(rewritten);

            var internalInterface = assembly.GetType("FakeSdk.IInternalBindingsFeature", throwOnError: true)!;
            await Assert.That(internalInterface.IsPublic).IsTrue();

            var internalClass = assembly.GetType("FakeSdk.InternalWidget", throwOnError: true)!;
            await Assert.That(internalClass.IsPublic).IsTrue();
            var constructor = internalClass.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            await Assert.That(constructor.Length).IsEqualTo(1);

            // Identity must match the original, strong-name public key included, so compiled IL
            // binds to the real assembly at runtime.
            var original = AssemblyName.GetAssemblyName(TargetLibPath);
            var publicized = assembly.GetName();
            await Assert.That(publicized.FullName).IsEqualTo(original.FullName);
            await Assert.That(publicized.GetPublicKeyToken()).IsEquivalentTo(original.GetPublicKeyToken()!);
        }
        finally
        {
            context.Unload();
        }
    }

    [Test]
    public async Task Generates_IgnoresAccessChecksTo_Source_For_All_Requested_Assemblies()
    {
        var dir = NewScratchDirectory();
        var task = CreateTask(dir, TargetLibName, "TUnit.Mocks");

        await Assert.That(task.Execute()).IsTrue();

        var source = await File.ReadAllTextAsync(task.GeneratedSourceFile);
        await Assert.That(source).Contains($"IgnoresAccessChecksTo(\"{TargetLibName}\")");
        await Assert.That(source).Contains("IgnoresAccessChecksTo(\"TUnit.Mocks\")");
        await Assert.That(source).Contains("class IgnoresAccessChecksToAttribute");

        await Assert.That(task.PublicizedReferences.Length).IsEqualTo(2);
        await Assert.That(task.PublicizedReferences[0].GetMetadata("Original")).IsEqualTo(TargetLibPath);
        await Assert.That(task.PublicizedReferences[0].GetMetadata("Private")).IsEqualTo("false");
    }

    [Test]
    public async Task Second_Run_Is_Incremental()
    {
        var dir = NewScratchDirectory();

        var first = CreateTask(dir, TargetLibName);
        await Assert.That(first.Execute()).IsTrue();

        var rewritten = first.PublicizedReferences[0].ItemSpec;
        var firstWrite = File.GetLastWriteTimeUtc(rewritten);
        var firstSourceWrite = File.GetLastWriteTimeUtc(first.GeneratedSourceFile);

        var second = CreateTask(dir, TargetLibName);
        await Assert.That(second.Execute()).IsTrue();

        await Assert.That(File.GetLastWriteTimeUtc(rewritten)).IsEqualTo(firstWrite);
        await Assert.That(File.GetLastWriteTimeUtc(second.GeneratedSourceFile)).IsEqualTo(firstSourceWrite);
    }

    [Test]
    public async Task Content_Change_Invalidates_Publicized_Copy_Despite_Older_Timestamp()
    {
        var dir = NewScratchDirectory();

        // Use a private copy of the source so its content/timestamp can be manipulated.
        var sourceDir = NewScratchDirectory();
        var source = Path.Combine(sourceDir, TargetLibName + ".dll");
        File.Copy(TargetLibPath, source);

        var first = CreateTask(dir, TargetLibName);
        first.ReferencePaths = [new TaskItem(source)];
        await Assert.That(first.Execute()).IsTrue();

        var rewritten = first.PublicizedReferences[0].ItemSpec;
        var firstWrite = File.GetLastWriteTimeUtc(rewritten);

        // Different content, timestamp pushed BEFORE the publicized copy — a timestamp-only
        // check would treat the output as up to date.
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TUnit.Mocks.dll"), source, overwrite: true);
        File.SetLastWriteTimeUtc(source, firstWrite.AddMinutes(-5));

        var second = CreateTask(dir, TargetLibName);
        second.ReferencePaths = [new TaskItem(source)];
        await Assert.That(second.Execute()).IsTrue();

        await Assert.That(File.GetLastWriteTimeUtc(rewritten)).IsNotEqualTo(firstWrite);
    }

    [Test]
    public async Task Replacement_Reference_Preserves_Compiler_Metadata()
    {
        var dir = NewScratchDirectory();
        var reference = new TaskItem(TargetLibPath);
        reference.SetMetadata("Aliases", "sdkalias");
        reference.SetMetadata("EmbedInteropTypes", "false");

        var task = CreateTask(dir, TargetLibName);
        task.ReferencePaths = [reference];

        await Assert.That(task.Execute()).IsTrue();

        var publicized = task.PublicizedReferences[0];
        await Assert.That(publicized.GetMetadata("Aliases")).IsEqualTo("sdkalias");
        await Assert.That(publicized.GetMetadata("EmbedInteropTypes")).IsEqualTo("false");
        // The overrides still win over copied metadata.
        await Assert.That(publicized.GetMetadata("Private")).IsEqualTo("false");
        await Assert.That(publicized.GetMetadata("CopyLocal")).IsEqualTo("false");
    }

    [Test]
    public async Task Attribute_Definition_Can_Be_Suppressed()
    {
        var dir = NewScratchDirectory();
        var task = CreateTask(dir, TargetLibName);
        task.EmitAttributeDefinition = false;

        await Assert.That(task.Execute()).IsTrue();

        var source = await File.ReadAllTextAsync(task.GeneratedSourceFile);
        await Assert.That(source).Contains($"IgnoresAccessChecksTo(\"{TargetLibName}\")");
        await Assert.That(source).DoesNotContain("class IgnoresAccessChecksToAttribute");
    }

    [Test]
    public async Task Ambiguous_Simple_Name_Warns_With_TUMIA004()
    {
        var dir = NewScratchDirectory();
        var duplicateDir = NewScratchDirectory();
        var duplicate = Path.Combine(duplicateDir, TargetLibName + ".dll");
        File.Copy(TargetLibPath, duplicate);

        var engine = new StubBuildEngine();
        var task = CreateTask(dir, TargetLibName);
        task.BuildEngine = engine;
        task.ReferencePaths = [new TaskItem(TargetLibPath), new TaskItem(duplicate)];

        await Assert.That(task.Execute()).IsTrue();
        await Assert.That(engine.Warnings.Count).IsEqualTo(1);
        await Assert.That(engine.Warnings[0].Code).IsEqualTo("TUMIA004");
        // Deterministic: first match wins.
        await Assert.That(task.PublicizedReferences[0].GetMetadata("Original")).IsEqualTo(TargetLibPath);
        // ALL matches are superseded — a leftover duplicate would carry the same assembly
        // identity as the publicized copy and break the compile with CS1703.
        var supersededPaths = task.SupersededReferences.Select(s => s.ItemSpec).ToList();
        await Assert.That(supersededPaths).Contains(TargetLibPath);
        await Assert.That(supersededPaths).Contains(duplicate);
    }

    [Test]
    public async Task Duplicate_Requests_Produce_Single_Publicized_Reference()
    {
        var dir = NewScratchDirectory();
        // Same simple name requested twice (case difference included) — e.g. duplicated items
        // from a multi-imported props file. The compiler must see exactly one publicized copy.
        var task = CreateTask(dir, TargetLibName, TargetLibName.ToUpperInvariant());

        await Assert.That(task.Execute()).IsTrue();

        await Assert.That(task.PublicizedReferences.Length).IsEqualTo(1);
        var source = await File.ReadAllTextAsync(task.GeneratedSourceFile);
        var applications = source.Split([$"IgnoresAccessChecksTo(\"{TargetLibName}\")"], StringSplitOptions.None).Length - 1;
        await Assert.That(applications).IsEqualTo(1);
    }

    [Test]
    public async Task Ambiguous_Matches_Union_Compiler_Metadata()
    {
        var dir = NewScratchDirectory();
        var duplicateDir = NewScratchDirectory();
        var duplicatePath = Path.Combine(duplicateDir, TargetLibName + ".dll");
        File.Copy(TargetLibPath, duplicatePath);

        // The alias and interop flag live ONLY on the non-selected match. Both matches leave
        // the compiler's reference list, so the replacement item must carry them or an existing
        // `extern alias sdkalias` in the consuming project stops resolving.
        var winner = new TaskItem(TargetLibPath);
        var duplicate = new TaskItem(duplicatePath);
        duplicate.SetMetadata("Aliases", "sdkalias");
        duplicate.SetMetadata("EmbedInteropTypes", "true");

        var task = CreateTask(dir, TargetLibName);
        task.ReferencePaths = [winner, duplicate];

        await Assert.That(task.Execute()).IsTrue();

        var publicized = task.PublicizedReferences[0];
        // The winner had no aliases (global visibility) — that must survive the union too.
        await Assert.That(publicized.GetMetadata("Aliases")).IsEqualTo("global,sdkalias");
        await Assert.That(publicized.GetMetadata("EmbedInteropTypes")).IsEqualTo("true");
    }

    [Test]
    public async Task Different_Identity_Same_Name_References_Are_Each_Publicized()
    {
        var dir = NewScratchDirectory();
        var otherVersionDir = NewScratchDirectory();
        var otherVersionPath = Path.Combine(otherVersionDir, TargetLibName + ".dll");
        CreateDifferentVersionCopy(TargetLibPath, otherVersionPath);

        // A same-file-name pair with DIFFERENT identities is legal only under extern aliases,
        // and the simple-name request cannot single one out — so each identity gets its own
        // publicized copy, with per-identity metadata (the alias) preserved on its own item.
        var winner = new TaskItem(TargetLibPath);
        var otherVersion = new TaskItem(otherVersionPath);
        otherVersion.SetMetadata("Aliases", "oldsdk");

        var engine = new StubBuildEngine();
        var task = CreateTask(dir, TargetLibName);
        task.BuildEngine = engine;
        task.ReferencePaths = [winner, otherVersion];

        await Assert.That(task.Execute()).IsTrue();
        await Assert.That(engine.Warnings.Count).IsEqualTo(1);
        await Assert.That(engine.Warnings[0].Code).IsEqualTo("TUMIA004");

        // Both identities are superseded — each is replaced by its own publicized copy.
        var supersededPaths = task.SupersededReferences.Select(s => s.ItemSpec).ToList();
        await Assert.That(supersededPaths).Contains(TargetLibPath);
        await Assert.That(supersededPaths).Contains(otherVersionPath);

        await Assert.That(task.PublicizedReferences.Length).IsEqualTo(2);
        await Assert.That(task.PublicizedReferences[0].GetMetadata("Original")).IsEqualTo(TargetLibPath);
        await Assert.That(task.PublicizedReferences[1].GetMetadata("Original")).IsEqualTo(otherVersionPath);
        // Aliases stay with their own identity — never merged across identities.
        await Assert.That(task.PublicizedReferences[0].GetMetadata("Aliases")).IsEqualTo("");
        await Assert.That(task.PublicizedReferences[1].GetMetadata("Aliases")).IsEqualTo("oldsdk");

        // Same file name from different directories — the copies must not overwrite each other,
        // and each must keep its own identity.
        var copyPaths = task.PublicizedReferences.Select(p => p.ItemSpec).ToList();
        await Assert.That(copyPaths[0]).IsNotEqualTo(copyPaths[1]);
        await Assert.That(AssemblyName.GetAssemblyName(copyPaths[0]).Version)
            .IsEqualTo(AssemblyName.GetAssemblyName(TargetLibPath).Version);
        await Assert.That(AssemblyName.GetAssemblyName(copyPaths[1]).Version).IsEqualTo(new Version(99, 0, 0, 0));

        // One simple name -> one IgnoresAccessChecksTo application, even with two identities.
        var source = await File.ReadAllTextAsync(task.GeneratedSourceFile);
        var applications = source.Split([$"IgnoresAccessChecksTo(\"{TargetLibName}\")"], StringSplitOptions.None).Length - 1;
        await Assert.That(applications).IsEqualTo(1);
    }

    [Test]
    public async Task Same_File_Name_Sources_From_Different_Directories_Get_Distinct_Outputs()
    {
        var dir = NewScratchDirectory();
        var dirA = NewScratchDirectory();
        var dirB = NewScratchDirectory();

        // Two opted-in references whose compile assets share a file name but hold different
        // assemblies (renamed assets) — their publicized copies must not collide.
        var renamedTargetLib = Path.Combine(dirA, "Common.dll");
        File.Copy(TargetLibPath, renamedTargetLib);
        var renamedMocks = Path.Combine(dirB, "Common.dll");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TUnit.Mocks.dll"), renamedMocks);

        var task = CreateTask(dir, TargetLibName, "TUnit.Mocks");
        task.ReferencePaths = [new TaskItem(renamedTargetLib), new TaskItem(renamedMocks)];

        await Assert.That(task.Execute()).IsTrue();
        await Assert.That(task.PublicizedReferences.Length).IsEqualTo(2);

        var copyPaths = task.PublicizedReferences.Select(p => p.ItemSpec).ToList();
        await Assert.That(copyPaths[0]).IsNotEqualTo(copyPaths[1]);
        // Each copy holds the assembly it was publicized from, not the other name's overwrite.
        var identities = copyPaths.Select(p => AssemblyName.GetAssemblyName(p).Name).ToList();
        await Assert.That(identities).Contains(TargetLibName);
        await Assert.That(identities).Contains("TUnit.Mocks");
    }

    [Test]
    public async Task Requested_Name_Matches_Assembly_Identity_When_Compile_Asset_Is_Renamed()
    {
        var dir = NewScratchDirectory();
        var renamedDir = NewScratchDirectory();
        var renamed = Path.Combine(renamedDir, "VendorAlias.dll");
        File.Copy(TargetLibPath, renamed);

        // The documented contract is the simple ASSEMBLY name — a compile asset renamed
        // relative to the identity it contains must still resolve, not fail with TUMIA001.
        var engine = new StubBuildEngine();
        var task = CreateTask(dir, TargetLibName);
        task.BuildEngine = engine;
        task.ReferencePaths = [new TaskItem(renamed)];

        await Assert.That(task.Execute()).IsTrue();
        await Assert.That(engine.Errors.Count).IsEqualTo(0);
        await Assert.That(task.PublicizedReferences[0].GetMetadata("Original")).IsEqualTo(renamed);

        var source = await File.ReadAllTextAsync(task.GeneratedSourceFile);
        await Assert.That(source).Contains($"IgnoresAccessChecksTo(\"{TargetLibName}\")");
    }

    private static void CreateDifferentVersionCopy(string source, string destination)
    {
        using var module = Mono.Cecil.ModuleDefinition.ReadModule(source);
        module.Assembly.Name.Version = new Version(99, 0, 0, 0);
        module.Write(destination);
    }

    [Test]
    public async Task Task_Version_Change_Invalidates_Publicized_Copy()
    {
        var dir = NewScratchDirectory();

        var first = CreateTask(dir, TargetLibName);
        await Assert.That(first.Execute()).IsTrue();

        var rewritten = first.PublicizedReferences[0].ItemSpec;
        var signaturePath = rewritten + ".sig";

        // Emulate a copy produced by an older TUnit.Mocks: same source path and hash, different
        // task version on the first signature line.
        var lines = (await File.ReadAllTextAsync(signaturePath)).Split('\n');
        lines[0] = "0.0.0-previous-task-version";
        await File.WriteAllTextAsync(signaturePath, string.Join("\n", lines));

        var staleWrite = File.GetLastWriteTimeUtc(rewritten).AddMinutes(-5);
        File.SetLastWriteTimeUtc(rewritten, staleWrite);

        var second = CreateTask(dir, TargetLibName);
        await Assert.That(second.Execute()).IsTrue();

        await Assert.That(File.GetLastWriteTimeUtc(rewritten)).IsNotEqualTo(staleWrite);
    }

    [Test]
    public async Task Publicize_Promotes_Only_Assembly_Visible_Members()
    {
        var dir = NewScratchDirectory();
        var task = CreateTask(dir, TargetLibName);

        await Assert.That(task.Execute()).IsTrue();

        var context = new AssemblyLoadContext("visibility-probe", isCollectible: true);
        try
        {
            var assembly = context.LoadFromAssemblyPath(task.PublicizedReferences[0].ItemSpec);
            var surface = assembly.GetType("FakeSdk.PublicSurface", throwOnError: true)!;

            // internal -> public.
            var internalHelper = surface.GetMethod("InternalHelper", BindingFlags.Instance | BindingFlags.Public);
            await Assert.That(internalHelper).IsNotNull();

            // private overload stays private — promoting it would rebind Describe(null) in
            // consuming code from the object overload to the string overload.
            var describeString = surface.GetMethod(
                "Describe", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, [typeof(string)], modifiers: null)!;
            await Assert.That(describeString.IsPrivate).IsTrue();

            // protected stays protected — already accessible where it matters (derived mocks).
            var protectedHook = surface.GetMethod("ProtectedHook", BindingFlags.Instance | BindingFlags.NonPublic)!;
            await Assert.That(protectedHook.IsFamily).IsTrue();

            // Nested types: internal -> public, private stays private.
            var internalNested = surface.GetNestedType("InternalNested", BindingFlags.Public);
            await Assert.That(internalNested).IsNotNull();
            var privateNested = surface.GetNestedType("PrivateNested", BindingFlags.NonPublic)!;
            await Assert.That(privateNested.IsNestedPrivate).IsTrue();
        }
        finally
        {
            context.Unload();
        }
    }

    [Test]
    public async Task Generated_Source_Uses_Real_Assembly_Identity_When_File_Name_Differs()
    {
        var dir = NewScratchDirectory();
        var renamedDir = NewScratchDirectory();
        var renamed = Path.Combine(renamedDir, "Renamed.Lib.dll");
        File.Copy(TargetLibPath, renamed);

        var task = CreateTask(dir, "Renamed.Lib");
        task.ReferencePaths = [new TaskItem(renamed)];

        await Assert.That(task.Execute()).IsTrue();

        // The runtime matches IgnoresAccessChecksTo against the assembly's real identity, not
        // the file name the reference was requested by.
        var source = await File.ReadAllTextAsync(task.GeneratedSourceFile);
        await Assert.That(source).Contains($"IgnoresAccessChecksTo(\"{TargetLibName}\")");
        await Assert.That(source).DoesNotContain("IgnoresAccessChecksTo(\"Renamed.Lib\")");
    }

    [Test]
    public async Task Unreadable_Assembly_Fails_With_TUMIA005_Not_An_Unhandled_Exception()
    {
        var dir = NewScratchDirectory();
        var garbage = Path.Combine(dir, "Garbage.Assembly.dll");
        await File.WriteAllTextAsync(garbage, "this is not a PE file");

        var engine = new StubBuildEngine();
        var task = CreateTask(dir, "Garbage.Assembly");
        task.BuildEngine = engine;
        task.ReferencePaths = [new TaskItem(garbage)];

        await Assert.That(task.Execute()).IsFalse();
        await Assert.That(engine.Errors.Count).IsEqualTo(1);
        await Assert.That(engine.Errors[0].Code).IsEqualTo("TUMIA005");
    }

    [Test]
    public async Task Reference_Assembly_Without_Implementation_Warns_With_TUMIA003()
    {
        var dir = NewScratchDirectory();
        var refAsmDir = NewScratchDirectory();
        var refAsm = Path.Combine(refAsmDir, TargetLibName + ".dll");
        CreateReferenceAssemblyCopy(TargetLibPath, refAsm);

        var engine = new StubBuildEngine();
        var task = CreateTask(dir, TargetLibName);
        task.BuildEngine = engine;
        task.ReferencePaths = [new TaskItem(refAsm)];

        await Assert.That(task.Execute()).IsTrue();
        await Assert.That(engine.Warnings.Count).IsEqualTo(1);
        await Assert.That(engine.Warnings[0].Code).IsEqualTo("TUMIA003");
    }

    [Test]
    public async Task Reference_Assembly_Falls_Back_To_Runtime_Implementation()
    {
        var dir = NewScratchDirectory();
        var refAsmDir = NewScratchDirectory();
        var refAsm = Path.Combine(refAsmDir, TargetLibName + ".dll");
        CreateReferenceAssemblyCopy(TargetLibPath, refAsm);

        var engine = new StubBuildEngine();
        var task = CreateTask(dir, TargetLibName);
        task.BuildEngine = engine;
        task.ReferencePaths = [new TaskItem(refAsm)];
        task.RuntimeAssemblies = [new TaskItem(TargetLibPath)];

        await Assert.That(task.Execute()).IsTrue();
        await Assert.That(engine.Warnings.Count).IsEqualTo(0);

        // The swap must still Remove the compiler's item (the ref assembly), while the
        // publicized bits come from the implementation.
        await Assert.That(task.PublicizedReferences[0].GetMetadata("Original")).IsEqualTo(refAsm);
    }

    [Test]
    public async Task Renamed_Reference_Assembly_Resolves_Implementation_By_Assembly_Identity()
    {
        // The compile asset's file name can differ from the assembly identity it contains; the
        // implementation then sits among the runtime assets under the REAL name, so a
        // file-name-only match would miss it and publicize the stripped ref assembly (TUMIA003).
        var dir = NewScratchDirectory();
        var refAsmDir = NewScratchDirectory();
        var refAsm = Path.Combine(refAsmDir, "Renamed.Compile.Asset.dll");
        CreateReferenceAssemblyCopy(TargetLibPath, refAsm);

        var engine = new StubBuildEngine();
        var task = CreateTask(dir, "Renamed.Compile.Asset");
        task.BuildEngine = engine;
        task.ReferencePaths = [new TaskItem(refAsm)];
        task.RuntimeAssemblies = [new TaskItem(TargetLibPath)];

        await Assert.That(task.Execute()).IsTrue();
        await Assert.That(engine.Warnings.Count).IsEqualTo(0);
        await Assert.That(task.PublicizedReferences[0].GetMetadata("Original")).IsEqualTo(refAsm);

        // The publicized bits must come from the implementation — the ref-assembly copy would
        // still carry ReferenceAssemblyAttribute.
        var publicized = task.PublicizedReferences[0].ItemSpec;
        using var module = Mono.Cecil.ModuleDefinition.ReadModule(publicized);
        var isRefAssembly = module.Assembly.CustomAttributes.Any(a =>
            a.AttributeType.FullName == "System.Runtime.CompilerServices.ReferenceAssemblyAttribute");
        await Assert.That(isRefAssembly).IsFalse();
    }

    private static void CreateReferenceAssemblyCopy(string source, string destination)
    {
        using var module = Mono.Cecil.ModuleDefinition.ReadModule(source);
        var attributeType = new Mono.Cecil.TypeReference(
            "System.Runtime.CompilerServices", "ReferenceAssemblyAttribute",
            module, module.TypeSystem.CoreLibrary);
        var constructor = new Mono.Cecil.MethodReference(".ctor", module.TypeSystem.Void, attributeType)
        {
            HasThis = true,
        };
        module.Assembly.CustomAttributes.Add(new Mono.Cecil.CustomAttribute(constructor));
        module.Write(destination);
    }

    [Test]
    public async Task Unresolved_Assembly_Name_Fails_With_TUMIA001()
    {
        var dir = NewScratchDirectory();
        var engine = new StubBuildEngine();
        var task = CreateTask(dir, "No.Such.Assembly");
        task.BuildEngine = engine;

        await Assert.That(task.Execute()).IsFalse();
        await Assert.That(engine.Errors.Count).IsEqualTo(1);
        await Assert.That(engine.Errors[0].Code).IsEqualTo("TUMIA001");
    }

    private sealed class StubBuildEngine : IBuildEngine
    {
        public List<BuildErrorEventArgs> Errors { get; } = [];

        public List<BuildWarningEventArgs> Warnings { get; } = [];

        public bool ContinueOnError => false;

        public int LineNumberOfTaskNode => 0;

        public int ColumnNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => "";

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => false;

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
        }

        public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
        }

        public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
    }
}
