var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("postgresdb");

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// API Service
var apiService = builder.AddProject<Projects.CloudShop_ApiService>("apiservice")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health")
    .WithoutHttpsCertificate();

// Worker Service
builder.AddProject<Projects.CloudShop_Worker>("worker")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(apiService)
    .WithoutHttpsCertificate();

// Web Frontend
builder.AddProject<Projects.CloudShop_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health")
    .WithoutHttpsCertificate();

builder.Build().Run();
