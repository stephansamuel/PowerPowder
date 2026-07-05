using System;
using System.Collections;
using System.Data.Common;
using PowerPowder.Snowflake.PowerShell;

namespace PowerPowder.Snowflake.PowerShell.Tests;

public class SnowflakeConnectionStringFactoryTests
{
    [Fact]
    public void Build_WithCredentialAuth_ContainsExpectedProperties()
    {
        var request = new SnowflakeConnectRequest
        {
            Account = "xy12345",
            AuthMode = SnowflakeAuthMode.Credential,
            UserName = "alice",
            Password = "secret",
            Warehouse = "COMPUTE_WH",
            Database = "DB1",
            Schema = "PUBLIC",
            Role = "SYSADMIN",
            Region = "us-west-2"
        };

        var connectionString = SnowflakeConnectionStringFactory.Build(request);
        var values = Parse(connectionString);

        Assert.Equal("xy12345", values["account"]);
        Assert.Equal("alice", values["user"]);
        Assert.Equal("secret", values["password"]);
        Assert.Equal("COMPUTE_WH", values["warehouse"]);
        Assert.Equal("DB1", values["db"]);
        Assert.Equal("PUBLIC", values["schema"]);
        Assert.Equal("SYSADMIN", values["role"]);
        Assert.Equal("us-west-2", values["region"]);
    }

    [Fact]
    public void Build_WithKeyPairAuth_ContainsKeyPairFields()
    {
        var request = new SnowflakeConnectRequest
        {
            Account = "xy12345",
            AuthMode = SnowflakeAuthMode.KeyPair,
            UserName = "alice",
            PrivateKeyFile = "/tmp/private_key.p8",
            PrivateKeyPassphrase = "passphrase"
        };

        var values = Parse(SnowflakeConnectionStringFactory.Build(request));
        Assert.Equal("/tmp/private_key.p8", values["private_key_file"]);
        Assert.Equal("passphrase", values["private_key_pwd"]);
    }

    [Fact]
    public void Build_WithSsoAuth_UsesExternalBrowserAuthenticator()
    {
        var request = new SnowflakeConnectRequest
        {
            Account = "xy12345",
            AuthMode = SnowflakeAuthMode.Sso,
            UserName = "alice"
        };

        var values = Parse(SnowflakeConnectionStringFactory.Build(request));
        Assert.Equal("externalbrowser", values["authenticator"]);
    }

    [Fact]
    public void Build_WithAdditionalParameters_AllowsOverrides()
    {
        var request = new SnowflakeConnectRequest
        {
            Account = "xy12345",
            AuthMode = SnowflakeAuthMode.Credential,
            UserName = "alice",
            Password = "secret",
            AdditionalParameters = new Hashtable
            {
                ["warehouse"] = "WH_OVERRIDE",
                ["insecuremode"] = "true"
            }
        };

        var values = Parse(SnowflakeConnectionStringFactory.Build(request));
        Assert.Equal("WH_OVERRIDE", values["warehouse"]);
        Assert.Equal("true", values["insecuremode"]);
    }

    [Fact]
    public void Build_WithNullRequest_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SnowflakeConnectionStringFactory.Build(null!));
    }

    private static DbConnectionStringBuilder Parse(string connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };
        return builder;
    }
}
