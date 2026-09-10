using System.Collections.Concurrent;

namespace TUnit.RpcTests.Clients;

public class LogsCollector : ConcurrentBag<TestingPlatformClient.Log>;
