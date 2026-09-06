using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("[START] {Request}", typeof(TRequest).Name);

        var timer = Stopwatch.StartNew();
        var response = await next();
        timer.Stop();

        if (timer.Elapsed.TotalSeconds > 3)
        {
            logger.LogWarning("[PERFORMANCE] {Request} took {Elapsed}s", typeof(TRequest).Name, timer.Elapsed.TotalSeconds);
        }

        logger.LogInformation("[END] {Request}", typeof(TRequest).Name);
        return response;
    }
}
