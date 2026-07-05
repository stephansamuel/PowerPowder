using System;
using System.Data.Common;
using System.Management.Automation;
using System.Diagnostics.CodeAnalysis;

namespace PowerPowder.Snowflake.PowerShell;

[Cmdlet(VerbsCommunications.Disconnect, "Snowflake")]
[ExcludeFromCodeCoverage]
public sealed class DisconnectSnowflakeCmdlet : PSCmdlet
{
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string ConnectionName { get; set; } = "default";

    [Parameter]
    public SwitchParameter Force { get; set; }

    protected override void ProcessRecord()
    {
        if (!SnowflakeConnectionStore.Remove(ConnectionName, out var connection))
        {
            WriteVerbose($"Connection '{ConnectionName}' was not found.");
            return;
        }

        try
        {
            if (Force)
            {
                connection.Dispose();
            }
            else
            {
                connection.Close();
                connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            var error = new ErrorRecord(ex, "SnowflakeDisconnectFailed", ErrorCategory.CloseError, ConnectionName);
            WriteError(error);
        }
    }
}

[Cmdlet(VerbsCommon.Get, "SnowflakeConnection")]
[OutputType(typeof(PSObject))]
[ExcludeFromCodeCoverage]
public sealed class GetSnowflakeConnectionCmdlet : PSCmdlet
{
    [Parameter]
    public string? ConnectionName { get; set; }

    protected override void ProcessRecord()
    {
        var snapshot = SnowflakeConnectionStore.Snapshot();

        if (!string.IsNullOrWhiteSpace(ConnectionName))
        {
            if (!snapshot.TryGetValue(ConnectionName!, out var connection))
            {
                var error = new ErrorRecord(
                    new InvalidOperationException($"Connection '{ConnectionName}' does not exist."),
                    "SnowflakeConnectionNotFound",
                    ErrorCategory.ObjectNotFound,
                    ConnectionName);
                WriteError(error);
                return;
            }

            WriteObject(ToRecord(ConnectionName!, connection));
            return;
        }

        foreach (var item in snapshot)
        {
            WriteObject(ToRecord(item.Key, item.Value));
        }
    }

    private static PSObject ToRecord(string name, DbConnection connection)
    {
        var output = new PSObject();
        output.Properties.Add(new PSNoteProperty("ConnectionName", name));
        output.Properties.Add(new PSNoteProperty("State", connection.State.ToString()));
        output.Properties.Add(new PSNoteProperty("Database", connection.Database));
        output.Properties.Add(new PSNoteProperty("DataSource", connection.DataSource));
        return output;
    }
}
