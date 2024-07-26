using System.Data;
using System.Data.Common;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.Tests.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace DbUp.SqlServer.Tests
{
    public class SqlScriptExecutorTests
    {
        [Fact]
        public void verify_schema_should_not_check_when_schema_is_null()
        {
            var executor = new SqlScriptExecutor(() => Substitute.For<IConnectionManager>(), () => null, null,
                () => false, null, () => Substitute.For<IJournal>());

            executor.VerifySchema();
        }

        [Fact]
        public void when_schema_is_null_schema_is_stripped_from_scripts()
        {
            var command = Substitute.For<IDbCommand>();
            var executor = GetSqlScriptExecutor(command);

            executor.Execute(new SqlScript("Test", "create $schema$.Table"));

            command.Received().ExecuteNonQuery();
            command.CommandText.ShouldBe("create Table");
        }

        [Fact]
        public void uses_variable_substitute_preprocessor_when_running_scripts()
        {
            var command = Substitute.For<IDbCommand>();
            var executor = GetSqlScriptExecutor(command);

            executor.Execute(new SqlScript("Test", "create $foo$.Table"),
                new Dictionary<string, string> { { "foo", "bar" } });

            command.Received().ExecuteNonQuery();
            command.CommandText.ShouldBe("create bar.Table");
        }

        [Fact]
        public void uses_variable_subtitute_preprocessor_when_running_scripts_with_single_line_comment()
        {
            var oneLineComment = @"--from excel $A$6
                                  create $foo$.Table";
            var oneLineCommentResult = @"--from excel $A$6
                                  create bar.Table";

            var command = Substitute.For<IDbCommand>();
            var executor = GetSqlScriptExecutor(command);
            executor.Execute(new SqlScript("Test", oneLineComment),
                new Dictionary<string, string> { { "foo", "bar" } });

            command.Received().ExecuteNonQuery();
            command.CommandText.ShouldBe(oneLineCommentResult);
        }

        [Fact]
        public void uses_variable_substitute_preprocessor_when_running_scripts_with_one_line_comment()
        {
            var oneLineComment = @"/* from excel $A$6 */
                                  create $foo$.Table";
            var oneLineCommentResult = @"/* from excel $A$6 */
                                  create bar.Table";

            var command = Substitute.For<IDbCommand>();
            var executor = GetSqlScriptExecutor(command);
            executor.Execute(new SqlScript("Test", oneLineComment),
                new Dictionary<string, string> { { "foo", "bar" } });

            executor.Execute(new SqlScript("Test", oneLineComment),
                new Dictionary<string, string> { { "foo", "bar" } });

            command.Received().ExecuteNonQuery();
            command.CommandText.ShouldBe(oneLineCommentResult);
        }

        [Fact]
        public void uses_variable_substitute_preprocessor_when_running_scripts_with_multi_line_comment()
        {
            var multiLineComment = @"/* 
                                        some comment
                                        from excel $A$6 
                                        some comment
                                      */
                                  create $foo$.Table";
            var multiLineCommentResult = @"/* 
                                        some comment
                                        from excel $A$6 
                                        some comment
                                      */
                                  create bar.Table";
            var dbConnection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var executor = GetSqlScriptExecutor(command);

            executor.Execute(new SqlScript("Test", multiLineComment),
                new Dictionary<string, string> { { "foo", "bar" } });

            command.Received().ExecuteNonQuery();
            command.CommandText.ShouldBe(multiLineCommentResult);
        }

        [Fact]
        public void uses_variable_substitute_preprocessor_when_running_scripts_with_nested_single_line_comment()
        {
            var multiLineComment = @"/* 
                                        some comment
                                        --from excel $A$6 
                                        some comment
                                      */
                                  create $foo$.Table";
            var multiLineCommentResult = @"/* 
                                        some comment
                                        --from excel $A$6 
                                        some comment
                                      */
                                  create bar.Table";
            var dbConnection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var executor = GetSqlScriptExecutor(command);

            executor.Execute(new SqlScript("Test", multiLineComment),
                new Dictionary<string, string> { { "foo", "bar" } });

            command.Received().ExecuteNonQuery();
            command.CommandText.ShouldBe(multiLineCommentResult);
        }

        [Fact]
        public void uses_variable_substitute_preprocessor_when_running_scripts_with_nested_comment()
        {
            var multiLineComment = @"/* 
                                        some comment
                                        /* from excel $A$6 */
                                        some comment
                                      */
                                  create $foo$.Table";
            var multiLineCommentResult = @"/* 
                                        some comment
                                        /* from excel $A$6 */
                                        some comment
                                      */
                                  create bar.Table";
            var dbConnection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var executor = GetSqlScriptExecutor(command);

            executor.Execute(new SqlScript("Test", multiLineComment),
                new Dictionary<string, string> { { "foo", "bar" } });

            command.Received().ExecuteNonQuery();
            command.CommandText.ShouldBe(multiLineCommentResult);
        }

        [Fact]
        public void does_not_use_variable_substitute_preprocessor_when_setting_false()
        {
            var command = Substitute.For<IDbCommand>();
            var executor = GetSqlScriptExecutor(command, variablesEnabled: false);

            executor.Execute(new SqlScript("Test", "create $foo$.Table"),
                new Dictionary<string, string> { { "foo", "bar" } });

            command.Received().ExecuteNonQuery();
            command.CommandText.ShouldBe("create $foo$.Table");
        }


        [Fact]
        public void uses_variable_subtitutes_schema()
        {
            var command = Substitute.For<IDbCommand>();
            var executor = GetSqlScriptExecutor(command, schema: "foo");

            executor.Execute(new SqlScript("Test", "create $schema$.Table"));

            command.Received().ExecuteNonQuery();
            command.CommandText.ShouldBe("create [foo].Table");
        }

        [Fact]
        public void logs_output_when_configured_to()
        {
            var command = Substitute.For<IDbCommand>();

            var reader = Substitute.For<IDataReader>();
            reader.FieldCount.Returns(2);
            reader.GetName(Arg.Is(0)).Returns("One");
            reader.GetName(Arg.Is(1)).Returns("Two");
            reader.GetName(Arg.Is<int>(i => i < 0 || i > 1)).Throws(new ArgumentOutOfRangeException("i"));

            reader.Read().Returns(true, false);
            reader.GetValue(Arg.Is(0)).Returns("A");
            reader.GetValue(Arg.Is(1)).Returns("B");
            reader.NextResult().Returns(false);

            command.ExecuteReader().Returns(reader);

            var logger = new CaptureLogsLogger();
            var executor = GetSqlScriptExecutor(command, log: logger, schema: "foo", isScriptOutputLogged: true);

            executor.Execute(new SqlScript("Test", "SELECT * FROM $schema$.[Table]"));

            command.ReceivedWithAnyArgs().ExecuteReader();
            command.DidNotReceive().ExecuteNonQuery();
            command.CommandText.ShouldBe("SELECT * FROM [foo].[Table]");

            logger.Log.Trim()
                .ShouldBe(string.Join(Environment.NewLine,
                    new[]
                    {
                        "Info:         Executing Database Server script 'Test'", "Info:         -------------",
                        "Info:         | One | Two |", "Info:         -------------", "Info:         |   A |   B |",
                        "Info:         -------------", "Info:"
                    }));
        }

        [Fact]
        public void logs_when_dbexception()
        {
            var dbConnection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            command.When(x => x.ExecuteNonQuery()).Do(x =>
            {
                var ex = Substitute.For<DbException>();
                ex.Message.Returns("Message with curly braces {0}");
                throw ex;
            });
            dbConnection.CreateCommand().Returns(command);
            var logger = Substitute.For<IUpgradeLog>();
            logger.WhenForAnyArgs(x => x.LogError(null, (object[]?)null))
                .Do(x => Console.WriteLine(x.Arg<string>(), x.Arg<object[]>()));

            var executor = GetSqlScriptExecutor(command, log: logger);

            Action exec = () => executor.Execute(new SqlScript("Test", "create $schema$.Table"));
            exec.ShouldThrow<DbException>();
            command.Received().ExecuteNonQuery();
            logger.ReceivedWithAnyArgs().LogError("", null);
        }

        static SqlScriptExecutor GetSqlScriptExecutor(
            IDbCommand command,
            bool variablesEnabled = true,
            string? schema = null,
            IUpgradeLog? log = null,
            bool isScriptOutputLogged = false
        )
        {
            var dbConnection = Substitute.For<IDbConnection>();
            dbConnection.CreateCommand().Returns(command);
            var testConnectionManager = new TestConnectionManager(dbConnection) { IsScriptOutputLogged = isScriptOutputLogged };
            log ??= new ConsoleUpgradeLog();
            testConnectionManager.OperationStarting(log, []);

            var executor = new SqlScriptExecutor(() => testConnectionManager, () => log, schema, () => variablesEnabled,
                null, () => Substitute.For<IJournal>());
            return executor;
        }
    }
}
