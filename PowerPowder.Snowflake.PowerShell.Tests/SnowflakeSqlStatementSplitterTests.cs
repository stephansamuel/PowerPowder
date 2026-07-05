using System;
using PowerPowder.Snowflake.PowerShell;

namespace PowerPowder.Snowflake.PowerShell.Tests;

public class SnowflakeSqlStatementSplitterTests
{
    [Fact]
    public void Split_WithSingleStatement_ReturnsOne()
    {
        var result = SnowflakeSqlStatementSplitter.Split("select 1");
        Assert.Single(result);
        Assert.Equal("select 1", result[0]);
    }

    [Fact]
    public void Split_WithMultipleStatements_ReturnsEachStatement()
    {
        var result = SnowflakeSqlStatementSplitter.Split("select 1; select 2;");
        Assert.Equal(2, result.Count);
        Assert.Equal("select 1", result[0]);
        Assert.Equal("select 2", result[1]);
    }

    [Fact]
    public void Split_IgnoresSemicolonInsideQuotedString()
    {
        var result = SnowflakeSqlStatementSplitter.Split("select 'a;b'; select 2;");
        Assert.Equal(2, result.Count);
        Assert.Equal("select 'a;b'", result[0]);
    }

    [Fact]
    public void Split_HandlesCommentsWithoutSplittingInsideThem()
    {
        const string sql = "select 1; -- comment ;\nselect 2; /* block;comment */ select 3;";
        var result = SnowflakeSqlStatementSplitter.Split(sql);
        Assert.Equal(3, result.Count);
        Assert.Equal("select 1", result[0]);
        Assert.Equal("-- comment ;\nselect 2", result[1]);
        Assert.Equal("/* block;comment */ select 3", result[2]);
    }

    [Fact]
    public void Split_WithWhitespaceOnly_ReturnsEmptyCollection()
    {
        var result = SnowflakeSqlStatementSplitter.Split(" ;  ;\n\t");
        Assert.Empty(result);
    }

    [Fact]
    public void Split_WithNullSql_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SnowflakeSqlStatementSplitter.Split(null!));
    }
}
