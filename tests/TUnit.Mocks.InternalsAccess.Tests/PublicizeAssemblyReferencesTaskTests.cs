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

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
        }
    }
}
