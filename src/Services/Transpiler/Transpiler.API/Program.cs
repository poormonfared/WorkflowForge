using BuildingBlocks.Behaviors;
using BuildingBlocks.Exceptions.Handler;
using Carter;
using FluentValidation;
using Transpiler.Adapters.N8n;
using Transpiler.API.Infrastructure;
using Transpiler.CodeGen.CSharp;
using Transpiler.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarter();

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
app.MapCarter();

app.Run();

// Exposes the implicit top-level-statements Program class for Transpiler.API.Tests' WebApplicationFactory<Program>.
public partial class Program;
