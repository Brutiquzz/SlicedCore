using Microsoft.Extensions.Compliance.Redaction;

namespace ProjectTemplate.Compliance;

/// <summary>
/// A no-op redactor that returns the value unchanged.
/// Registered for Development so PII is visible in local logs
/// without changing any call sites or log method signatures.
/// </summary>
public sealed class PassThroughRedactor : Redactor
{
    public override int GetRedactedLength(ReadOnlySpan<char> input) => input.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        source.CopyTo(destination);
        return source.Length;
    }
}
