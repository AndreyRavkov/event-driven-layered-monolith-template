using AR.ApiServices.Account;
using AR.ApiServices.Account.Models;
using AR.ApiServices.Users;
using AR.Infrastructure.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AR.API.Controllers.Account;

[Route("v{version:apiVersion}/me")]
[Tags("Account")]
public class MeController : BaseApiV1Controller
{
    private readonly ILogger<MeController> _logger;
    private readonly IUserApiService _userApiService;
    private readonly IAccountApiService _accountApiService;

    public MeController(ILogger<MeController> logger, IUserApiService userApiService, IAccountApiService accountApiService)
    {
        _logger = logger;
        _userApiService = userApiService;
        _accountApiService = accountApiService;
    }

    /// <summary>
    /// Get user info
    /// </summary>
    /// <returns>200 or 401 status error.</returns>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var users = await _userApiService.GetAsync();
        return Ok(users.Select(s => new { s.Id, s.Name }).FirstOrDefault());
    }

    /// <summary>
    /// Create token
    /// </summary>
    /// <param name="userRequest"></param>
    /// <returns>Token value</returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Post([FromBody] UserRequest userRequest)
    {
        _logger.LogInformation("{Template}", JsonProvider.Serialize(userRequest));

        var token = await _accountApiService.CreateTokenAsync();

        return Ok(token);
    }
}
