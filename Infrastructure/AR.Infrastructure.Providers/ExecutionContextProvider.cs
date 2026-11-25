using System.Security.Claims;
using AR.Shared;
using Microsoft.AspNetCore.Http;

namespace AR.Infrastructure.Providers;

public interface IExecutionContextProvider
{
    Guid UserId { get; }
    string UserName { get; }
    string ChangedSource { get; }
    string? CorrelationId { get; }
}

internal sealed class ExecutionContextProvider : IExecutionContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExecutionContextProvider(IHttpContextAccessor httpContextAccessor) => 
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public Guid UserId
    {
        get
        {
            var claim = _httpContextAccessor
                        .HttpContext?.User.Claims
                        .FirstOrDefault(f => f.Type == ClaimTypes.Sid);
            if (claim is not null)
            {
                if (Guid.TryParse(claim.Value, out var result))
                {
                    return result;
                }
            }
            throw new ArgumentNullException(nameof(UserId));
        }
    }

    public string UserName
    {
        get
        {
            var claim = _httpContextAccessor
                        .HttpContext?.User.Claims
                        .FirstOrDefault(f => f.Type == ClaimTypes.Name);
            return claim is not null
                       ? claim.Value
                       : throw new ArgumentNullException(nameof(UserName));
        }
    }

    public string ChangedSource => 
        $"{_httpContextAccessor.HttpContext?.Request.Method}:{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}{_httpContextAccessor.HttpContext?.Request.Path}";

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext != null
            && _httpContextAccessor.HttpContext.Items.TryGetValue(Constants.CorrelationIdHeader, out var item)
                ? item?.ToString()
                : null;
}