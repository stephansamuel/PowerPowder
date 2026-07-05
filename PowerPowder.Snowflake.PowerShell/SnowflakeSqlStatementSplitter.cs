using System;
using System.Collections.Generic;
using System.Text;

namespace PowerPowder.Snowflake.PowerShell;

internal static class SnowflakeSqlStatementSplitter
{
    public static IReadOnlyList<string> Split(string sql)
    {
        if (sql == null)
        {
            throw new ArgumentNullException(nameof(sql));
        }

        var results = new List<string>();
        var current = new StringBuilder();
        var inSingleQuote = false;
        var inLineComment = false;
        var inBlockComment = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var ch = sql[i];
            var next = i + 1 < sql.Length ? sql[i + 1] : '\0';

            if (inLineComment)
            {
                current.Append(ch);
                if (ch == '\n')
                {
                    inLineComment = false;
                }

                continue;
            }

            if (inBlockComment)
            {
                current.Append(ch);
                if (ch == '*' && next == '/')
                {
                    current.Append(next);
                    i++;
                    inBlockComment = false;
                }

                continue;
            }

            if (!inSingleQuote && ch == '-' && next == '-')
            {
                current.Append(ch);
                current.Append(next);
                i++;
                inLineComment = true;
                continue;
            }

            if (!inSingleQuote && ch == '/' && next == '*')
            {
                current.Append(ch);
                current.Append(next);
                i++;
                inBlockComment = true;
                continue;
            }

            if (ch == '\'')
            {
                if (inSingleQuote && next == '\'')
                {
                    current.Append(ch);
                    current.Append(next);
                    i++;
                    continue;
                }

                inSingleQuote = !inSingleQuote;
            }

            if (!inSingleQuote && ch == ';')
            {
                AddIfNotBlank(results, current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        AddIfNotBlank(results, current.ToString());
        return results;
    }

    private static void AddIfNotBlank(ICollection<string> target, string statement)
    {
        var trimmed = statement.Trim();
        if (trimmed.Length > 0)
        {
            target.Add(trimmed);
        }
    }
}
