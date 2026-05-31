namespace ProjectTemplate.Domains.Sample;

public partial class CreateSample
{
    /// <summary>Inbound request contract exposed to callers (e.g. HTTP request body), with PII annotations for log redaction.</summary>
    public interface ICreateSampleRequest
    {
        [PiiData] string Name { get; set; }
        [PiiData] string Name2 { get; set; }
    }

    /// <summary>Outbound response contract returned to the caller on success.</summary>
    public interface ICreateSampleResponse
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
    }

    /// <summary>Contracts for the <c>CreateSample</c> feature shared across all layers.</summary>
    protected sealed partial class Core : Dependencies.Core
    {
        /// <summary>Application-layer inbound DTO, forwarded from the presentation layer.</summary>
        public interface IApplicationRequestDTO
        {
            [PiiData] string Name { get; set; }
            [PublicData] string Name2 { get; set; }
        }

        /// <summary>Application-layer outbound DTO, returned to the presentation layer.</summary>
        public interface IApplicationResponseDTO
        {
            int Id { get; set; }
            string Name { get; set; }
            string Name2 { get; set; }
        }

        /// <summary>Infrastructure-layer inbound DTO, forwarded from the application layer for persistence.</summary>
        public interface IPersistenceRequestDTO
        {
            [PiiData] string Name { get; set; }
            [PublicData] string Name2 { get; }
        }

        /// <summary>Infrastructure-layer outbound DTO, returned after the entity has been persisted.</summary>
        public interface IPersistenceResponseDTO
        {
            int Id { get; set; }
            string Name { get; set; }
            string Name2 { get; set; }
        }
    }
}
