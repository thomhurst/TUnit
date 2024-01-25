using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;

namespace TUnit.TestAdapter;

public class SourceLocationHelper : IDisposable
{
    private readonly IMessageLogger? _logger;
    private static readonly SourceLocation EmptySourceLocation = new(null, 0, 0);
    private readonly ReflectionMetadataProvider _metadataProvider;
    
    private readonly Dictionary<string, DiaSession> _sessionsByAssemblyPath = new (StringComparer.OrdinalIgnoreCase);

    public SourceLocationHelper(IMessageLogger? logger)
    {
        _logger = logger;
        _metadataProvider = new ReflectionMetadataProvider();
    }
    
    public SourceLocation GetSourceLocation(string assemblyLocation, string className, string methodName)
    {
        try
        {
            var navigationData = TryGetNavigationData(assemblyLocation, className, methodName);

            if (navigationData is null)
            {
                _logger?.SendMessage(TestMessageLevel.Error, $"No navigation data found for {className}.{methodName}");
                _logger?.SendMessage(TestMessageLevel.Error, $"Assembly: {assemblyLocation}");

                return EmptySourceLocation;
            }
            
            return new SourceLocation(navigationData.FileName, navigationData.MinLineNumber, navigationData.MaxLineNumber);
        }
        catch (Exception e)
        {
            _logger?.SendMessage(TestMessageLevel.Error, $"Error retrieving source location for {className}.{methodName}");
            _logger?.SendMessage(TestMessageLevel.Error, e.ToString());
            
            return EmptySourceLocation;
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
    
    private Type? DoWithBreaker(string assemblyLocation, Func<string, string, string, TypeInfo?> method, string declaringTypeName, string methodName)
    {
        try
        {
            return method.Invoke(assemblyLocation, declaringTypeName, methodName);
        }
        catch
        {
            // Ignored
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
            : new SourceLocation(data.FileName, data.MinLineNumber, data.MaxLineNumber);
    }

    public void Dispose()
    {
        foreach (var diaSession in _sessionsByAssemblyPath.Values)
        {
            diaSession.Dispose();
        }
    }
}