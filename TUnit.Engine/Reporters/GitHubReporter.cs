﻿using System.Collections.Concurrent;
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

        if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is not { } fileName
            || !File.Exists(fileName))
        {
            return false;
        }

        _outputSummaryFilePath = fileName;
        
        return await extension.IsEnabledAsync();
    }

    public string Uid { get; } = $"{extension.Uid}GitHubReporter";

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
        if (_updates.Count is 0)
        {
            return Task.CompletedTask;
        }
        
        var last = _updates.ToDictionary(x => x.Key, x => x.Value.Last());
        
        var passedCount = last.Count(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is PassedTestNodeStateProperty));
        var failed = last.Where(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is FailedTestNodeStateProperty or ErrorTestNodeStateProperty)).ToArray();
        var cancelled = last.Where(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is CancelledTestNodeStateProperty)).ToArray();
        var timeout = last.Where(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is TimeoutTestNodeStateProperty)).ToArray();
        var skipped = last.Where(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is SkippedTestNodeStateProperty)).ToArray();
        var inProgress = last.Where(x => x.Value.TestNode.Properties.AsEnumerable().Any(p => p is InProgressTestNodeStateProperty)).ToArray();

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("# Summary");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("| Count | Status |");
        stringBuilder.AppendLine("| --- | --- |");
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
        stringBuilder.AppendLine("| Test | Status | Details | Duration |");
        stringBuilder.AppendLine("| --- | --- | --- | --- |");

        foreach (var testNodeUpdateMessage in last.Values)
        {
            var name = testNodeUpdateMessage.TestNode.DisplayName;

            var stateProperty = testNodeUpdateMessage.TestNode.Properties.OfType<TestNodeStateProperty>().FirstOrDefault();
            
            if (stateProperty is PassedTestNodeStateProperty)
            {
                continue;
            }
            
            var status = GetStatus(stateProperty);

            var details = GetDetails(stateProperty, testNodeUpdateMessage.TestNode.Properties);
            
            var timingProperty = testNodeUpdateMessage.TestNode.Properties.AsEnumerable().OfType<TimingProperty>().FirstOrDefault();
            
            var duration = timingProperty?.GlobalTiming.Duration;
            
            stringBuilder.AppendLine($"| {name} | {status} | {details} | {duration} |");
        }

#if NET
        return File.WriteAllTextAsync(_outputSummaryFilePath, stringBuilder.ToString(), Encoding.UTF8, cancellation);
#else
        File.WriteAllText(_outputSummaryFilePath, stringBuilder.ToString(), Encoding.UTF8);
        return Task.CompletedTask;
#endif
    }

    private string GetDetails(TestNodeStateProperty? stateProperty, PropertyBag properties)
    {
        if (stateProperty is FailedTestNodeStateProperty 
            or ErrorTestNodeStateProperty 
            or TimeoutTestNodeStateProperty
            or CancelledTestNodeStateProperty)
        {
            return GetError(stateProperty)!;
        }

        if (stateProperty is SkippedTestNodeStateProperty skippedTestNodeStateProperty)
        {
            return skippedTestNodeStateProperty.Explanation ?? "Skipped (No reason provided)";
        }

        if (stateProperty is InProgressTestNodeStateProperty)
        {
            var timingProperty = properties.AsEnumerable().OfType<TimingProperty>().FirstOrDefault();

            var start = timingProperty?.GlobalTiming.StartTime;
            var end = timingProperty?.GlobalTiming.EndTime;

            return $"Start: {start} | End: {end}";
        }
        
        return "Unknown Test State";
    }

    private string? GetError(TestNodeStateProperty? stateProperty)
    {
        return stateProperty switch
        {
            ErrorTestNodeStateProperty errorTestNodeStateProperty => errorTestNodeStateProperty.Exception?.ToString() ??
                                                                     errorTestNodeStateProperty.Explanation,
            FailedTestNodeStateProperty failedTestNodeStateProperty =>
                failedTestNodeStateProperty.Exception?.ToString() ?? failedTestNodeStateProperty.Explanation,
            TimeoutTestNodeStateProperty timeoutTestNodeStateProperty => timeoutTestNodeStateProperty.Exception
                ?.ToString() ?? timeoutTestNodeStateProperty.Explanation,
            CancelledTestNodeStateProperty cancelledTestNodeStateProperty => cancelledTestNodeStateProperty.Exception?.ToString() ?? cancelledTestNodeStateProperty.Explanation,
            _ => null
        };
    }

    private static string GetStatus(TestNodeStateProperty? stateProperty)
    {
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