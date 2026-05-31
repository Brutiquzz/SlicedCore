using System;

namespace ProjectTemplate.Dependencies.Attributes;

/// <summary>
/// Classifies a Sliced Core feature as either a state-changing command or a read-only query.
/// Used by source generators to route mediator extensions to the correct typed dispatcher.
/// </summary>
public enum FeatureType
{
    /// <summary>
    /// A state-changing feature. Extensions are emitted on <see cref="ProjectTemplate.Dependencies.Commands"/>
    /// so only command infrastructure layers can invoke it cross-feature.
    /// </summary>
    Command,

    /// <summary>
    /// A read-only feature. Extensions are emitted on <see cref="ProjectTemplate.Dependencies.Queries"/>
    /// so application layers can invoke it cross-feature.
    /// </summary>
    Query,
}

/// <summary>
/// Marks a top-level partial class as a Sliced Core feature, enabling source generators
/// to produce all layer boilerplate (presentation, application, infrastructure handlers,
/// mediator extensions, and DTOs) for the annotated feature.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class FeatureAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="FeatureAttribute"/>.
    /// </summary>
    /// <param name="featureType">
    /// Whether this feature is a <see cref="FeatureType.Command"/> or a <see cref="FeatureType.Query"/>.
    /// </param>
    public FeatureAttribute(FeatureType featureType)
    {
        FeatureType = featureType;
    }

    /// <summary>Gets the feature classification.</summary>
    public FeatureType FeatureType { get; }
}
