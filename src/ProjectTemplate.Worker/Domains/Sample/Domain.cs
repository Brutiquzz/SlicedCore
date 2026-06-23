using ProjectTemplate.Dependencies.Attributes;

namespace ProjectTemplate.Worker.Domains.Sample;

/// <summary>Domain container for the worker sample feature.</summary>
[Domain]
public class SampleDomain
{
    [BusinessModel]
    private interface ISample
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
    }

    [PersistenceModel]
    private interface ISampleEntity
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
    }
}
