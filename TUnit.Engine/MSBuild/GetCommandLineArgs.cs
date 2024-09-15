using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Task = Microsoft.Build.Utilities.Task;

namespace TUnit.Engine.MSBuild;

public sealed class GetCommandLineArgs : Task
{
    [Output] public ITaskItem2[] CommandLineArgs { get; private set; } = [];

    public override bool Execute() {
        CommandLineArgs = Environment.GetCommandLineArgs().Select(a => new TaskItem(a)).Cast<ITaskItem2>().ToArray();
        return true;
    }
}