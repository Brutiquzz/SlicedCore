namespace ProjectTemplate.Generators.Tests;

public class GenerateRefitClientGeneratorTests
{
    private const string ApiSource = """
        using ProjectTemplate.Dependencies.Attributes;
        using System.Threading;

        namespace MyApp.Domains.Order;

        public interface IEndpoint
        {
            void MapEndpoint(IEndpointRouteBuilder builder);
        }

        public interface IEndpointRouteBuilder { }
        public interface IMediator { }

        [Feature(FeatureType.Query)]
        public partial class GetOrder
        {
            public interface IGetOrderRequest { int Id { get; set; } }
            public interface IGetOrderResponse { int Id { get; set; } string Name { get; set; } }

            partial class PresentationLayer
            {
                private sealed class GetOrderFeatureEndpoint : IEndpoint
                {
                    public void MapEndpoint(IEndpointRouteBuilder builder)
                    {
                        builder.MapGet("/orders/{id:int}", async (int id, IMediator mediator, CancellationToken cancellationToken) => id)
                            .WithName("GetOrder");
                    }
                }
            }
        }

        [Feature(FeatureType.Command)]
        public partial class CreateOrder
        {
            public interface ICreateOrderRequest { string Name { get; set; } }
            public interface ICreateOrderResponse { int Id { get; set; } string Name { get; set; } }

            partial class PresentationLayer
            {
                private sealed class CreateOrderFeatureEndpoint : IEndpoint
                {
                    public void MapEndpoint(IEndpointRouteBuilder builder)
                    {
                        builder.MapPost("/orders", async (CreateOrderRequest request, IMediator mediator, CancellationToken cancellationToken) => request)
                            .WithName("CreateOrder");
                    }
                }
            }
        }

        [Feature(FeatureType.Command)]
        public partial class UpdateOrder
        {
            public interface IUpdateOrderRequest { int Id { get; set; } string Name { get; set; } }
            public interface IUpdateOrderResponse { int Id { get; set; } string Name { get; set; } }

            partial class PresentationLayer
            {
                private sealed class UpdateOrderFeatureEndpoint : IEndpoint
                {
                    public void MapEndpoint(IEndpointRouteBuilder builder)
                    {
                        builder.MapPut("/orders/{id:int}", async (int id, UpdateOrderRequest request, IMediator mediator, CancellationToken cancellationToken) => { request.Id = id; return request; })
                            .WithName("UpdateOrder");
                    }
                }
            }
        }
        """;

    [Test]
    public async Task GeneratesOneClientSourceFile()
    {
        var result = GeneratorTestHelper.Run<GenerateRefitClientGenerator>(ApiSource);

        await Assert.That(result.GeneratedSources).Count().IsEqualTo(1);
        await Assert.That(result.GeneratedSources[0].HintName).IsEqualTo("MyApp.Client.RefitClients.g.cs");
    }

    [Test]
    public async Task GeneratedSourceContainsRefitApiGroupedByDomain()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateRefitClientGenerator>(ApiSource));

        var source = sources["MyApp.Client.RefitClients.g.cs"];

        await Assert.That(source).Contains("public interface IOrderApi");
        await Assert.That(source).Contains("[global::Refit.Get(\"/orders/{id}\")]");
        await Assert.That(source).Contains("[global::Refit.Post(\"/orders\")]");
        await Assert.That(source).Contains("GetOrderAsync(");
        await Assert.That(source).Contains("[global::Refit.Body] CreateOrderRequest request");
    }

    [Test]
    public async Task GeneratedSourceDuplicatesPresentationContracts()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateRefitClientGenerator>(ApiSource));

        var source = sources["MyApp.Client.RefitClients.g.cs"];

        await Assert.That(source).Contains("public interface ICreateOrderRequest");
        await Assert.That(source).Contains("public sealed class CreateOrderRequest : ICreateOrderRequest");
        await Assert.That(source).Contains("public interface IGetOrderResponse");
        await Assert.That(source).Contains("public sealed class GetOrderResponse : IGetOrderResponse");
    }

    [Test]
    public async Task GeneratedSourceContainsBuilderStyleDomainClient()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateRefitClientGenerator>(ApiSource));

        var source = sources["MyApp.Client.RefitClients.g.cs"];

        await Assert.That(source).Contains("public sealed class OrderClient");
        await Assert.That(source).Contains("public GetOrderRequestBuilder GetOrder()");
        await Assert.That(source).Contains("public GetOrderRequestBuilder GetOrder(int id)");
        await Assert.That(source).Contains("public GetOrderRequestBuilder WithId(");
        await Assert.That(source).Contains("public async global::System.Threading.Tasks.Task<GetOrderResponse> Send(");
    }

    [Test]
    public async Task GeneratedSourceContainsProjectWrapperAndRegistration()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateRefitClientGenerator>(ApiSource));

        var source = sources["MyApp.Client.RefitClients.g.cs"];

        await Assert.That(source).Contains("public sealed class MyAppClient");
        await Assert.That(source).Contains("public OrderClient Order { get; }");
        await Assert.That(source).Contains("public static class MyAppClientServiceCollectionExtensions");
        await Assert.That(source).Contains("AddMyAppClient(");
        await Assert.That(source).Contains("BearerTokenProvider");
        await Assert.That(source).Contains("public sealed class MyAppClientApiException");
    }

    [Test]
    public async Task BuilderDoesNotGenerateDuplicateWithMethodWhenRouteParamAndBodyPropertyShareName()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateRefitClientGenerator>(ApiSource));

        var source = sources["MyApp.Client.RefitClients.g.cs"];

        // UpdateOrder has route {id} AND body property Id — WithId should only appear once
        var builderStart = source.IndexOf("public sealed class UpdateOrderRequestBuilder", StringComparison.Ordinal);
        await Assert.That(builderStart).IsGreaterThanOrEqualTo(0);

        var builderSection = source.Substring(builderStart);
        var nextClass = builderSection.IndexOf("public sealed class ", 1, StringComparison.Ordinal);
        if (nextClass > 0)
            builderSection = builderSection.Substring(0, nextClass);

        var withIdCount = 0;
        var search = "public UpdateOrderRequestBuilder WithId(";
        var pos = 0;
        while ((pos = builderSection.IndexOf(search, pos, StringComparison.Ordinal)) >= 0)
        {
            withIdCount++;
            pos += search.Length;
        }

        await Assert.That(withIdCount).IsEqualTo(1);
    }
}
