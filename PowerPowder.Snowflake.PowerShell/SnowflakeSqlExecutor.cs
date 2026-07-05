using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace PowerPowder.Snowflake.PowerShell;

internal static class SnowflakeSqlExecutor
{
    public static IReadOnlyList<SnowflakeSqlExecutionItem> Execute(
        DbConnection connection,
        string sql,
        int commandTimeoutSeconds,
        bool asDataTable)
    {
        if (connection == null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (sql == null)
        {
            throw new ArgumentNullException(nameof(sql));
        }

        var outputs = new List<SnowflakeSqlExecutionItem>();
        var statements = SnowflakeSqlStatementSplitter.Split(sql);

        for (var statementIndex = 0; statementIndex < statements.Count; statementIndex++)
        {
            var statement = statements[statementIndex];
            using (var command = connection.CreateCommand())
            {
                command.CommandText = statement;
                if (commandTimeoutSeconds > 0)
                {
                    command.CommandTimeout = commandTimeoutSeconds;
                }

                using (var reader = command.ExecuteReader())
                {
                    var wroteResultRows = false;
                    var resultSetIndex = 0;
                    do
                    {
                        if (reader.FieldCount > 0)
                        {
                            wroteResultRows = true;
                            if (asDataTable)
                            {
                                var table = ToDataTable(reader);
                                outputs.Add(SnowflakeSqlExecutionItem.FromDataTable(statement, statementIndex + 1, resultSetIndex + 1, table));
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                                    for (var i = 0; i < reader.FieldCount; i++)
                                    {
                                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    }

                                    outputs.Add(SnowflakeSqlExecutionItem.FromRow(statement, statementIndex + 1, resultSetIndex + 1, row));
                                }
                            }
                        }

                        resultSetIndex++;
                    }
                    while (reader.NextResult());

                    if (!wroteResultRows)
                    {
                        outputs.Add(SnowflakeSqlExecutionItem.FromNonQuery(statement, statementIndex + 1, reader.RecordsAffected));
                    }
                }
            }
        }

        return outputs;
    }

    private static DataTable ToDataTable(IDataReader reader)
    {
        var table = new DataTable();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var type = reader.GetFieldType(i);
            table.Columns.Add(reader.GetName(i), type);
        }

        while (reader.Read())
        {
            var values = new object[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                values[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
            }

            table.Rows.Add(values);
        }

        return table;
    }
}

internal sealed class SnowflakeSqlExecutionItem
{
    private SnowflakeSqlExecutionItem()
    {
    }

    public bool IsNonQuery { get; private set; }

    public string Statement { get; private set; } = string.Empty;

    public int StatementIndex { get; private set; }

    public int ResultSetIndex { get; private set; }

    public int RowsAffected { get; private set; }

    public IReadOnlyDictionary<string, object?>? Row { get; private set; }

    public DataTable? Table { get; private set; }

    public static SnowflakeSqlExecutionItem FromRow(
        string statement,
        int statementIndex,
        int resultSetIndex,
        IReadOnlyDictionary<string, object?> row)
    {
        return new SnowflakeSqlExecutionItem
        {
            Statement = statement,
            StatementIndex = statementIndex,
            ResultSetIndex = resultSetIndex,
            Row = row,
            IsNonQuery = false
        };
    }

    public static SnowflakeSqlExecutionItem FromDataTable(
        string statement,
        int statementIndex,
        int resultSetIndex,
        DataTable table)
    {
        return new SnowflakeSqlExecutionItem
        {
            Statement = statement,
            StatementIndex = statementIndex,
            ResultSetIndex = resultSetIndex,
            Table = table,
            IsNonQuery = false
        };
    }

    public static SnowflakeSqlExecutionItem FromNonQuery(string statement, int statementIndex, int rowsAffected)
    {
        return new SnowflakeSqlExecutionItem
        {
            Statement = statement,
            StatementIndex = statementIndex,
            RowsAffected = rowsAffected,
            IsNonQuery = true
        };
    }
}
