---
paths:
  - "solution/AR/API/AR.API/**/*.cs"
---

## Rule for **API Layer** for Web API

- Each controller should inherit from `BaseApiV1Controller` or default `ApiController`.
- Controllers should be located in the `Controllers` folder.
- Controllers must be [Route("v{version:apiVersion}/...")] and carry a [Tags("...")] group. One tag per controller. Action methods should be tagged with the http verb name.
- Controllers stay thin: delegate to an injected `ApiService` from [AR.ApiServices](layer-style-api-service.md); no business logic, no direct repository/DbContext access.
- All Infrastructure (Middlewares, Auth, Exceptions, Attributes) should be located in the `Infrastructure` folder.
- All application settings should be located in the `appsettings.json` use environment-specific variants.
- All request Models should be used from `AR.ApiServices` project. See rules from [AR.ApiServices](layer-style-api-service.md).

## Cross-cutting / what NOT to do
- Don't write try/catch in controllers — let GlobalExceptionHandler map exceptions to ProblemDetails.
- Don't validate manually — rely on [RequestModelFluentValidator] (inherited) + the request model's IValidator<T>.
- Get the caller's identity from UserId/UserName on the base controller, not from HttpContext.User directly.