using System.Text;
using System.Text.Json;

namespace Commandor;

/// <summary>
/// Helper class to build cache keys for method calls.
/// Pattern: ServiceType.MethodName(arg1, arg2, ...)
/// </summary>
public static class CacheKeyBuilder
{
    public static string Build(Type serviceType, string methodName, params object?[] arguments)
    {
        var sb = new StringBuilder();
        sb.Append(serviceType.Name);
        sb.Append('.');
        sb.Append(methodName);
        sb.Append('(');
        
        for (int i = 0; i < arguments.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");
                
            var arg = arguments[i];
            if (arg == null)
            {
                sb.Append("null");
            }
            else if (arg is string str)
            {
                sb.Append('"');
                sb.Append(str);
                sb.Append('"');
            }
            else if (arg.GetType().IsPrimitive || arg is decimal || arg is DateTime || arg is DateTimeOffset)
            {
                sb.Append(arg);
            }
            else
            {
                // Complex objects - serialize to JSON for cache key
                var json = JsonSerializer.Serialize(arg);
                sb.Append(json);
            }
        }
        
        sb.Append(')');
        return sb.ToString();
    }
}


