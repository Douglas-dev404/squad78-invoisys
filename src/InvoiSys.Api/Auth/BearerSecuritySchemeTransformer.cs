using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InvoiSys.Api.Auth;

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider esquemas)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument documento,
        OpenApiDocumentTransformerContext contexto,
        CancellationToken cancellationToken)
    {
        var existentes = await esquemas.GetAllSchemesAsync();
        if (!existentes.Any(e => e.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            return;
        }

        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        };

        documento.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", documento)] = [],
            },
        ];
    }
}