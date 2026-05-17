using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using InovaSkillGrespan.Infrastructure.Database;

namespace InovaSkillGrespan.Infrastructure.Persistence;

public sealed class SqlConnectionFactory(IOptions<DatabaseOptions> options)
{
    private readonly DatabaseOptions _options = options.Value;

    public SqlConnection Create()
    {
        return new SqlConnection(_options.ConnectionString);
    }
}

