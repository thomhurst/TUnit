using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;

namespace TUnit.TestAdapter;

public class SourceLocationHelper
{
    private readonly IMessageLogger? _logger;
    private static readonly SourceLocation EmptySourceLocation = new(null, 0, 0);

    public SourceLocationHelper(IMessageLogger? logger)
    {
        _logger = logger;
    }
    
    public SourceLocation GetSourceLocation(DiaSession diaSession, string className, string methodName)
    {
        try
        {
            var navigationData = diaSession.GetNavigationDataForMethod(className, methodName);

            if (navigationData is null)
            {
                _logger?.SendMessage(TestMessageLevel.Error, $"No navigation data found for {className}.{methodName}");
                
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
}