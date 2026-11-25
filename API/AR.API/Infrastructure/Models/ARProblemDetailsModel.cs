using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace AR.API.Infrastructure.Models;

internal sealed class ARProblemDetailsModel : ProblemDetails
{
    [JsonPropertyName("details")]
    public IReadOnlyList<string> Details { get; set; } = new List<string>();

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }
}