using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics.CodeAnalysis;

namespace PowerPowder.Snowflake.PowerShell;

[Cmdlet(VerbsLifecycle.Invoke, "SnowflakeSql")]
[Alias("Invoke-SnowflakeQuery")]
[OutputType(typeof(PSObject), typeof(DataTable))]
[ExcludeFromCodeCoverage]
public sealed class InvokeSnowflakeSqlCmdlet : PSCmdlet
{
    private readonly ISnowflakeDbConnectionFactory _connectionFactory = new SnowflakeDbConnectionFactory();

    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    [Alias("Query")]
    public string Sql { get; set; } = string.Empty;

    [Parameter]
    public DbConnection? Connection { get; set; }

    [Parameter]
    [ValidateNotNullOrEmpty]
    public string ConnectionName { get; set; } = "default";

    [Parameter]
    public string? Account { get; set; }

    [Parameter]
    public PSCredential? Credential { get; set; }

    [Parameter]
    public string? User { get; set; }

    [Parameter]
    public string? PrivateKeyFile { get; set; }

    [Parameter]
    public SecureString? PrivateKeyPassphrase { get; set; }

    [Parameter]
    public SwitchParameter UseSso { get; set; }

    [Parameter]
    public string? Warehouse { get; set; }

    [Parameter]
    public string? Database { get; set; }

    [Parameter]
    public string? Schema { get; set; }

    [Parameter]
    public string? Role { get; set; }

    [Parameter]
    public string? Region { get; set; }

    [Parameter]
    public Hashtable? AdditionalParameters { get; set; }

    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int CommandTimeoutSeconds { get; set; }

    [Parameter]
    public SwitchParameter AsDataTable { get; set; }

    protected override void ProcessRecord()
    {
        try
        {
            var connection = ResolveConnection();
            var results = SnowflakeSqlExecutor.Execute(connection, Sql, CommandTimeoutSeconds, AsDataTable.IsPresent);

            foreach (var result in results)
            {
                if (result.IsNonQuery)
                {
                    var nonQuery = new PSObject();
                    nonQuery.Properties.Add(new PSNoteProperty("Statement", result.Statement));
                    nonQuery.Properties.Add(new PSNoteProperty("StatementIndex", result.StatementIndex));
                    nonQuery.Properties.Add(new PSNoteProperty("RowsAffected", result.RowsAffected));
                    nonQuery.Properties.Add(new PSNoteProperty("ResultType", "NonQuery"));
                    WriteObject(nonQuery);
                    continue;
                }

                if (result.Table != null)
                {
                    WriteObject(result.Table);
                    continue;
                }

                if (result.Row != null)
                {
                    var row = new PSObject();
                    foreach (var property in result.Row)
                    {
                        row.Properties.Add(new PSNoteProperty(property.Key, property.Value));
                    }

                    row.Properties.Add(new PSNoteProperty("StatementIndex", result.StatementIndex));
                    row.Properties.Add(new PSNoteProperty("ResultSetIndex", result.ResultSetIndex));
                    WriteObject(row);
                }
            }
        }
        catch (Exception ex)
        {
            var error = new ErrorRecord(ex, "SnowflakeSqlExecutionFailed", ErrorCategory.OperationStopped, Sql);
            WriteError(error);
        }
    }

    private DbConnection ResolveConnection()
    {
        if (Connection != null)
        {
            return Connection;
        }

        if (SnowflakeConnectionStore.TryGet(ConnectionName, out var existing))
        {
            return existing;
        }

        if (!CanPerformInlineConnect())
        {
            throw new InvalidOperationException(
                $"Connection '{ConnectionName}' does not exist. Provide a connection, run Connect-Snowflake, or pass inline connect parameters.");
        }

        var request = BuildInlineConnectRequest();
        var connectionString = SnowflakeConnectionStringFactory.Build(request);
        var connection = _connectionFactory.Create(connectionString);
        connection.Open();
        SnowflakeConnectionStore.AddOrReplace(ConnectionName, connection);
        return connection;
    }

    private bool CanPerformInlineConnect()
    {
        return !string.IsNullOrWhiteSpace(Account) &&
            (Credential != null ||
             (!string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(PrivateKeyFile) && PrivateKeyPassphrase != null) ||
             (!string.IsNullOrWhiteSpace(User) && UseSso.IsPresent));
    }

    private SnowflakeConnectRequest BuildInlineConnectRequest()
    {
        if (!string.IsNullOrWhiteSpace(Account) && Credential != null)
        {
            return BuildBaseInlineRequest(SnowflakeAuthMode.Credential, Credential.UserName, Credential.GetNetworkCredential().Password, null, null);
        }

        if (!string.IsNullOrWhiteSpace(Account) && !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(PrivateKeyFile) && PrivateKeyPassphrase != null)
        {
            return BuildBaseInlineRequest(
                SnowflakeAuthMode.KeyPair,
                User!,
                null,
                PrivateKeyFile!,
                SecureStringToPlainText(PrivateKeyPassphrase));
        }

        if (!string.IsNullOrWhiteSpace(Account) && !string.IsNullOrWhiteSpace(User) && UseSso.IsPresent)
        {
            return BuildBaseInlineRequest(SnowflakeAuthMode.Sso, User!, null, null, null);
        }

        throw new InvalidOperationException("Inline connection parameters are incomplete.");
    }

    private SnowflakeConnectRequest BuildBaseInlineRequest(
        SnowflakeAuthMode authMode,
        string userName,
        string? password,
        string? privateKeyFile,
        string? privateKeyPassphrase)
    {
        return new SnowflakeConnectRequest
        {
            Account = Account ?? string.Empty,
            AuthMode = authMode,
            UserName = userName,
            Password = password,
            PrivateKeyFile = privateKeyFile,
            PrivateKeyPassphrase = privateKeyPassphrase,
            Warehouse = Warehouse,
            Database = Database,
            Schema = Schema,
            Role = Role,
            Region = Region,
            AdditionalParameters = AdditionalParameters
        };
    }

    private static string SecureStringToPlainText(SecureString value)
    {
        var ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(value);
            return Marshal.PtrToStringUni(ptr) ?? string.Empty;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}
