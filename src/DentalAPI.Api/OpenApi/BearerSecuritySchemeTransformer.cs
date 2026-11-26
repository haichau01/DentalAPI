using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace DentalAPI.Api.OpenApi;

/// <summary>Declares the JWT bearer scheme in the OpenAPI document so clients know to send "Authorization: Bearer".</summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeId = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Access token from /api/v1/auth/staff/login or /api/v1/auth/patients/login.",
        };

        var schemeReference = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SchemeId },
        };
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement { [schemeReference] = [] });

        return Task.CompletedTask;
    }
}
