using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ConferenceRoomBooking.Api.Security;

// Describes the admin API key in the OpenAPI document, so Swagger UI shows the "Authorize" button
public class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "AdminApiKey";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = AdminApiKeyAttribute.HeaderName,
            Description = "Admin API key. Required for managing rooms and for reports."
        };

        // Once the key is entered in Swagger UI, it is sent with every request
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
        });

        return Task.CompletedTask;
    }
}