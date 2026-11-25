using AR.Core.Persistence.Context;
using Dapper;

namespace AR.Core.Persistence.Handlers.Commands;

public interface ICommandHandler
{
    Task ExecuteAsync(int id, string name);
}

internal sealed class CommandHandler(DbDomainContext dbDomainContext) : BaseHandler(dbDomainContext), ICommandHandler
{
    public Task ExecuteAsync(int id, string name)
    {
        const string sql = """
                           INSERT TOP 1
                           """;
        
        return Connection.ExecuteAsync(sql, new { id, name }, Transaction);
        
    }
}