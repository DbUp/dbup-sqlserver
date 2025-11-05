using System.Collections.Generic;
using System.Threading;
using Microsoft.Data.SqlClient;
using DbUp.Engine.Transactions;
using DbUp.Support;
using Azure.Core;
using Azure.Identity;

namespace DbUp.SqlServer;

/// <summary>Manages an Azure Sql Server database connection.</summary>
public class AzureSqlConnectionManager : DatabaseConnectionManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureSqlConnectionManager"/> class using a connection string and an Azure token credential.
    /// </summary>
    /// <param name="connectionString">The SQL connection string.</param>
    /// <param name="tokenCredential">The Azure token credential used for authentication.</param>
    /// <param name="resource">The resource URI for Azure SQL authentication.</param>
    /// <param name="tenantId">Optional tenant ID for multi-tenant authentication scenarios.</param>
    public AzureSqlConnectionManager(
        string connectionString,
        TokenCredential tokenCredential,
        string resource = "https://database.windows.net/",
        string tenantId = null
    )
        : base(new DelegateConnectionFactory((log, dbManager) =>
        {
            var tokenContext =
                new TokenRequestContext(scopes: new string[] { resource + "/.default" }, tenantId: tenantId);
            var conn = new SqlConnection(connectionString)
            {
                AccessToken = tokenCredential.GetToken(tokenContext, CancellationToken.None).Token
            };

            if (dbManager.IsScriptOutputLogged)
                conn.InfoMessage += (sender, e) => log.LogInformation($"{{0}}", e.Message);

            return conn;
        }))
    {
    }

    /// <inheritdoc/>
    public override IEnumerable<string> SplitScriptIntoCommands(string scriptContents)
    {
        var commandSplitter = new SqlCommandSplitter();
        var scriptStatements = commandSplitter.SplitScriptIntoCommands(scriptContents);
        return scriptStatements;
    }
}
