using System;
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
            : this(() => new SqlConnection(connectionString))
        {
        }

        /// <summary>
        /// Manages Sql Database Connections using an existing connection.
        /// </summary>
        /// <param name="connectionFactory">A factory function that creates a new SQL connection.</param>
        public SqlConnectionManager(Func<SqlConnection> connectionFactory)
            : base(new DelegateConnectionFactory((log, dbManager) =>
            {
                var conn = connectionFactory();
                if (dbManager.IsScriptOutputLogged)
                    conn.InfoMessage += (sender, e) => log.LogInformation("{0}", e.Message);

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
}