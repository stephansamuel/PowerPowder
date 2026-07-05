using System;
using System.Collections.Generic;
using PowerPowder.Snowflake.PowerShell;
using PowerPowder.Snowflake.PowerShell.Tests.TestDoubles;

namespace PowerPowder.Snowflake.PowerShell.Tests;

public class SnowflakeSqlExecutorTests
{
    [Fact]
    public void Execute_WithNullConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SnowflakeSqlExecutor.Execute(null!, "select 1", 0, false));
    }

    [Fact]
    public void Execute_WithNullSql_ThrowsArgumentNullException()
    {
        var connection = new FakeDbConnection(new List<KeyValuePair<string, FakeCommandPlan>>());
        Assert.Throws<ArgumentNullException>(() => SnowflakeSqlExecutor.Execute(connection, null!, 0, false));
    }

    [Fact]
    public void Execute_WithRowsAndNonQuery_ProducesExpectedItems()
    {
        var selectSet = new FakeResultSet();
        selectSet.Columns.Add(new FakeColumn("ID", typeof(int)));
        selectSet.Columns.Add(new FakeColumn("NAME", typeof(string)));
        selectSet.Rows.Add(new object?[] { 1, "alpha" });

        var selectPlan = new FakeCommandPlan();
        selectPlan.ResultSets.Add(selectSet);

        var updatePlan = new FakeCommandPlan { RecordsAffected = 3 };

        var connection = new FakeDbConnection(new[]
        {
            new KeyValuePair<string, FakeCommandPlan>("select 1", selectPlan),
            new KeyValuePair<string, FakeCommandPlan>("update t set c = 1", updatePlan)
        });

        var results = SnowflakeSqlExecutor.Execute(connection, "select 1; update t set c = 1;", 7, false);

        Assert.Equal(2, results.Count);

        var row = results[0];
        Assert.False(row.IsNonQuery);
        Assert.NotNull(row.Row);
        Assert.Equal(1, row.StatementIndex);
        Assert.Equal(1, row.ResultSetIndex);
        Assert.Equal(1, row.Row!["ID"]);
        Assert.Equal("alpha", row.Row["NAME"]);

        var nonQuery = results[1];
        Assert.True(nonQuery.IsNonQuery);
        Assert.Equal(2, nonQuery.StatementIndex);
        Assert.Equal(3, nonQuery.RowsAffected);

        Assert.Equal(7, connection.CreatedCommands[0].CommandTimeout);
        Assert.Equal(7, connection.CreatedCommands[1].CommandTimeout);
    }

    [Fact]
    public void Execute_AsDataTable_ReturnsDataTableOutput()
    {
        var resultSet = new FakeResultSet();
        resultSet.Columns.Add(new FakeColumn("ID", typeof(int)));
        resultSet.Columns.Add(new FakeColumn("AMOUNT", typeof(decimal)));
        resultSet.Rows.Add(new object?[] { 1, 10.5m });
        resultSet.Rows.Add(new object?[] { 2, 11.0m });

        var plan = new FakeCommandPlan();
        plan.ResultSets.Add(resultSet);

        var connection = new FakeDbConnection(new[]
        {
            new KeyValuePair<string, FakeCommandPlan>("select * from t", plan)
        });

        var results = SnowflakeSqlExecutor.Execute(connection, "select * from t", 0, true);

        Assert.Single(results);
        Assert.NotNull(results[0].Table);
        var table = results[0].Table!;
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(10.5m, table.Rows[0]["AMOUNT"]);
    }

    [Fact]
    public void Execute_WithMultipleResultSets_TracksResultSetIndex()
    {
        var firstSet = new FakeResultSet();
        firstSet.Columns.Add(new FakeColumn("COL", typeof(string)));
        firstSet.Rows.Add(new object?[] { "first" });

        var secondSet = new FakeResultSet();
        secondSet.Columns.Add(new FakeColumn("COL", typeof(string)));
        secondSet.Rows.Add(new object?[] { "second" });

        var plan = new FakeCommandPlan();
        plan.ResultSets.Add(firstSet);
        plan.ResultSets.Add(secondSet);

        var connection = new FakeDbConnection(new[]
        {
            new KeyValuePair<string, FakeCommandPlan>("select mixed", plan)
        });

        var results = SnowflakeSqlExecutor.Execute(connection, "select mixed", 0, false);

        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].ResultSetIndex);
        Assert.Equal(2, results[1].ResultSetIndex);
        Assert.Equal("first", results[0].Row!["COL"]);
        Assert.Equal("second", results[1].Row!["COL"]);
    }
}
