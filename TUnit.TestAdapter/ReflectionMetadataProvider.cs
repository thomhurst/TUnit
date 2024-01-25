using System.Reflection;
using System.Runtime.Loader;

namespace TUnit.TestAdapter
{
    internal sealed class ReflectionMetadataProvider
    {
        public Type? GetDeclaringType(string assemblyPath, string reflectedTypeName, string methodName)
        {
            var type = TryGetSingleMethod(assemblyPath, reflectedTypeName, methodName)?.DeclaringType;
            if (type == null)
            {
                return null;
            }

            if (type.IsConstructedGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            return type;
        }

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

        private static MethodInfo? TryGetSingleMethod(string assemblyPath, string reflectedTypeName, string methodName)
        {
            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

                var type = assembly.GetType(reflectedTypeName, throwOnError: false);

                var methods = type?.GetMethods().Where(m => m.Name == methodName).Take(2).ToList();
                return methods?.Count == 1 ? methods[0] : null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public void Dispose()
        {
        }
    }
}
