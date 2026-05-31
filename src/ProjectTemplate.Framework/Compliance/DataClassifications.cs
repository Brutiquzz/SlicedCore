using Microsoft.Extensions.Compliance.Classification;

namespace ProjectTemplate.Compliance;

/// <summary>
/// Defines data classification taxonomy for this application.
/// Tag properties with these attributes so the redaction pipeline
/// automatically masks them before they are written to any log sink.
/// </summary>
public static class DataClassifications
{
    private const string TaxonomyName = "Taxonomy";

    /// <summary>Personally Identifiable Information (PII) — masked in production logs.</summary>
    public static DataClassification Pii => new(TaxonomyName, nameof(Pii));

    /// <summary>Data that is safe to log as-is.</summary>
    public static DataClassification Public => new(TaxonomyName, nameof(Public));
}

/// <summary>
/// Apply to properties that contain PII.
/// The redaction pipeline will mask these values before writing to logs.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PiiDataAttribute() : DataClassificationAttribute(DataClassifications.Pii);

/// <summary>Apply to properties that are safe to log in plain text.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PublicDataAttribute() : DataClassificationAttribute(DataClassifications.Public);
