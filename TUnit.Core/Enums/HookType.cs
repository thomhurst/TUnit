namespace TUnit.Core;

public enum HookType
{ 
    /// <summary>
    /// Instance hook to run before/after every test in the same class
    /// </summary>
    EachTest,
    
    /// <summary>
    /// Static hook to run once on class set up and tear down
    /// </summary>
    Class,
    
    /// <summary>
    /// Static hook to run once on assembly set up and tear down
    /// </summary>
    Assembly,
}