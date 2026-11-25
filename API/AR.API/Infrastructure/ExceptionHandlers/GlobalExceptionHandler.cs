using AR.API.Infrastructure.Models;
using AR.Infrastructure.Hosting;
using AR.Infrastructure.Json;
using AR.NetExt.Models.Exceptions;
using AR.NetExt.Utils.Collections;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace AR.API.Infrastructure.ExceptionHandlers;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, exception.Message);

        httpContext.Response.ContentType = ApiConstants.ContentTypeJson;
        var problemDetails = new ARProblemDetailsModel()
        {
            Detail = exception.Message,
        };

        switch (exception)
        {
            case UnAuthorizationExceptionExt:
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                problemDetails.Title = ApiConstants.UnauthorizedResponseTitle;
                break;
            }
            case ValidationException validationException:
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Title = ApiConstants.BadRequestResponseTitle;
                problemDetails.Details = validationException.Errors.Select(s => s.ErrorMessage).AsReadOnlyListExt();
                break;
            }
            case BadRequestExceptionExt:
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Title = ApiConstants.BadRequestResponseTitle;
                break;
            }
            case NotFoundExceptionExt:
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails.Title = ApiConstants.NotFoundResponseTitle;
                break;
            }
            case ForbiddenExceptionExt:
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                problemDetails.Title = ApiConstants.ForbiddenResponseTitle;
                break;
            }
            default:
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails.Title = ApiConstants.InternalServerErrorResponseTitle;
                if (AREnvironment.IsProduction())
                {
                    problemDetails.Detail = ApiConstants.InternalServerErrorResponseDetail;
                }
                break;
            }
        }

        // add stack trace for local requests
        if (!AREnvironment.IsProduction())
        {
            problemDetails.StackTrace = exception.StackTrace;
        }

        problemDetails.Status = httpContext.Response.StatusCode;
        problemDetails.Type = $"{ApiConstants.TypePattern}/{httpContext.Response.StatusCode}";

        await httpContext.Response.WriteAsync(JsonProvider.Serialize(problemDetails), cancellationToken: ct);

        return true;
    }
}