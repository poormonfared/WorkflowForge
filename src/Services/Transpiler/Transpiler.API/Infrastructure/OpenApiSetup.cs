using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Transpiler.API.Infrastructure;

public static class OpenApiSetup
{
    public static WebApplicationBuilder AddTranspilerOpenApi(this WebApplicationBuilder builder)
    {
        var gatewayRoutePrefix = builder.Configuration["Gateway:RoutePrefix"]!;

        builder.Services.AddOpenApi(options =>
        {
            // Relative to wherever the document itself was fetched from — through the gateway that's
            // /transpiler-service/..., so "Try it" requests stay routed through the gateway too instead
            // of going straight to this API's own address.
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Servers = [new OpenApiServer { Url = gatewayRoutePrefix }];
                return Task.CompletedTask;
            });
        });

        return builder;
    }

    public static WebApplication MapTranspilerOpenApi(this WebApplication app)
    {
        var gatewayRoutePrefix = app.Configuration["Gateway:RoutePrefix"]!;
        var openApiRoutePattern = $"{gatewayRoutePrefix}/openapi/{{documentName}}.json";

        // Mounted at the gateway-facing path (not /openapi, /scalar) — WorkflowForge.Gateway forwards
        // /transpiler-service/{scalar,openapi}/* here unchanged, and Scalar's generated links need to
        // match what the browser actually sees through the gateway. Transpiler.API isn't meant to be
        // browsed to directly.
        app.MapOpenApi(openApiRoutePattern);
        app.MapScalarApiReference($"{gatewayRoutePrefix}/scalar", options =>
        {
            options.WithTitle("Transpiler API");
            options.WithOpenApiRoutePattern(openApiRoutePattern);
        });

        return app;
    }
}
