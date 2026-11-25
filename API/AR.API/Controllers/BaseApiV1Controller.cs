using AR.API.Infrastructure;
using AR.API.Infrastructure.Attributes;
using AR.API.Infrastructure.Models;
using AR.Infrastructure.Providers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AR.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = $"v{ApiConstants.ApiVersion1}")]
[Produces(ApiConstants.ContentTypeJson)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ARProblemDetailsModel))]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ARProblemDetailsModel))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ARProblemDetailsModel))]
[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ARProblemDetailsModel))]
[RequestModelFluentValidator]
public class BaseApiV1Controller : ControllerBase
{
    private IExecutionContextProvider ExecutionContext =>
        HttpContext.RequestServices.GetService<IExecutionContextProvider>()
            ?? throw new ArgumentNullException(nameof(IExecutionContextProvider));
    
    private readonly string _controllerName = "Controller";
    
    protected Guid UserId => ExecutionContext.UserId;
    protected string UserName => ExecutionContext.UserName;
    
    protected string GetControllerName(string controller)
    {
        // necessary for: return CreatedAtAction(nameof(Get), GetControllerName(nameof(TestController)), <model>);
        var index = controller.IndexOf(_controllerName, StringComparison.InvariantCultureIgnoreCase);
        return index > 0 ? controller.Substring(0, index) : controller;
    }
}