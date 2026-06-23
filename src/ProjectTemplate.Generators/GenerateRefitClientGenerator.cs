using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ProjectTemplate.Generators;

[Generator]
public sealed class GenerateRefitClientGenerator : IIncrementalGenerator
{
    private sealed class ClientEndpoint
    {
        public string RootNamespace { get; }
        public string ClientNamespace { get; }
        public string DomainName { get; }
        public string FeatureName { get; }
        public string MethodName { get; }
        public string HttpAttribute { get; }
        public string Route { get; }
        public IReadOnlyList<ClientProperty> RequestProperties { get; }
        public IReadOnlyList<ClientProperty> ResponseProperties { get; }
        public IReadOnlyList<ClientParameter> RouteParameters { get; }
        public bool HasBody { get; }

        public ClientEndpoint(
            string rootNamespace,
            string clientNamespace,
            string domainName,
            string featureName,
            string methodName,
            string httpAttribute,
            string route,
            IReadOnlyList<ClientProperty> requestProperties,
            IReadOnlyList<ClientProperty> responseProperties,
            IReadOnlyList<ClientParameter> routeParameters,
            bool hasBody)
        {
            RootNamespace = rootNamespace;
            ClientNamespace = clientNamespace;
            DomainName = domainName;
            FeatureName = featureName;
            MethodName = methodName;
            HttpAttribute = httpAttribute;
            Route = route;
            RequestProperties = requestProperties;
            ResponseProperties = responseProperties;
            RouteParameters = routeParameters;
            HasBody = hasBody;
        }
    }

    private sealed class ClientProperty
    {
        public string Name { get; }
        public string TypeName { get; }
        public bool RequiresInitializer { get; }
        public bool IsString { get; }

        public ClientProperty(string name, string typeName, bool requiresInitializer, bool isString)
        {
            Name = name;
            TypeName = typeName;
            RequiresInitializer = requiresInitializer;
            IsString = isString;
        }
    }

    private sealed class ClientParameter
    {
        public string Name { get; }
        public string TypeName { get; }
        public bool IsValueType { get; }

        public ClientParameter(string name, string typeName, bool isValueType)
        {
            Name = name;
            TypeName = typeName;
            IsValueType = isValueType;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var endpoints = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (syntaxContext, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var endpointClass = (ClassDeclarationSyntax)syntaxContext.Node;
                    if (!endpointClass.Identifier.Text.EndsWith("Endpoint", StringComparison.Ordinal))
                        return null;

                    var endpointSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(endpointClass, cancellationToken) as INamedTypeSymbol;
                    if (endpointSymbol?.ContainingType?.ContainingType is not INamedTypeSymbol featureSymbol)
                        return null;

                    if (!featureSymbol.GetAttributes().Any(static a => a.AttributeClass?.Name == "FeatureAttribute"))
                        return null;

                    var targetNamespace = GetTargetNamespace(featureSymbol.ContainingNamespace);
                    if (string.IsNullOrWhiteSpace(targetNamespace))
                        return null;

                    var mapInvocation = endpointClass.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .FirstOrDefault(IsHttpMapInvocation);
                    if (mapInvocation is null)
                        return null;

                    var methodName = GetMemberName(mapInvocation.Expression);
                    var httpAttribute = methodName.Substring("Map".Length);
                    var route = GetStringArgument(mapInvocation.ArgumentList.Arguments.FirstOrDefault());
                    if (route is null || string.IsNullOrWhiteSpace(route))
                        return null;

                    var lambda = mapInvocation.ArgumentList.Arguments
                        .Select(static arg => arg.Expression)
                        .OfType<ParenthesizedLambdaExpressionSyntax>()
                        .FirstOrDefault();
                    if (lambda is null)
                        return null;

                    var routeTokens = GetRouteTokens(route);
                    var routeParameters = GetRouteParameters(lambda, routeTokens, syntaxContext.SemanticModel, cancellationToken);
                    var hasBody = HasBodyParameter(lambda, routeTokens);

                    var generatedMethodName = GetEndpointName(endpointClass) ?? featureSymbol.Name;
                    var (rootNamespace, clientNamespace, domainName) = GetClientNames(targetNamespace);

                    var requestInterface = featureSymbol.GetTypeMembers($"I{featureSymbol.Name}Request").FirstOrDefault();
                    var responseInterface = featureSymbol.GetTypeMembers($"I{featureSymbol.Name}Response").FirstOrDefault();

                    return new ClientEndpoint(
                        rootNamespace,
                        clientNamespace,
                        domainName,
                        featureSymbol.Name,
                        generatedMethodName,
                        httpAttribute,
                        NormalizeRefitRoute(route),
                        requestInterface is null ? new List<ClientProperty>() : GetProperties(requestInterface).Select(ToClientProperty).ToList(),
                        responseInterface is null ? new List<ClientProperty>() : GetProperties(responseInterface).Select(ToClientProperty).ToList(),
                        routeParameters,
                        hasBody);
                })
            .Where(static endpoint => endpoint is not null);

        context.RegisterSourceOutput(endpoints.Collect(), static (productionContext, source) =>
        {
            var groupedByProject = source
                .OfType<ClientEndpoint>()
                .GroupBy(static endpoint => endpoint.ClientNamespace, StringComparer.Ordinal);

            foreach (var projectEndpoints in groupedByProject)
            {
                var endpointsForProject = projectEndpoints
                    .GroupBy(static endpoint => endpoint.DomainName + "|" + endpoint.MethodName + "|" + endpoint.HttpAttribute + "|" + endpoint.Route, StringComparer.Ordinal)
                    .Select(static group => group.First())
                    .OrderBy(static endpoint => endpoint.DomainName, StringComparer.Ordinal)
                    .ThenBy(static endpoint => endpoint.MethodName, StringComparer.Ordinal)
                    .ToList();

                if (endpointsForProject.Count == 0)
                    continue;

                var sourceText = BuildClientSource(projectEndpoints.Key, endpointsForProject);
                productionContext.AddSource(
                    $"{projectEndpoints.Key}.RefitClients.g.cs",
                    SourceText.From(sourceText, Encoding.UTF8));
            }
        });
    }

    private static string BuildClientSource(string clientNamespace, IReadOnlyList<ClientEndpoint> endpoints)
    {
        var rootNamespace = endpoints[0].RootNamespace;
        var wrapperName = GetWrapperName(rootNamespace);
        var domains = endpoints
            .GroupBy(static endpoint => endpoint.DomainName, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Refit;");
        sb.AppendLine();
        sb.AppendLine($"namespace {clientNamespace};");
        sb.AppendLine();

        AppendException(sb, wrapperName);
        AppendOptions(sb, wrapperName);
        AppendAuthenticationHandler(sb, wrapperName);

        foreach (var endpoint in endpoints)
        {
            AppendContracts(sb, endpoint);
        }

        foreach (var domain in domains)
        {
            AppendRefitApi(sb, domain.Key, domain.ToList());
            AppendDomainClient(sb, domain.Key, domain.ToList());
            foreach (var endpoint in domain)
            {
                AppendBuilder(sb, endpoint);
            }
        }

        AppendWrapper(sb, wrapperName, domains.Select(static group => group.Key).ToList());
        AppendServiceCollectionExtensions(sb, wrapperName, domains.Select(static group => group.Key).ToList());

        return sb.ToString();
    }

    private static void AppendException(StringBuilder sb, string wrapperName)
    {
        sb.AppendLine($"public sealed class {wrapperName}ApiException : global::System.Exception");
        sb.AppendLine("{");
        sb.AppendLine("    public global::System.Net.HttpStatusCode StatusCode { get; }");
        sb.AppendLine();
        sb.AppendLine($"    public {wrapperName}ApiException(global::System.Net.HttpStatusCode statusCode, string message)");
        sb.AppendLine("        : base(message)");
        sb.AppendLine("    {");
        sb.AppendLine("        StatusCode = statusCode;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendOptions(StringBuilder sb, string wrapperName)
    {
        sb.AppendLine($"public sealed class {wrapperName}Options");
        sb.AppendLine("{");
        sb.AppendLine("    public global::System.Func<global::System.Net.Http.HttpRequestMessage, global::System.Threading.CancellationToken, global::System.Threading.Tasks.ValueTask<string?>>? BearerTokenProvider { get; set; }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendAuthenticationHandler(StringBuilder sb, string wrapperName)
    {
        sb.AppendLine($"internal sealed class {wrapperName}AuthenticationHandler : global::System.Net.Http.DelegatingHandler");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {wrapperName}Options _options;");
        sb.AppendLine();
        sb.AppendLine($"    public {wrapperName}AuthenticationHandler({wrapperName}Options options)");
        sb.AppendLine("    {");
        sb.AppendLine("        _options = options;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    protected override async global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> SendAsync(");
        sb.AppendLine("        global::System.Net.Http.HttpRequestMessage request,");
        sb.AppendLine("        global::System.Threading.CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_options.BearerTokenProvider is not null)");
        sb.AppendLine("        {");
        sb.AppendLine("            var token = await _options.BearerTokenProvider(request, cancellationToken);");
        sb.AppendLine("            if (!string.IsNullOrWhiteSpace(token))");
        sb.AppendLine("            {");
        sb.AppendLine("                request.Headers.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue(\"Bearer\", token);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return await base.SendAsync(request, cancellationToken);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendContracts(StringBuilder sb, ClientEndpoint endpoint)
    {
        AppendInterface(sb, $"I{endpoint.FeatureName}Request", endpoint.RequestProperties);
        AppendDto(sb, $"{endpoint.FeatureName}Request", $"I{endpoint.FeatureName}Request", endpoint.RequestProperties);
        AppendInterface(sb, $"I{endpoint.FeatureName}Response", endpoint.ResponseProperties);
        AppendDto(sb, $"{endpoint.FeatureName}Response", $"I{endpoint.FeatureName}Response", endpoint.ResponseProperties);
    }

    private static void AppendInterface(StringBuilder sb, string name, IReadOnlyList<ClientProperty> properties)
    {
        sb.AppendLine($"public interface {name}");
        sb.AppendLine("{");
        foreach (var property in properties)
        {
            sb.AppendLine($"    {property.TypeName} {property.Name} {{ get; set; }}");
        }
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendDto(StringBuilder sb, string name, string interfaceName, IReadOnlyList<ClientProperty> properties)
    {
        sb.AppendLine($"public sealed class {name} : {interfaceName}");
        sb.AppendLine("{");
        foreach (var property in properties)
        {
            sb.Append($"    public {property.TypeName} {property.Name} {{ get; set; }}");
            if (property.RequiresInitializer)
            {
                sb.Append(property.IsString ? " = string.Empty;" : " = default;");
            }
            sb.AppendLine();
        }
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendRefitApi(StringBuilder sb, string domainName, IReadOnlyList<ClientEndpoint> endpoints)
    {
        sb.AppendLine($"public interface I{domainName}Api");
        sb.AppendLine("{");
        foreach (var endpoint in endpoints)
        {
            sb.AppendLine($"    [global::Refit.{endpoint.HttpAttribute}(\"{EscapeString(endpoint.Route)}\")]");
            sb.Append($"    global::System.Threading.Tasks.Task<global::Refit.ApiResponse<{endpoint.FeatureName}Response>> {endpoint.MethodName}Async(");
            var parameters = new List<string>();
            parameters.AddRange(endpoint.RouteParameters.Select(static parameter => $"{parameter.TypeName} {ToCamelCase(parameter.Name)}"));
            if (endpoint.HasBody)
            {
                parameters.Add($"[global::Refit.Body] {endpoint.FeatureName}Request request");
            }
            parameters.Add("global::System.Threading.CancellationToken cancellationToken = default");
            sb.Append(string.Join(", ", parameters));
            sb.AppendLine(");");
            sb.AppendLine();
        }
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendDomainClient(StringBuilder sb, string domainName, IReadOnlyList<ClientEndpoint> endpoints)
    {
        sb.AppendLine($"public sealed class {domainName}Client");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly I{domainName}Api _api;");
        sb.AppendLine();
        sb.AppendLine($"    public {domainName}Client(I{domainName}Api api)");
        sb.AppendLine("    {");
        sb.AppendLine("        _api = api;");
        sb.AppendLine("    }");
        sb.AppendLine();
        foreach (var endpoint in endpoints)
        {
            sb.AppendLine($"    public {endpoint.MethodName}RequestBuilder {endpoint.MethodName}()");
            sb.AppendLine($"        => new {endpoint.MethodName}RequestBuilder(_api);");
            sb.AppendLine();
        }
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendBuilder(StringBuilder sb, ClientEndpoint endpoint)
    {
        sb.AppendLine($"public sealed class {endpoint.MethodName}RequestBuilder");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly I{endpoint.DomainName}Api _api;");
        foreach (var parameter in endpoint.RouteParameters)
        {
            sb.AppendLine($"    private {parameter.TypeName}{(parameter.IsValueType ? "?" : string.Empty)} _{ToCamelCase(parameter.Name)};");
        }
        if (endpoint.HasBody)
        {
            sb.AppendLine($"    private readonly {endpoint.FeatureName}Request _request = new();");
        }
        sb.AppendLine();
        sb.AppendLine($"    public {endpoint.MethodName}RequestBuilder(I{endpoint.DomainName}Api api)");
        sb.AppendLine("    {");
        sb.AppendLine("        _api = api;");
        sb.AppendLine("    }");
        sb.AppendLine();
        foreach (var parameter in endpoint.RouteParameters)
        {
            sb.AppendLine($"    public {endpoint.MethodName}RequestBuilder With{parameter.Name}({parameter.TypeName} value)");
            sb.AppendLine("    {");
            sb.AppendLine($"        _{ToCamelCase(parameter.Name)} = value;");
            sb.AppendLine("        return this;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        if (endpoint.HasBody)
        {
            var routeParamNames = new HashSet<string>(
                endpoint.RouteParameters.Select(static p => p.Name),
                StringComparer.OrdinalIgnoreCase);

            foreach (var property in endpoint.RequestProperties)
            {
                if (routeParamNames.Contains(property.Name))
                    continue;

                sb.AppendLine($"    public {endpoint.MethodName}RequestBuilder With{property.Name}({property.TypeName} value)");
                sb.AppendLine("    {");
                sb.AppendLine($"        _request.{property.Name} = value;");
                sb.AppendLine("        return this;");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }
        sb.AppendLine($"    public async global::System.Threading.Tasks.Task<{endpoint.FeatureName}Response> Send(");
        sb.AppendLine("        global::System.Threading.CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        foreach (var parameter in endpoint.RouteParameters)
        {
            var fieldName = "_" + ToCamelCase(parameter.Name);
            sb.AppendLine($"        if ({fieldName} is null)");
            sb.AppendLine("        {");
            sb.AppendLine($"            throw new global::System.InvalidOperationException(\"The {parameter.Name} route value is required before sending the request.\");");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        sb.Append("        var response = await _api.");
        sb.Append(endpoint.MethodName);
        sb.Append("Async(");
        var arguments = new List<string>();
        arguments.AddRange(endpoint.RouteParameters.Select(static parameter => "_" + ToCamelCase(parameter.Name) + (parameter.IsValueType ? ".Value" : string.Empty)));
        if (endpoint.HasBody)
        {
            arguments.Add("_request");
        }
        arguments.Add("cancellationToken");
        sb.Append(string.Join(", ", arguments));
        sb.AppendLine(");");
        sb.AppendLine();
        sb.AppendLine("        if (!response.IsSuccessStatusCode)");
        sb.AppendLine("        {");
        sb.AppendLine($"            throw new {GetWrapperName(endpoint.RootNamespace)}ApiException(response.StatusCode ?? global::System.Net.HttpStatusCode.InternalServerError, response.Error?.Message ?? response.ReasonPhrase ?? \"Unexpected API failure\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return response.Content ?? throw new global::System.InvalidOperationException(\"The API response did not contain a response body.\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendWrapper(StringBuilder sb, string wrapperName, IReadOnlyList<string> domainNames)
    {
        sb.AppendLine($"public sealed class {wrapperName}");
        sb.AppendLine("{");
        sb.Append($"    public {wrapperName}(");
        sb.Append(string.Join(", ", domainNames.Select(static domain => $"{domain}Client {ToCamelCase(domain)}")));
        sb.AppendLine(")");
        sb.AppendLine("    {");
        foreach (var domainName in domainNames)
        {
            sb.AppendLine($"        {domainName} = {ToCamelCase(domainName)};");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
        foreach (var domainName in domainNames)
        {
            sb.AppendLine($"    public {domainName}Client {domainName} {{ get; }}");
        }
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void AppendServiceCollectionExtensions(StringBuilder sb, string wrapperName, IReadOnlyList<string> domainNames)
    {
        sb.AppendLine($"public static class {wrapperName}ServiceCollectionExtensions");
        sb.AppendLine("{");
        sb.AppendLine($"    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection Add{wrapperName}(");
        sb.AppendLine("        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,");
        sb.AppendLine("        global::System.Uri baseUri,");
        sb.AppendLine($"        global::System.Action<{wrapperName}Options>? configureClient = null,");
        sb.AppendLine("        global::Refit.RefitSettings? refitSettings = null)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var options = new {wrapperName}Options();");
        sb.AppendLine("        configureClient?.Invoke(options);");
        sb.AppendLine($"        services.AddSingleton(options);");
        sb.AppendLine($"        services.AddTransient<{wrapperName}AuthenticationHandler>();");
        foreach (var domainName in domainNames)
        {
            sb.AppendLine($"        services.AddRefitClient<I{domainName}Api>(refitSettings)");
            sb.AppendLine("            .ConfigureHttpClient(client => client.BaseAddress = baseUri)");
            sb.AppendLine($"            .AddHttpMessageHandler<{wrapperName}AuthenticationHandler>();");
            sb.AppendLine($"        services.AddScoped<{domainName}Client>();");
        }
        sb.AppendLine($"        services.AddScoped<{wrapperName}>();");
        sb.AppendLine();
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    private static bool IsHttpMapInvocation(InvocationExpressionSyntax invocation)
    {
        var memberName = GetMemberName(invocation.Expression);
        switch (memberName)
        {
            case "MapGet":
            case "MapPost":
            case "MapPut":
            case "MapPatch":
            case "MapDelete":
                return true;
            default:
                return false;
        }
    }

    private static string GetMemberName(ExpressionSyntax expression)
        => expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.Identifier.Text
            : string.Empty;

    private static string? GetStringArgument(ArgumentSyntax? argument)
        => argument?.Expression is LiteralExpressionSyntax literal && literal.Token.ValueText.Length > 0
            ? literal.Token.ValueText
            : null;

    private static string? GetEndpointName(ClassDeclarationSyntax endpointClass)
        => endpointClass.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(static invocation => GetMemberName(invocation.Expression) == "WithName")
            .Select(static invocation => GetStringArgument(invocation.ArgumentList.Arguments.FirstOrDefault()))
            .FirstOrDefault(static name => !string.IsNullOrWhiteSpace(name));

    private static IReadOnlyList<string> GetRouteTokens(string route)
    {
        var tokens = new List<string>();
        for (var i = 0; i < route.Length; i++)
        {
            if (route[i] != '{')
                continue;

            var end = route.IndexOf('}', i + 1);
            if (end < 0)
                break;

            var token = route.Substring(i + 1, end - i - 1);
            var delimiterIndex = token.IndexOfAny(new[] { ':', '=', '?' });
            if (delimiterIndex >= 0)
                token = token.Substring(0, delimiterIndex);
            if (!string.IsNullOrWhiteSpace(token))
                tokens.Add(token);
            i = end;
        }

        return tokens;
    }

    private static string NormalizeRefitRoute(string route)
    {
        var normalized = route;
        foreach (var token in GetRouteTokens(route))
        {
            var start = normalized.IndexOf("{" + token, StringComparison.Ordinal);
            if (start < 0)
                continue;

            var end = normalized.IndexOf('}', start + 1);
            if (end < 0)
                continue;

            normalized = normalized.Substring(0, start) + "{" + token + "}" + normalized.Substring(end + 1);
        }

        return normalized;
    }

    private static List<ClientParameter> GetRouteParameters(
        ParenthesizedLambdaExpressionSyntax lambda,
        IReadOnlyList<string> routeTokens,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        var parameters = new List<ClientParameter>();
        foreach (var parameter in lambda.ParameterList.Parameters)
        {
            if (!routeTokens.Any(token => string.Equals(token, parameter.Identifier.Text, StringComparison.OrdinalIgnoreCase)))
                continue;

            var typeInfo = semanticModel.GetTypeInfo(parameter.Type!, cancellationToken).Type;
            var typeName = typeInfo is null
                ? parameter.Type?.ToString() ?? "object"
                : typeInfo.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            parameters.Add(new ClientParameter(ToPascalCase(parameter.Identifier.Text), typeName, typeInfo?.IsValueType ?? false));
        }

        return parameters;
    }

    private static bool HasBodyParameter(ParenthesizedLambdaExpressionSyntax lambda, IReadOnlyList<string> routeTokens)
    {
        foreach (var parameter in lambda.ParameterList.Parameters)
        {
            var name = parameter.Identifier.Text;
            var typeName = parameter.Type?.ToString() ?? string.Empty;
            if (string.Equals(typeName, "IMediator", StringComparison.Ordinal) ||
                string.Equals(typeName, "CancellationToken", StringComparison.Ordinal) ||
                typeName.EndsWith(".IMediator", StringComparison.Ordinal) ||
                typeName.EndsWith(".CancellationToken", StringComparison.Ordinal) ||
                routeTokens.Any(token => string.Equals(token, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static ClientProperty ToClientProperty(IPropertySymbol property)
    {
        var typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new ClientProperty(
            property.Name,
            typeName,
            property.Type.IsReferenceType && property.Type.NullableAnnotation != NullableAnnotation.Annotated,
            property.Type.SpecialType == SpecialType.System_String);
    }

    private static IEnumerable<IPropertySymbol> GetProperties(INamedTypeSymbol interfaceSymbol)
        => interfaceSymbol.AllInterfaces
            .Concat(new[] { interfaceSymbol })
            .SelectMany(static type => type.GetMembers().OfType<IPropertySymbol>())
            .GroupBy(static property => property.Name, StringComparer.Ordinal)
            .Select(static group => group.First());

    private static (string RootNamespace, string ClientNamespace, string DomainName) GetClientNames(string targetNamespace)
    {
        const string domainsSegment = ".Domains.";
        var domainsIndex = targetNamespace.IndexOf(domainsSegment, StringComparison.Ordinal);
        if (domainsIndex >= 0)
        {
            var rootNamespace = targetNamespace.Substring(0, domainsIndex);
            var domainPart = targetNamespace.Substring(domainsIndex + domainsSegment.Length);
            var domainName = domainPart.Split('.')[0];
            return (rootNamespace, rootNamespace + ".Client", domainName);
        }

        var parts = targetNamespace.Split('.');
        var root = parts.Length > 1 ? string.Join(".", parts.Take(parts.Length - 1)) : targetNamespace;
        return (root, root + ".Client", parts[parts.Length - 1]);
    }

    private static string GetWrapperName(string rootNamespace)
    {
        var parts = rootNamespace.Split('.');
        return ToPascalCase(parts[parts.Length - 1]) + "Client";
    }

    private static string GetTargetNamespace(INamespaceSymbol namespaceSymbol)
        => namespaceSymbol.IsGlobalNamespace ? string.Empty : namespaceSymbol.ToDisplayString();

    private static string ToPascalCase(string value)
        => string.IsNullOrEmpty(value) || char.IsUpper(value[0])
            ? value
            : char.ToUpperInvariant(value[0]) + value.Substring(1);

    private static string ToCamelCase(string value)
        => string.IsNullOrEmpty(value) || char.IsLower(value[0])
            ? value
            : char.ToLowerInvariant(value[0]) + value.Substring(1);

    private static string EscapeString(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
