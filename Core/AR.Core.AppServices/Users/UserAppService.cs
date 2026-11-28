using AR.Core.Persistence.Repositories.Users;
using AR.NetExt.Utils.Collections;
using JetBrains.Annotations;

namespace AR.Core.AppServices.Users;

public interface IUserAppService {
    Task<IReadOnlyList<(int Id, string Name)>> GetAsync();
}

[UsedImplicitly]
internal sealed class UserAppService : IUserAppService
{
    private readonly IUserRepository _userRepository;

    public UserAppService(IUserRepository userRepository) => _userRepository = userRepository;

    public async Task<IReadOnlyList<(int Id, string Name)>> GetAsync()
    {
        var users = await _userRepository.GetAsync();

        return users.Select(s => (s.Id, s.Name)).AsReadOnlyListExt();
    }
}
