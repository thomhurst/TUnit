using System.Reflection;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TUnit.Engine
{
    internal sealed class ReflectionMetadataProvider(IMessageLogger messageLogger)
    {
        public Type? GetStateMachineType(string assemblyPath, string reflectedTypeName, string methodName)
        {
            var method = TryGetSingleMethod(assemblyPath, reflectedTypeName, methodName);
            if (method == null)
            {
                return null;
            }

            var candidate = null as Type;

            foreach (var attributeData in CustomAttributeData.GetCustomAttributes(method))
            {
                for (var current = attributeData.Constructor.DeclaringType; current != null; current = current.GetTypeInfo().BaseType)
                {
                    if (current.FullName != "System.Runtime.CompilerServices.StateMachineAttribute")
                    {
                        continue;
                    }

                    var parameters = attributeData.Constructor.GetParameters();
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].Name != "stateMachineType")
                        {
                            continue;
                        }

                        if (attributeData.ConstructorArguments[i].Value is Type argument)
                        {
                            if (candidate != null)
                            {
                                return null;
                            }

                            candidate = argument;
                        }
                    }
                }
            }

            return candidate;
        }

        private MethodInfo? TryGetSingleMethod(string assemblyPath, string reflectedTypeName, string methodName)
        {
            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

                var type = assembly.GetType(reflectedTypeName, throwOnError: false);

                var methods = type?.GetMethods().Where(m => m.Name == methodName).Take(2).ToList();
                return methods?.Count == 1 ? methods[0] : null;
            }
            catch (FileNotFoundException e)
            {
                messageLogger.SendMessage(TestMessageLevel.Error, $"Error getting single method from {reflectedTypeName} {methodName}");
                messageLogger.SendMessage(TestMessageLevel.Error, e.ToString());
                return null;
            }
        }
    }
}
