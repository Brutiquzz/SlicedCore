namespace ProjectTemplate.Worker.Domains.Sample;

public partial class CreateSample
{
    /// <summary>Inbound job request contract exposed to callers.</summary>
    public interface ICreateSampleRequest
    {
        int Id { get; set; }
    }

    /// <summary>Outbound response contract returned on success.</summary>
    public interface ICreateSampleResponse
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
    }

    /// <summary>Contracts for the <c>CreateSample</c> worker feature shared across all layers.</summary>
    protected sealed partial class Core : Dependencies.Core
    {
        /// <summary>Application-layer inbound DTO.</summary>
        public interface IApplicationRequestDTO
        {
            int Id { get; set; }
        }

        /// <summary>Application-layer outbound DTO.</summary>
        public interface IApplicationResponseDTO
        {
            int Id { get; set; }
            string Name { get; set; }
            string Name2 { get; set; }
        }

        /// <summary>Infrastructure-layer inbound DTO.</summary>
        public interface IPersistenceRequestDTO
        {
            int Id { get; set; }
            string Name { get; set; }
            string Name2 { get; set; }
        }

        /// <summary>Infrastructure-layer outbound DTO.</summary>
        public interface IPersistenceResponseDTO
        {
            int Id { get; set; }
            string Name { get; set; }
            string Name2 { get; set; }
        }
    }
}
