using System.Runtime.CompilerServices;
using AR.NetExt.Models.Exceptions;

namespace AR.Shared.Extensions;

public static class GuidExtensions
{
    public static Guid GetValidatedValueExt(
        this Guid? value,
        [CallerArgumentExpression(nameof(value))] string? name = null) =>
        value ?? throw new BadRequestExceptionExt($"Passed null or empty value for required '{name}' param.");
}