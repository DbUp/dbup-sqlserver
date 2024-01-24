using DbUp.Tests.Common;

namespace DbUp.SqlServer.Tests;

public class NoPublicApiChanges : NoPublicApiChangesBase
{
    public NoPublicApiChanges()
        : base(typeof(SqlServerExtensions).Assembly)
    {
    }
}
