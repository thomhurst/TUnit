using CloudShop.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<WorkerDbContext>("postgresdb");
builder.AddRabbitMQClient("rabbitmq");

builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
