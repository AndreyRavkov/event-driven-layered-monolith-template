using AR.Core.Persistence.Context;
using Dapper;

namespace AR.Core.Persistence.Handlers.Queries;

public interface IUserQueryHandler
{
    Task<string> GetUserNameAsync(int id);
}

internal sealed class UserQueryHandler(DbDomainContext dbDomainContext) : BaseHandler(dbDomainContext), IUserQueryHandler
{
    public Task<string> GetUserNameAsync(int id) => 
        Connection.QueryFirstAsync<string>("SELECT Name FROM users WHERE Id = @id", new { id }, Transaction);
}