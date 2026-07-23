using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Dialitech.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling {RequestName} at {Time}", requestName, DateTime.UtcNow);

        var response = await next();

        stopwatch.Stop();
        _logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms - {@Response}",
            requestName, stopwatch.ElapsedMilliseconds, response);

        return response;
    }
}
