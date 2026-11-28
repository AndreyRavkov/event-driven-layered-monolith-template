using AR.Core.AppServices.Account;
using JetBrains.Annotations;

namespace AR.ApiServices.Account;

public interface IAccountApiService
{
    Task<string> CreateTokenAsync();
    string RefreshToken(int userId, string userName);
}

[UsedImplicitly]
internal sealed class AccountApiService : IAccountApiService
{
    private readonly IAccountAppService _accountAppService;

    public AccountApiService(IAccountAppService accountAppService) => _accountAppService = accountAppService;

    public Task<string> CreateTokenAsync() => _accountAppService.CreateTokenAsync();

    public string RefreshToken(int userId, string userName) => _accountAppService.RefreshToken(userId, userName);
}
