using AR.Core.AppServices.Users;

namespace AR.ApiServices.Users;

public interface IUserApiService
{
    Task<IReadOnlyList<(int Id, string Name)>> GetAsync();
}

internal sealed class UserApiService : IUserApiService
{
    private readonly IUserAppService _userAppService;

    public UserApiService(IUserAppService userAppService) => _userAppService = userAppService;

    public Task<IReadOnlyList<(int Id, string Name)>> GetAsync() => _userAppService.GetAsync();
}