using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace EcShop.Api.Filters;

public sealed partial class LoggingEndpointFilter : IEndpointFilter
{
    private const int MaxStringLength = 200;
    private const int MaxDepth = 3;
    private const int MaxCollectionItems = 10;
    private const int MaxProperties = 16;
    private static readonly string[] SensitiveMarkers = ["password", "token", "secret", "credential", "authorization", "api_key", "apikey"];

    private readonly ILogger<LoggingEndpointFilter> _logger;

    public LoggingEndpointFilter(ILogger<LoggingEndpointFilter> logger) => _logger = logger;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var method = httpContext.Request.Method;
        var route = Route(httpContext);
        var stopwatch = Stopwatch.StartNew();
        if (_logger.IsEnabled(LogLevel.Debug)) LogRequest(method, route, DescribeArguments(context.Arguments));
        try
        {
            var result = await next(context);
            stopwatch.Stop();
            var statusCode = (result as IStatusCodeHttpResult)?.StatusCode ?? httpContext.Response.StatusCode;
            LogResponse(method, route, statusCode, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            LogFailure(method, route, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "{HttpMethod} {Route} args: {Arguments}")]
    private partial void LogRequest(string httpMethod, string route, string arguments);

    [LoggerMessage(Level = LogLevel.Information, Message = "{HttpMethod} {Route} responded {StatusCode} in {ElapsedMilliseconds}ms")]
    private partial void LogResponse(string httpMethod, string route, int statusCode, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "{HttpMethod} {Route} failed after {ElapsedMilliseconds}ms")]
    private partial void LogFailure(string httpMethod, string route, long elapsedMilliseconds, Exception exception);

    private static string Route(HttpContext httpContext)
    {
        var pattern = httpContext.GetEndpoint()?.Metadata.GetMetadata<RouteEndpoint>()?.RoutePattern.RawText;
        return pattern ?? httpContext.Request.Path.Value ?? "/";
    }

    private static string DescribeArguments(IList<object?> arguments)
    {
        var parts = new List<string>(arguments.Count);
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            if (ShouldSkip(argument)) continue;
            parts.Add(Describe(argument, 0));
        }
        return parts.Count == 0 ? "[]" : $"[{string.Join(", ", parts)}]";
    }

    private static bool ShouldSkip(object? value) => value switch
    {
        null => true,
        CancellationToken => true,
        HttpContext or HttpRequest or HttpResponse or ClaimsPrincipal or Stream => true,
        _ => IsFrameworkType(value.GetType())
    };

    private static bool IsFrameworkType(Type type)
    {
        if (type.Namespace is not { } ns) return false;
        if (IsSimple(type)) return false;
        return ns.StartsWith("Microsoft.", StringComparison.Ordinal)
            || ns.StartsWith("System.", StringComparison.Ordinal)
            || ns.StartsWith("EcShop.Infrastructure", StringComparison.Ordinal);
    }

    private static bool IsSimple(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly)
            || underlying == typeof(Guid)
            || underlying == typeof(TimeSpan);
    }

    private static string Describe(object? value, int depth)
    {
        if (value is null) return "null";
        if (value is string text) return Quote(Truncate(text));
        if (value is byte[] bytes) return $"<{bytes.Length} bytes>";
        if (IsSimple(value.GetType())) return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
        if (depth >= MaxDepth) return "...";
        if (value is IEnumerable enumerable) return DescribeEnumerable(enumerable, depth);
        return DescribeObject(value, depth);
    }

    private static string DescribeEnumerable(IEnumerable enumerable, int depth)
    {
        var parts = new List<string>();
        var more = false;
        foreach (var item in enumerable)
        {
            if (parts.Count >= MaxCollectionItems) { more = true; break; }
            parts.Add(Describe(item, depth + 1));
        }
        if (more) parts.Add("...");
        return $"[{string.Join(", ", parts)}]";
    }

    private static string DescribeObject(object value, int depth)
    {
        var parts = new List<string>();
        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;
            if (parts.Count >= MaxProperties) { parts.Add("..."); break; }
            var display = IsSensitive(property.Name) ? "***" : Describe(Read(property, value), depth + 1);
            parts.Add($"{property.Name} = {display}");
        }
        return $"{value.GetType().Name} {{ {string.Join(", ", parts)} }}";
    }

    private static object? Read(PropertyInfo property, object instance)
    {
        try
        {
            return property.GetValue(instance);
        }
        catch (Exception)
        {
            return "?";
        }
    }

    private static bool IsSensitive(string name) => SensitiveMarkers.Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string Truncate(string text) => text.Length <= MaxStringLength ? text : text[..MaxStringLength] + "…";

    private static string Quote(string text)
    {
        var builder = new StringBuilder(text.Length + 2);
        builder.Append('"');
        foreach (var character in text)
        {
            if (character is '"' or '\\') builder.Append('\\');
            builder.Append(character);
        }
        builder.Append('"');
        return builder.ToString();
    }
}
