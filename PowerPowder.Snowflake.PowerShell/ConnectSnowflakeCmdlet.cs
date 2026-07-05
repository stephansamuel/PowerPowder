using System;
using System.Collections;
using System.Data.Common;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics.CodeAnalysis;

namespace PowerPowder.Snowflake.PowerShell;

[Cmdlet(VerbsCommunications.Connect, "Snowflake")]
[OutputType(typeof(DbConnection))]
[ExcludeFromCodeCoverage]
public sealed class ConnectSnowflakeCmdlet : PSCmdlet
{
    private readonly ISnowflakeDbConnectionFactory _connectionFactory = new SnowflakeDbConnectionFactory();

    [Parameter(Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public string Account { get; set; } = string.Empty;

    [Parameter(Mandatory = true, ParameterSetName = "Credential")]
    [ValidateNotNull]
    public PSCredential Credential { get; set; } = null!;

    [Parameter(Mandatory = true, ParameterSetName = "KeyPair")]
    [Parameter(Mandatory = true, ParameterSetName = "Sso")]
    [ValidateNotNullOrEmpty]
    public string User { get; set; } = string.Empty;

    [Parameter(Mandatory = true, ParameterSetName = "KeyPair")]
    [ValidateNotNullOrEmpty]
    public string PrivateKeyFile { get; set; } = string.Empty;

    [Parameter(Mandatory = true, ParameterSetName = "KeyPair")]
    [ValidateNotNull]
    public SecureString PrivateKeyPassphrase { get; set; } = null!;

    [Parameter(Mandatory = true, ParameterSetName = "Sso")]
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
    [ValidateNotNullOrEmpty]
    public string ConnectionName { get; set; } = "default";

    [Parameter]
    public Hashtable? AdditionalParameters { get; set; }

    protected override void ProcessRecord()
    {
        try
        {
            var request = CreateRequest();
            var connectionString = SnowflakeConnectionStringFactory.Build(request);
            var connection = _connectionFactory.Create(connectionString);

            connection.Open();
            SnowflakeConnectionStore.AddOrReplace(ConnectionName, connection);
            WriteObject(connection);
        }
        catch (Exception ex)
        {
            var error = new ErrorRecord(ex, "SnowflakeConnectFailed", ErrorCategory.OpenError, ConnectionName);
            WriteError(error);
        }
    }

    private SnowflakeConnectRequest CreateRequest()
    {
        switch (ParameterSetName)
        {
            case "Credential":
                return CreateCredentialRequest();
            case "KeyPair":
                return CreateKeyPairRequest();
            case "Sso":
                return CreateSsoRequest();
            default:
                throw new InvalidOperationException($"Unsupported parameter set '{ParameterSetName}'.");
        }
    }

    private SnowflakeConnectRequest CreateCredentialRequest()
    {
        return CreateBaseRequest(
            SnowflakeAuthMode.Credential,
            Credential.UserName,
            Credential.GetNetworkCredential().Password,
            null,
            null);
    }

    private SnowflakeConnectRequest CreateKeyPairRequest()
    {
        return CreateBaseRequest(
            SnowflakeAuthMode.KeyPair,
            User,
            null,
            PrivateKeyFile,
            SecureStringToPlainText(PrivateKeyPassphrase));
    }

    private SnowflakeConnectRequest CreateSsoRequest()
    {
        return CreateBaseRequest(
            SnowflakeAuthMode.Sso,
            User,
            null,
            null,
            null);
    }

    private SnowflakeConnectRequest CreateBaseRequest(
        SnowflakeAuthMode authMode,
        string userName,
        string? password,
        string? privateKeyFile,
        string? privateKeyPassphrase)
    {
        return new SnowflakeConnectRequest
        {
            Account = Account,
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
