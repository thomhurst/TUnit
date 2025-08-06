using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Tests.Benchmark;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default.WithRuntime(CoreRuntime.Core90));
        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.GitHub);
        
        // Add the framework version column
        AddColumn(new FrameworkVersionColumn());
        
        // Add default columns
        AddColumn(TargetMethodColumn.Method);
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.Error);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(StatisticColumn.Median);
        AddColumn(BaselineRatioColumn.RatioMean);
        
        WithOptions(ConfigOptions.DisableLogFile);
    }
}