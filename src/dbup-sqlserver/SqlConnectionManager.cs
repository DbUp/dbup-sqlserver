using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using DbUp.Engine.Transactions;
using DbUp.Support;

namespace DbUp.SqlServer
{
    /// <summary>
    /// Manages Sql Database Connections
    /// </summary>
    public class SqlConnectionManager : DatabaseConnectionManager
    {
        /// <summary>
        /// Manages Sql Database Connections
        /// </summary>
        /// <param name="connectionString"></param>
        public SqlConnectionManager(string connectionString)
            : base(new DelegateConnectionFactory((log, dbManager) =>
            {
                var connection = new SqlConnection(connectionString);

                if (dbManager.IsScriptOutputLogged)
                    connection.InfoMessage += (sender, e) => log.LogInformation($"{{0}}", e.Message);

                return connection;
            }))
        { }

        /// <summary>
        /// Manages Sql Database Connections using an existing connection.
        /// </summary>
        /// <param name="connection">The existing SQL connection to use.</param>
        public SqlConnectionManager(SqlConnection connection)
            : base(new DelegateConnectionFactory((log, dbManager) =>
            {
                if (dbManager.IsScriptOutputLogged)
                    connection.InfoMessage += (sender, e) => log.LogInformation($"{{0}}", e.Message);

                return connection;
            }))
        { }

        /// <inheritdoc/>
        public override IEnumerable<string> SplitScriptIntoCommands(string scriptContents)
        {
            var commandSplitter = new SqlCommandSplitter();
            var scriptStatements = commandSplitter.SplitScriptIntoCommands(scriptContents);
            return scriptStatements;
        }
    }
}
