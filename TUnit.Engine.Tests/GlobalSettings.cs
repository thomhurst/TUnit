using TUnit.Core.Helpers;
using TUnit.Engine.Tests.Attributes;

[assembly: ParallelLimiter<DefaultParallelLimit>]
[assembly: SetDisplayNameWithClass]
[assembly: Timeout(300_000)]