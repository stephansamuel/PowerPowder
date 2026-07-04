# PowerPowder

> A syndicated PowerShell wrapper for Snowflake connections

## What Is It?

PowerPowder is a series of PowerShell cmdlets to connect to and query Snowflake data stores. The project aims to have two working cmdlets:

- `Connect-Snowflake` to open a connection to Snowflake using either: a credential object for user name and password; a user name, RSA key file name and a passphrase; or a user name for SSO. The account name must be provided, and standard account parameters may be provided, such as warehouse, role or database, as well as any other connection flags. We aim to return a connection object and to cache the connection object in a way that successive calls to library functions can access it without a specific handle.
- `Invoke-SnowflakeSql` to run a query against an existing Snowflake connection and return the result in an ordinary object format just as would have been returned from a similar query through `Invoke-Sqlcmd`, including handling of multiple-query statements. Non-query statements should return with the same paradigm as one would receive from Snowsight. A connection object can be specified, or used directly from the environment if a connection has already been made. Alternately, credentials can be provided directly as parameters to the command and a connection will be transparently attempted before the query is resolved.

The full package should be available to download from any ordinary source as a single module that will automatically install normally after download. Users should be able to `Install-Module` and begin working immediately. Any dependencies should be resolved automatically as part of the normal installation process.

## Why Bother?

**tl;dr** if you need to run 68 SQL files in a local directory, there's no easier way to do it.

Strong corporate data platforms aren't just for technology companies anymore. While many technology companies, especially less business-savvy smaller ones, use non-Windows platforms, the vast majority of larger organizations run Windows on the desktop. Python, or another platform allowing shell-based access to Snowflake, may not be available or permitted by policy, leaving no way to perform shell-based Snowflake queries.

PowerShell provides a rich, first-rate platform for casual CLI-based work. It is feature-complete, offering full native access to the underlying platform as well as a mature set of support libraries that competes with or exceeds any other scripting platform. Further, PowerShell is more modern and less provincial than many competitors, _e.g.,_ `Get-Content` _vs._ `cat` or 3 lines of Python. Further, PowerShell is now available on many linux distributions, making it an attractive cross-platform option for C-style shell scripting without suffering 

This set of cmdlets puts the ability to run Snowflake back into the hands of Windows users.
