using Imposter.Abstractions;
using TUnit.Mocks.Benchmarks;

[assembly: GenerateImposter(typeof(INotificationService))]
[assembly: GenerateImposter(typeof(ILogger))]
[assembly: GenerateImposter(typeof(IUserRepository))]
[assembly: GenerateImposter(typeof(ICalculatorService))]
