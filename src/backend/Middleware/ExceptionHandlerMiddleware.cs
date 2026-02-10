using System.Net;
using System.Text.Json;

using backend.DTOs;

using Grpc.Core;

namespace backend.Middleware;

public class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Request was cancelled by the client.");
            if (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, new ApiException("REQUEST_CANCELLED", "Request was cancelled by the client", (HttpStatusCode)499));
            }
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
        {
            logger.LogInformation("gRPC call was cancelled.");
            if (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, new ApiException("GRPC_CANCELLED", "gRPC call was cancelled", (HttpStatusCode)499));
            }
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "gRPC error occurred.");
            if (!context.Response.HasStarted)
            {
                var statusCode = ex.StatusCode switch
                {
                    Grpc.Core.StatusCode.Unavailable => HttpStatusCode.ServiceUnavailable,
                    Grpc.Core.StatusCode.DeadlineExceeded => HttpStatusCode.GatewayTimeout,
                    _ => HttpStatusCode.InternalServerError
                };
                var code = ex.StatusCode switch
                {
                    Grpc.Core.StatusCode.Unavailable => "SERVICE_UNAVAILABLE",
                    Grpc.Core.StatusCode.DeadlineExceeded => "GATEWAY_TIMEOUT",
                    _ => "GRPC_ERROR"
                };
                await HandleExceptionAsync(context, new ApiException(code, "A service dependency error occurred.", statusCode));
            }
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, ex);
            }
            else
            {
                logger.LogError(ex, "An unhandled exception occurred after the response had already started.");
            }
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        var response = new ErrorDto
        {
            Code = "INTERNAL_SERVER_ERROR",
            Message = "An unexpected error occurred. Please try again later."
        };
        var statusCode = HttpStatusCode.InternalServerError;

        if (exception is ApiException apiException)
        {
            response.Code = apiException.Code;
            response.Message = apiException.Message;
            response.Errors = apiException.Errors;
            statusCode = apiException.StatusCode;
        }
        else
        {
            logger.LogError(exception, "An unhandled exception occurred.");
        }

        var result = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(result);
    }
}

public class ApiException(string code, string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string[]? errors = null)
    : Exception(message)
{
    public string Code { get; } = code;
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string[]? Errors { get; } = errors;
}

public static class ExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlerMiddleware>();
    }
}
