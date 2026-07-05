using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Snowflake.Data.Client;

namespace PowerPowder.Snowflake.PowerShell;

internal static class SnowflakeConnectionStringFactory
{
    public static string Build(SnowflakeConnectRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var builder = new DbConnectionStringBuilder();
        foreach (var pair in request.ToConnectionProperties())
        {
            builder[pair.Key] = pair.Value;
        }

        return builder.ConnectionString;
    }
}

internal sealed class SnowflakeDbConnectionFactory : ISnowflakeDbConnectionFactory
{
    [ExcludeFromCodeCoverage]
    public DbConnection Create(string connectionString)
    {
        return new SnowflakeDbConnection
        {
            ConnectionString = connectionString
        };
    }
}
