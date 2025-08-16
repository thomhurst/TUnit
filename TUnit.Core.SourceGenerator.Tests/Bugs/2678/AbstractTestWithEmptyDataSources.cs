namespace TUnit.Core.SourceGenerator.Tests.Bugs._2678;

public abstract class AbstractTestWithEmptyDataSources(object collection, IEnumerable<object> wantedServiceDescriptors)
{
    protected IEnumerable<object> SingletonServices() => []; // This returns empty
    protected IEnumerable<object> TransientServices() => []; // This returns empty
    protected IEnumerable<object> ScopedServices() => []; // This returns empty
        
    [Test]
    [DisplayName("The service can be provided as singleton.")]
    [InstanceMethodDataSource(nameof(SingletonServices))]
    public async Task ServiceCanBeCreatedAsSingleton(object descriptor) 
    {
        await Task.Delay(1);
    }
    
    [Test]
    [DisplayName("The service can be provided as transient.")]
    [InstanceMethodDataSource(nameof(TransientServices))]
    public async Task ServiceCanBeCreatedAsTransient(object descriptor) 
    {
        await Task.Delay(1);
    } 

    [Test]
    [DisplayName("The service can be provided as scoped.")]
    [InstanceMethodDataSource(nameof(ScopedServices))]
    public async Task ServiceCanBeCreatedAsScoped(object descriptor) 
    {
        await Task.Delay(1);
    } 

    [Test]
    [DisplayName("A service matching has been registered")]
    [InstanceMethodDataSource(nameof(wantedServiceDescriptors))]
    public async Task ServiceIsRegistered(object matcher) 
    {
        await Task.Delay(1);
    } 
}