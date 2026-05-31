using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using System.Reflection;
using System.Text;

namespace ProjectTemplate.Compliance;

/// <summary>
/// A generic log wrapper that reflects over <typeparamref name="T"/>'s properties and
/// redacts any property whose interface or concrete declaration carries a
/// <see cref="DataClassificationAttribute"/> (e.g. <see cref="PiiDataAttribute"/>).
///
/// Usage in [LoggerMessage]:
///   [LoggerMessage(LogLevel.Information, "Handling request: {Request}")]
///   private partial void LogHandling(RedactedLog&lt;IMyRequestDTO&gt; request);
///
///   // call site:
///   LogHandling(new RedactedLog&lt;IMyRequestDTO&gt;(request, redactorProvider));
/// </summary>
public sealed class RedactedLog<T>
{
    private readonly T _value;
    private readonly IRedactorProvider _redactorProvider;

    public RedactedLog(T value, IRedactorProvider redactorProvider)
    {
        _value = value;
        _redactorProvider = redactorProvider;
    }

    public override string ToString()
    {
        if (_value is null) return "(null)";

        // Build classification map from T (the declared interface/type) for attribute discovery.
        // Read actual values from the runtime concrete type so Mapster-generated proxies work correctly.
        var runtimeType = _value.GetType();
        var classificationMap = BuildClassificationMap(typeof(T));

        var sb = new StringBuilder();
        sb.Append("{ ");

        var first = true;
        foreach (var (name, classification) in classificationMap)
        {
            var runtimeProp = runtimeType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (runtimeProp is null || !runtimeProp.CanRead) continue;

            if (!first) sb.Append(", ");
            first = false;

            var raw = runtimeProp.GetValue(_value)?.ToString() ?? string.Empty;
            string logged;

            if (classification.HasValue)
            {
                var redactor = _redactorProvider.GetRedactor(new DataClassificationSet(classification.Value));
                logged = redactor.Redact(raw);
            }
            else
            {
                logged = raw;
            }

            sb.Append(name).Append(": ").Append(logged);
        }

        sb.Append(" }");
        return sb.ToString();
    }

    /// <summary>
    /// Builds a property-name → classification map by scanning <paramref name="type"/> itself
    /// (when it is an interface) plus all interfaces it extends.
    /// This ensures attributes declared directly on T are not missed.
    /// </summary>
    private static Dictionary<string, DataClassification?> BuildClassificationMap(Type type)
    {
        var map = new Dictionary<string, DataClassification?>();

        // When T is an interface include T itself; GetInterfaces() only returns parent interfaces.
        var typesToScan = type.IsInterface
            ? new[] { type }.Concat(type.GetInterfaces())
            : type.GetInterfaces().AsEnumerable();

        foreach (var t in typesToScan)
        {
            foreach (var prop in t.GetProperties())
            {
                if (map.ContainsKey(prop.Name)) continue;
                var attr = prop.GetCustomAttribute<DataClassificationAttribute>();
                map[prop.Name] = attr?.Classification;
            }
        }

        // For concrete T, also scan its own declared properties.
        if (!type.IsInterface)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (map.ContainsKey(prop.Name)) continue;
                var attr = prop.GetCustomAttribute<DataClassificationAttribute>();
                map[prop.Name] = attr?.Classification;
            }
        }

        return map;
    }
}
