using BlessingStudio.WonderNetwork.Interfaces;
using System.Reflection;

namespace BlessingStudio.WonderNetwork.Utils;

public static class ReflectionUtils
{
    public static object Deserilize(Type type, ISerializer serilizer, byte[] data)
    {
        Type serilizerType = serilizer.GetType();
        Type interfaceType = serilizerType.GetInterfaces().FirstOrDefault(t =>
        {
            return t.GetGenericTypeDefinition() == typeof(ISerializer<>);
        });
        if (interfaceType.GenericTypeArguments.Length == 1)
        {
            Type genericType = interfaceType.GenericTypeArguments[0];
            if (type == genericType || type.IsSubclassOf(genericType) || type.GetInterfaces().Contains(genericType))
            {
                MethodInfo methodInfo = interfaceType.GetMethod("Deserialize")!;
                return methodInfo.Invoke(serilizer, new object[] { data })!;
            }
            throw new InvalidOperationException("Type is not the type or subclass in serilize");
        }
        throw new InvalidOperationException("Serilizer Error");
    }
    public static Type? GetType(string name)
    {
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        return allAssemblies.Select(assembly => assembly.GetType(name)).FirstOrDefault(assembly => assembly != null);
    }
}
