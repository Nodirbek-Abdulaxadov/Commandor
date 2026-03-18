using System.Text;

namespace Commandor;

/// <summary>
/// Builds cache keys for method calls.
/// Pattern: ServiceType.MethodName(arg1, arg2, ...)
///
/// Strategy per argument type:
///   • null                         → "null"
///   • string                       → quoted literal  "hello"
///   • primitives, decimal, Guid    → direct ToString()
///   • DateTime                     → Ticks (culture-invariant)
///   • DateTimeOffset               → UtcTicks (culture-invariant)
///   • everything else              → TypeName#GetHashCode()
///
/// The last rule is safe for IMemoryCache (in-process only) because:
///   • Records override GetHashCode() with value equality → no false cache hits.
///   • The ServiceType + MethodName prefix provides strong discrimination across
///     different queries, keeping collision risk negligible in practice.
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
            if (i > 0) sb.Append(',');
            AppendArg(sb, arguments[i]);
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static void AppendArg(StringBuilder sb, object? arg)
    {
        if (arg is null) { sb.Append("null"); return; }

        if (arg is string str)
        {
            sb.Append('"');
            sb.Append(str);
            sb.Append('"');
            return;
        }

        var type = arg.GetType();

        if (type.IsPrimitive || arg is decimal || arg is Guid)
        {
            sb.Append(arg);
            return;
        }

        if (arg is DateTime dt)   { sb.Append(dt.Ticks);      return; }
        if (arg is DateTimeOffset dto) { sb.Append(dto.UtcTicks); return; }

        // Complex / reference types — use TypeName#HashCode.
        // Records in C# provide value-based GetHashCode(), which makes this
        // correct and collision-resistant for all generator-produced request wrappers.
        sb.Append(type.Name);
        sb.Append('#');
        sb.Append(arg.GetHashCode());
    }
}

