using System.Diagnostics;
using System.Reflection;
using AR.NetExt.Strings;
using AR.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog.Context;

namespace AR.API.Infrastructure.Middlewares;

internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        var isApiController = endpoint?.Metadata
                                      .GetMetadata<ControllerActionDescriptor>()
                                      ?.ControllerTypeInfo
                                      .GetCustomAttribute(typeof(ApiControllerAttribute), inherit: true) is not null;
        if (isApiController)
        {
            var correlationId = httpContext.Request.Headers[Constants.CorrelationIdHeader].FirstOrDefault();
            if(correlationId.IsNullOrEmptyExt())
            {
                var traceId = Activity.Current?.TraceId.ToString();
                // local .net trace string
                var traceIdentifier = httpContext.TraceIdentifier;

                correlationId = traceId.IsNullOrEmptyExt() ? traceIdentifier : traceId;
            }

            // add to request to get from ExecutionContext
            httpContext.Items.TryAdd(Constants.CorrelationIdHeader, correlationId);
            // add to response
            httpContext.Response.Headers[Constants.CorrelationIdHeader] = correlationId;

            // add to logger
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next(httpContext);
            }
        }
        else
        {
            await next(httpContext);
        }
    }
}