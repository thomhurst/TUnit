using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;

namespace TUnit.TestAdapter;

public class SourceLocationHelper(IMessageLogger logger) : IDisposable
{
    private static SourceLocation GetEmptySourceLocation(string source) => new(source, null, 0, 0);
    
    private readonly ReflectionMetadataProvider _metadataProvider = new(logger);
    private readonly Dictionary<string, DiaSession> _sessionsByAssemblyPath = new (StringComparer.OrdinalIgnoreCase);

    public SourceLocation GetSourceLocation(string assemblyLocation, string className, string methodName)
    {
        try
        {
            var navigationData = TryGetNavigationData(assemblyLocation, className, methodName);

            if (navigationData is null)
            {
                logger.SendMessage(TestMessageLevel.Error, $"No navigation data found for {className}.{methodName}");
                logger.SendMessage(TestMessageLevel.Error, $"Assembly: {assemblyLocation}");

                return GetEmptySourceLocation(assemblyLocation);
            }
            
            return navigationData with { RawSource = assemblyLocation };
        }
        catch (Exception e)
        {
            logger.SendMessage(TestMessageLevel.Error, $"Error retrieving source location for {className}.{methodName}");
            logger.SendMessage(TestMessageLevel.Error, e.ToString());
            
            return GetEmptySourceLocation(assemblyLocation);
        }
    }
    
    private SourceLocation? TryGetNavigationData(string assemblyLocation, string className, string methodName)
    {
        var sessionData = TryGetSessionData(assemblyLocation, className, methodName);

        if (sessionData != null)
        {
            return sessionData;
        }

        var stateMachine =
            _metadataProvider.GetStateMachineType(assemblyLocation, className, methodName);

        if (stateMachine != null)
        {
            sessionData = TryGetSessionData(stateMachine.Assembly.Location, stateMachine.FullName!, "MoveNext");

            if (sessionData != null)
            {
                return sessionData;
            }
        }
        
        var declaringType2 =
            _metadataProvider.GetStateMachineType(assemblyLocation, className, methodName);

        if (declaringType2 != null)
        {
            sessionData = TryGetSessionData(declaringType2.Assembly.Location, declaringType2.FullName!, methodName);

            if (sessionData != null)
            {
                return sessionData;
            }
        }

        return null;
    }

    private SourceLocation? TryGetSessionData(string assemblyPath, string declaringTypeFullName, string methodName)
    {
        if (!_sessionsByAssemblyPath.TryGetValue(assemblyPath, out var session))
        {
            session = new DiaSession(assemblyPath);
            _sessionsByAssemblyPath.Add(assemblyPath, session);
        }
        
        var data = session.GetNavigationData(declaringTypeFullName, methodName);

        return string.IsNullOrEmpty(data?.FileName) 
            ? null 
            : new SourceLocation(assemblyPath, data.FileName, data.MinLineNumber, data.MaxLineNumber);
    }

    public void Dispose()
    {
        foreach (var diaSession in _sessionsByAssemblyPath.Values)
        {
            diaSession.Dispose();
        }
    }
}