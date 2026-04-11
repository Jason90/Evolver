using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Evolver.Shared.Api;

namespace Evolver.Web.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";

            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var message = environment.IsDevelopment()
                ? ex.Message
                : "服务器内部错误，请稍后重试。";

            var body = ApiEnvelope.Fail("internal_error", message, traceId: traceId);
            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }
    }
}
