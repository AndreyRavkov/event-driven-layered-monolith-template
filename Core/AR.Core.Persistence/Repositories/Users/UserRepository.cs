using AR.Domain.Users;
using AR.NetExt.MayBe;
using JetBrains.Annotations;

namespace AR.Core.Persistence.Repositories.Users;

public interface IUserRepository
{
    Task<IReadOnlyList<UserEntity>> GetAsync();

    ValueTask<MayBe<UserEntity>> GetAsync(int id);
}

[UsedImplicitly]
internal sealed class UserRepository : IUserRepository
{
    private readonly IRepository<UserEntity> _repository;

    public UserRepository(IRepository<UserEntity> repository) => _repository = repository;

    public Task<IReadOnlyList<UserEntity>> GetAsync() => _repository.GetAsync();

    public ValueTask<MayBe<UserEntity>> GetAsync(int id) => _repository.GetAsync(id);
}