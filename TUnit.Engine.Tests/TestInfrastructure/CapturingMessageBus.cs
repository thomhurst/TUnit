#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;

namespace TUnit.Engine.Tests;

internal sealed class CapturingMessageBus : IMessageBus
{
    public List<(IDataProducer Producer, IData Data)> Published = [];

    public Task PublishAsync(IDataProducer dataProducer, IData value)
    {
        Published.Add((dataProducer, value));
        return Task.CompletedTask;
    }
}
