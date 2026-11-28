var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("postgresdb");

var redis = builder.AddRedis("redis")
    .WithRedisInsight()
    .WithoutHttpsCertificate();

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// API Service
var apiService = builder.AddDotnetProject("apiservice", "../CloudShop.ApiService/CloudShop.ApiService.csproj")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health")
    .WithoutHttpsCertificate();

// Worker Service
builder.AddDotnetProject("worker", "../CloudShop.Worker/CloudShop.Worker.csproj")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(apiService)
    .WithoutHttpsCertificate();

// Web Frontend
builder.AddDotnetProject("webfrontend", "../CloudShop.Web/CloudShop.Web.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health")
    .WithoutHttpsCertificate();

builder.Build().Run();
