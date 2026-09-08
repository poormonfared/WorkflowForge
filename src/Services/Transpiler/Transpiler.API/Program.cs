using BuildingBlocks.Behaviors;
using BuildingBlocks.Exceptions.Handler;
using Carter;
using FluentValidation;
using Scalar.AspNetCore;
using Transpiler.Adapters.N8n;
using Transpiler.API.Infrastructure;
using Transpiler.CodeGen.CSharp;
using Transpiler.Core;

var builder = WebApplication.CreateBuilder(args);

var gatewayRoutePrefix = builder.Configuration["Gateway:RoutePrefix"]!;
var openApiRoutePattern = $"{gatewayRoutePrefix}/openapi/{{documentName}}.json";

builder.Services.AddCarter();
builder.Services.AddOpenApi(options =>
{
    // Relative to wherever the document itself was fetched from — through the gateway that's
    // /transpiler-service/..., so "Try it" requests stay routed through the gateway too instead
    // of going straight to this API's own address.
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Servers = [new Microsoft.OpenApi.OpenApiServer { Url = gatewayRoutePrefix }];
        return Task.CompletedTask;
    });
});

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<Program>();
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IWorkflowSessionStore, InMemoryWorkflowSessionStore>();

// Adapter-pattern boundary (see Transpiler.Core.IWorkflowSourceParser / IWorkflowCodeGenerator):
// this is the only place either concrete implementation is referenced. A future
// Transpiler.Adapters.ActivePieces would be registered here too, with zero changes below.
builder.Services.AddScoped<IWorkflowSourceParser, N8nWorkflowParser>();
builder.Services.AddSingleton<IWorkflowCodeGenerator, CSharpProjectGenerator>();

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

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

app.MapCarter();

app.Run();

// Exposes the implicit top-level-statements Program class for Transpiler.API.Tests' WebApplicationFactory<Program>.
public partial class Program;
