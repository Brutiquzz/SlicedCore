namespace ProjectTemplate.Domains.Sample;

public partial class GetSample
{
    /// <summary>Inbound request contract exposed to callers (e.g. route parameters).</summary>
    public interface IGetSampleRequest
    {
        int Id { get; set; }
    }

    /// <summary>Outbound response contract returned to the caller on success.</summary>
    public interface IGetSampleResponse
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
    }

    protected sealed partial class Core : Dependencies.Core
    {
        public interface IApplicationRequestDTO
        {
            int Id { get; set; }
        }

        public interface IApplicationResponseDTO
        {
            int Id { get; set; }
            string Name { get; set; }
            string Name2 { get; set; }
        }

        public interface IPersistenceRequestDTO
        {
            int Id { get; set; }
        }

        public interface IPersistenceResponseDTO
        {
            int Id { get; set; }
            string Name { get; set; }
            string Name2 { get; set; }
        }
    }
}

