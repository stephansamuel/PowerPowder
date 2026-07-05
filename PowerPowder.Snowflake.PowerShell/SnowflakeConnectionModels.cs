using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace PowerPowder.Snowflake.PowerShell;

internal enum SnowflakeAuthMode
{
    Credential,
    KeyPair,
    Sso
}

internal sealed class SnowflakeConnectRequest
{
    public string Account { get; set; } = string.Empty;

    public SnowflakeAuthMode AuthMode { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? Password { get; set; }

    public string? PrivateKeyFile { get; set; }

    public string? PrivateKeyPassphrase { get; set; }

    public string? Warehouse { get; set; }

    public string? Database { get; set; }

    public string? Schema { get; set; }

    public string? Role { get; set; }

    public string? Region { get; set; }

    public Hashtable? AdditionalParameters { get; set; }

    public IDictionary<string, object> ToConnectionProperties()
    {
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["account"] = Account,
            ["user"] = UserName
        };

        switch (AuthMode)
        {
            case SnowflakeAuthMode.Credential:
                values["password"] = Password ?? string.Empty;
                break;
            case SnowflakeAuthMode.KeyPair:
                values["private_key_file"] = PrivateKeyFile ?? string.Empty;
                values["private_key_pwd"] = PrivateKeyPassphrase ?? string.Empty;
                break;
            case SnowflakeAuthMode.Sso:
                values["authenticator"] = "externalbrowser";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (!string.IsNullOrWhiteSpace(Warehouse))
        {
            values["warehouse"] = Warehouse!;
        }

        if (!string.IsNullOrWhiteSpace(Database))
        {
            values["db"] = Database!;
        }

        if (!string.IsNullOrWhiteSpace(Schema))
        {
            values["schema"] = Schema!;
        }

        if (!string.IsNullOrWhiteSpace(Role))
        {
            values["role"] = Role!;
        }

        if (!string.IsNullOrWhiteSpace(Region))
        {
            values["region"] = Region!;
        }

        if (AdditionalParameters != null)
        {
            foreach (DictionaryEntry entry in AdditionalParameters)
            {
                if (entry.Key is string key && !string.IsNullOrWhiteSpace(key) && entry.Value != null)
                {
                    values[key] = entry.Value;
                }
            }
        }

        return values;
    }
}

internal interface ISnowflakeDbConnectionFactory
{
    DbConnection Create(string connectionString);
}
