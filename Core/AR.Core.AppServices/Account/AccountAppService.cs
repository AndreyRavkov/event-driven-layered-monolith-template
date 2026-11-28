using AR.Core.AppServices.Infrastructure;
using AR.Core.Events.Users;
using AR.Core.Persistence.Context;
using AR.Core.Persistence.Repositories.Users;
using AR.Infrastructure.Jwt;
using AR.Infrastructure.Jwt.DTO;
using AR.NetExt.Models.Exceptions;

namespace AR.Core.AppServices.Account;

public interface IAccountAppService
{
    Task<string> CreateTokenAsync();
    string RefreshToken(int userId, string userName);
}

internal sealed class AccountAppService : IAccountAppService
{
    private readonly IEventDispatcher _eventDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenProvider _jwtTokenProvider;
    private readonly IUserRepository _userRepository;

    public AccountAppService(IEventDispatcher eventDispatcher, IUnitOfWork unitOfWork, IJwtTokenProvider jwtTokenProvider, IUserRepository userRepository)
    {
        _eventDispatcher = eventDispatcher;
        _unitOfWork = unitOfWork;
        _jwtTokenProvider = jwtTokenProvider;
        _userRepository = userRepository;
    }

    public Task<string> CreateTokenAsync() =>
        _unitOfWork.CommitAsync(async () =>
        {
            var users = await _userRepository.GetAsync();
            if (!users.Any())
            {
                throw new UnAuthorizationExceptionExt("User name or password are not valid.");
            }

            await _unitOfWork.AddEventAsync(new UserLoginEvent());

            var token = _jwtTokenProvider.Create(new JwtTokenDtoModel
            {
                UserId = users[0].Id,
                UserName = users[0].Name,
            });
            return token;
        }, _eventDispatcher);

    public string RefreshToken(int userId, string userName)
    {
        var token = _jwtTokenProvider.Create(new JwtTokenDtoModel
        {
            UserId = userId,
            UserName = userName,
        });

        return token;
    }
}