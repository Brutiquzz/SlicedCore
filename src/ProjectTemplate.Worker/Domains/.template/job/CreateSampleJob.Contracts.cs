namespace ProjectTemplate.Worker.Domains.Sample;

public partial class CreateSampleJob
{
    /// <summary>Inbound job request contract passed to the mediator by the job runner.</summary>
    public interface ICreateSampleJobRequest
    {
        [PiiData] string Name { get; set; }
        [PiiData] string Name2 { get; set; }
        [PublicData] string ContextId { get; set; }
    }

    /// <summary>Outbound response contract returned on success.</summary>
    public interface ICreateSampleJobResponse
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
    }

    /// <summary>Contracts for the <c>CreateSampleJob</c> worker feature shared across all layers.</summary>
    protected sealed partial class Core : Dependencies.Core
    {
        /// <summary>Application-layer inbound DTO forwarded from the presentation layer; names include the appended context ID.</summary>
        public interface IApplicationRequestDTO
        {
            [PiiData] string Name { get; set; }
            [PiiData] string Name2 { get; set; }
            [PublicData] string ContextId { get; set; }
        }

        /// <summary>Application-layer outbound DTO returned to the presentation layer.</summary>
        public interface IApplicationResponseDTO
        {
            int Id { get; set; }
            string Name { get; set; }
            string Name2 { get; set; }
        }

        /// <summary>Infrastructure-layer inbound DTO forwarded from the application layer.</summary>
        public interface IPersistenceRequestDTO
        {
            [PiiData] string Name { get; set; }
            [PiiData] string Name2 { get; set; }
        }

        /// <summary>Infrastructure-layer outbound DTO returned after the remote create call.</summary>
        public interface IPersistenceResponseDTO
        {
            int Id { get; set; }
            string Name { get; set; }
            string Name2 { get; set; }
        }
    }
}
