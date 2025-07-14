using System;
using System.Threading.Channels;
using System.Threading.Tasks;

Console.WriteLine("Testing channel behavior...");

var channel = Channel.CreateUnbounded<int>();

// Producer
var producer = Task.Run(async () =>
{
    for (int i = 0; i < 5; i++)
    {
        await channel.Writer.WriteAsync(i);
        Console.WriteLine($"Produced: {i}");
        await Task.Delay(100);
    }
    
    Console.WriteLine("Producer signaling completion");
    channel.Writer.TryComplete();
});

// Consumer
var consumer = Task.Run(async () =>
{
    await foreach (var item in channel.Reader.ReadAllAsync())
    {
        Console.WriteLine($"Consumed: {item}");
    }
    Console.WriteLine("Consumer finished");
});

await Task.WhenAll(producer, consumer);
Console.WriteLine("All done!");
