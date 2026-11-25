using System.Data.Common;
using AR.Core.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AR.Core.Persistence.Handlers;

internal class BaseHandler(DbDomainContext context)
{
    protected DbConnection Connection => context.Database.GetDbConnection();
    
    protected DbTransaction? Transaction => context.Database.CurrentTransaction?.GetDbTransaction();
}