using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;

namespace PowerPowder.Snowflake.PowerShell;

internal static class SnowflakeConnectionStore
{
    private static readonly ConcurrentDictionary<string, DbConnection> Connections =
        new ConcurrentDictionary<string, DbConnection>(StringComparer.OrdinalIgnoreCase);

    public static void AddOrReplace(string name, DbConnection connection)
    {
        if (Connections.TryGetValue(name, out var existing))
        {
            try
            {
                existing.Close();
                existing.Dispose();
            }
            catch
            {
                // Ignore failures while replacing stale connection instances.
            }
        }

        Connections[name] = connection;
    }

    public static bool TryGet(string name, out DbConnection connection)
    {
        return Connections.TryGetValue(name, out connection!);
    }

    public static bool Remove(string name, out DbConnection connection)
    {
        return Connections.TryRemove(name, out connection!);
    }

    public static IReadOnlyDictionary<string, DbConnection> Snapshot()
    {
        return new Dictionary<string, DbConnection>(Connections);
    }

    public static void Clear()
    {
        foreach (var key in Connections.Keys)
        {
            if (Connections.TryRemove(key, out var connection))
            {
                try
                {
                    connection.Dispose();
                }
                catch
                {
                    // Ignore failures while clearing test or process state.
                }
            }
        }
    }
}
