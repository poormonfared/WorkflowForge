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

app.UseRateLimiter();
app.MapGet("/", () => Results.Redirect("/transpiler-service/scalar"));
app.MapReverseProxy();

app.Run();
