using System.Collections.Concurrent;
using TUnit.RpcTests.Models;

namespace TUnit.RpcTests.Clients;

public class TelemetryCollector : ConcurrentBag<TelemetryPayload>;
