using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.PermitLimit = 5;
    });
});

var app = builder.Build();

var docsUrl = builder.Configuration["Gateway:DocsUrl"]!;

app.UseRateLimiter();
app.MapGet("/", () => Results.Redirect(docsUrl));
app.MapReverseProxy();

app.Run();
