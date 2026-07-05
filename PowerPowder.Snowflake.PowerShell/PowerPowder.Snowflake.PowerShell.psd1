@{
    RootModule = 'PowerPowder.Snowflake.PowerShell.dll'
    ModuleVersion = '0.2.0'
    GUID = 'd7052f85-8604-469c-a8d1-b7456059153a'
    Author = 'PowerPowder'
    CompanyName = 'PowerPowder'
    Copyright = '(c) PowerPowder'
    Description = 'PowerShell cmdlets for connecting to Snowflake with the Snowflake .NET connector.'
    PowerShellVersion = '7.0'
    DotNetFrameworkVersion = '4.7.2'
    CmdletsToExport = @(
        'Connect-Snowflake',
        'Disconnect-Snowflake',
        'Get-SnowflakeConnection',
        'Invoke-SnowflakeSql'
    )
    FunctionsToExport = @()
    AliasesToExport = @('Invoke-SnowflakeQuery')
    VariablesToExport = @()
}
