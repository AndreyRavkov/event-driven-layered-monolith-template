namespace AR.API.Infrastructure;

internal abstract class ApiConstants
{
    internal const string ApiVersion1 = "1";
    internal const string ApiVersion2 = "2";
    internal const string ApiDocUrl = "api/doc";
    internal const string TypePattern = "https://httpstatuses.com";
    internal const string ContentTypeFormUrlEncoded = "application/x-www-form-urlencoded";
    internal const string ContentTypeJson = "application/json";

    internal const string InternalServerErrorResponseTitle = "Internal Application Error";
    internal const string InternalServerErrorResponseDetail = "An unexpected error occurred on the server while processing your request.";
    internal const string NotFoundResponseTitle = "Not Found";
    internal const string ForbiddenResponseTitle = "Forbidden";
    internal const string BadRequestResponseTitle = "Bad Request";
    internal const string UnauthorizedResponseTitle = "Unauthorized";
    internal const string AccessDenied = "Access denied";
}
