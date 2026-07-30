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

        var rewritten = Path.Combine(dir, TargetLibName + ".dll");
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

        var rewritten = Path.Combine(dir, TargetLibName + ".dll");
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

        var rewritten = Path.Combine(dir, TargetLibName + ".dll");
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
    public async Task Publicize_Promotes_Only_Assembly_Visible_Members()
    {
        var dir = NewScratchDirectory();
        var task = CreateTask(dir, TargetLibName);

        await Assert.That(task.Execute()).IsTrue();

        var context = new AssemblyLoadContext("visibility-probe", isCollectible: true);
        try
        {
            var assembly = context.LoadFromAssemblyPath(Path.Combine(dir, TargetLibName + ".dll"));
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
