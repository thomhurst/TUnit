using System.Collections.Concurrent;
using System.Text;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;

namespace TUnit.Engine.Reporters;

public class GitHubReporter(IExtension extension) : IDataConsumer, ITestApplicationLifecycleCallbacks
{
    private string _outputSummaryFilePath = null!;
    
    public async Task<bool> IsEnabledAsync()
    {
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is null)
        {
            return false;
        }

        if (Environment.GetEnvironmentVariable("OUTPUT_SUMMARY") is not { } fileName
            || !File.Exists(fileName))
        {
            return false;
        }

        _outputSummaryFilePath = fileName;
        
        return await extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;
    
    private readonly ConcurrentDictionary<string, List<TestNodeUpdateMessage>> _updates = [];
    
    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        var testNodeUpdateMessage = (TestNodeUpdateMessage)value;

        _updates.GetOrAdd(testNodeUpdateMessage.TestNode.Uid.Value, []).Add(testNodeUpdateMessage);
        
        return Task.CompletedTask;
    }

    public Type[] DataTypesConsumed { get; } = [typeof(TestNodeUpdateMessage)];
    
    public Task BeforeRunAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task AfterRunAsync(int exitCode, CancellationToken cancellation)
    {
        var last = _updates.ToDictionary(x => x.Key, x => x.Value.Last());

        var passedCount = last.Count(x => x.Value.Properties.AsEnumerable().Any(p => p is PassedTestNodeStateProperty));
        var failed = last.Where(x => x.Value.Properties.AsEnumerable().Any(p => p is FailedTestNodeStateProperty or ErrorTestNodeStateProperty)).ToArray();
        var cancelled = last.Where(x => x.Value.Properties.AsEnumerable().Any(p => p is CancelledTestNodeStateProperty)).ToArray();
        var timeout = last.Where(x => x.Value.Properties.AsEnumerable().Any(p => p is TimeoutTestNodeStateProperty)).ToArray();
        var skipped = last.Where(x => x.Value.Properties.AsEnumerable().Any(p => p is SkippedTestNodeStateProperty)).ToArray();
        var inProgress = last.Where(x => x.Value.Properties.AsEnumerable().Any(p => p is InProgressTestNodeStateProperty)).ToArray();

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("# Summary");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("| Count | Status |");
        stringBuilder.AppendLine("| _ | _ |");
        stringBuilder.AppendLine($"| {passedCount} | Passed |");
        stringBuilder.AppendLine($"| {failed.Length} | Failed |");
        
        if(skipped.Length > 0)
        {
            stringBuilder.AppendLine($"| {skipped.Length} | Skipped |");
        }
        
        if(timeout.Length > 0)
        {
            stringBuilder.AppendLine($"| {timeout.Length} | Timed Out |");
        }
        
        if(cancelled.Length > 0)
        {
            stringBuilder.AppendLine($"| {cancelled.Length} | Cancelled |");
        }

        if(inProgress.Length > 0)
        {
            stringBuilder.AppendLine($"| {inProgress.Length} | In Progress (never completed) |");
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("## Information");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("| Test | Status | Start | End | Duration |");
        stringBuilder.AppendLine("| _ | _ | _ | _ | _ |");

        foreach (var testNodeUpdateMessage in last.Values)
        {
            var name = testNodeUpdateMessage.TestNode.DisplayName;
            
            var status = GetStatus(testNodeUpdateMessage);

            if (status == "Passed")
            {
                continue;
            }

            var timingProperty = testNodeUpdateMessage.Properties.AsEnumerable().OfType<TimingProperty>().FirstOrDefault();
            
            var start = timingProperty
                ?.GlobalTiming.StartTime;
            
            var end = timingProperty
                ?.GlobalTiming.StartTime;
            
            var duration = timingProperty
                ?.GlobalTiming.Duration;
            
            stringBuilder.AppendLine($"| {name} | {status} | {start} | {end} | {duration} |");
        }

#if NET
        return File.WriteAllTextAsync(_outputSummaryFilePath, stringBuilder.ToString(), Encoding.UTF8, cancellation);
#else
        File.WriteAllText(_outputSummaryFilePath, stringBuilder.ToString(), Encoding.UTF8);
        return Task.CompletedTask;
#endif
    }

    private static string GetStatus(TestNodeUpdateMessage testNodeUpdateMessage)
    {
        var stateProperty = testNodeUpdateMessage.Properties.OfType<TestNodeStateProperty>().FirstOrDefault();

        return stateProperty switch
        {
            CancelledTestNodeStateProperty => "Cancelled",
            ErrorTestNodeStateProperty or FailedTestNodeStateProperty => "Failed",
            InProgressTestNodeStateProperty => "In Progress (never finished)",
            PassedTestNodeStateProperty => "Passed",
            SkippedTestNodeStateProperty => "Skipped",
            TimeoutTestNodeStateProperty => "Timed Out",
            _ => "Unknown"
        };
    }
}