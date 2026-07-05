using System.Collections.Generic;
using PowerPowder.Snowflake.PowerShell;
using PowerPowder.Snowflake.PowerShell.Tests.TestDoubles;

namespace PowerPowder.Snowflake.PowerShell.Tests;

public class SnowflakeConnectionStoreTests : IDisposable
{
    public SnowflakeConnectionStoreTests()
    {
        SnowflakeConnectionStore.Clear();
    }

    [Fact]
    public void AddOrReplace_ReplacesAndDisposesExistingConnection()
    {
        var first = new FakeDbConnection(new List<KeyValuePair<string, FakeCommandPlan>>());
        var second = new FakeDbConnection(new List<KeyValuePair<string, FakeCommandPlan>>());

        SnowflakeConnectionStore.AddOrReplace("default", first);
        SnowflakeConnectionStore.AddOrReplace("default", second);

        Assert.True(first.WasDisposed);
        Assert.True(SnowflakeConnectionStore.TryGet("default", out var stored));
        Assert.Same(second, stored);
    }

    [Fact]
    public void TryGet_IsCaseInsensitive()
    {
        var connection = new FakeDbConnection(new List<KeyValuePair<string, FakeCommandPlan>>());
        SnowflakeConnectionStore.AddOrReplace("MyConn", connection);

        Assert.True(SnowflakeConnectionStore.TryGet("myconn", out var stored));
        Assert.Same(connection, stored);
    }

    [Fact]
    public void Remove_ReturnsTrueAndRemovedConnection()
    {
        var connection = new FakeDbConnection(new List<KeyValuePair<string, FakeCommandPlan>>());
        SnowflakeConnectionStore.AddOrReplace("default", connection);

        var removed = SnowflakeConnectionStore.Remove("default", out var removedConnection);

        Assert.True(removed);
        Assert.Same(connection, removedConnection);
        Assert.False(SnowflakeConnectionStore.TryGet("default", out _));
    }

    [Fact]
    public void Snapshot_ReturnsCopy()
    {
        var connection = new FakeDbConnection(new List<KeyValuePair<string, FakeCommandPlan>>());
        SnowflakeConnectionStore.AddOrReplace("default", connection);

        var snapshot = SnowflakeConnectionStore.Snapshot();

        Assert.Single(snapshot);
        Assert.True(snapshot.ContainsKey("default"));
    }

    [Fact]
    public void Clear_DisposesAllConnections()
    {
        var one = new FakeDbConnection(new List<KeyValuePair<string, FakeCommandPlan>>());
        var two = new FakeDbConnection(new List<KeyValuePair<string, FakeCommandPlan>>());
        SnowflakeConnectionStore.AddOrReplace("one", one);
        SnowflakeConnectionStore.AddOrReplace("two", two);

        SnowflakeConnectionStore.Clear();

        Assert.True(one.WasDisposed);
        Assert.True(two.WasDisposed);
        Assert.Empty(SnowflakeConnectionStore.Snapshot());
    }

    public void Dispose()
    {
        SnowflakeConnectionStore.Clear();
    }
}
