#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace PowerPowder.Snowflake.PowerShell.Tests.TestDoubles;

internal sealed class FakeDbConnection : DbConnection
{
    private readonly Dictionary<string, FakeCommandPlan> _plans;
    private ConnectionState _state = ConnectionState.Closed;

    public FakeDbConnection(IEnumerable<KeyValuePair<string, FakeCommandPlan>> plans)
    {
        _plans = plans.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    public bool WasDisposed { get; private set; }

    public List<FakeDbCommand> CreatedCommands { get; } = new List<FakeDbCommand>();

    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database { get; } = "TEST_DB";

    public override string DataSource { get; } = "fake-source";

    public override string ServerVersion { get; } = "1.0";

    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close()
    {
        _state = ConnectionState.Closed;
    }

    public override void Open()
    {
        _state = ConnectionState.Open;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        throw new NotSupportedException();
    }

    protected override DbCommand CreateDbCommand()
    {
        var command = new FakeDbCommand(this, _plans);
        CreatedCommands.Add(command);
        return command;
    }

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        base.Dispose(disposing);
    }
}

internal sealed class FakeDbCommand : DbCommand
{
    private readonly FakeDbConnection _connection;
    private readonly Dictionary<string, FakeCommandPlan> _plans;

    public FakeDbCommand(FakeDbConnection connection, Dictionary<string, FakeCommandPlan> plans)
    {
        _connection = connection;
        _plans = plans;
    }

    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; } = CommandType.Text;

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection DbConnection
    {
        get => _connection;
        set => throw new NotSupportedException();
    }

    protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();

    protected override DbTransaction DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery()
    {
        throw new NotSupportedException();
    }

    public override object ExecuteScalar()
    {
        throw new NotSupportedException();
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter()
    {
        throw new NotSupportedException();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        if (!_plans.TryGetValue(CommandText, out var plan))
        {
            throw new InvalidOperationException("No fake plan registered for command: " + CommandText);
        }

        return new FakeDbDataReader(plan);
    }
}

internal sealed class FakeDbDataReader : DbDataReader
{
    private readonly FakeCommandPlan _plan;
    private int _resultSetIndex;
    private int _rowIndex = -1;

    public FakeDbDataReader(FakeCommandPlan plan)
    {
        _plan = plan;
    }

    private FakeResultSet CurrentResultSet =>
        _resultSetIndex >= 0 && _resultSetIndex < _plan.ResultSets.Count
            ? _plan.ResultSets[_resultSetIndex]
            : null;

    private object[] CurrentRow =>
        CurrentResultSet != null && _rowIndex >= 0 && _rowIndex < CurrentResultSet.Rows.Count
            ? CurrentResultSet.Rows[_rowIndex]
            : null;

    public override object this[int ordinal] => GetValue(ordinal);

    public override object this[string name] => GetValue(GetOrdinal(name));

    public override int Depth => 0;

    public override int FieldCount => CurrentResultSet?.Columns.Count ?? 0;

    public override bool HasRows => CurrentResultSet != null && CurrentResultSet.Rows.Count > 0;

    public override bool IsClosed => false;

    public override int RecordsAffected => _plan.RecordsAffected;

    public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);

    public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);

    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
    {
        throw new NotSupportedException();
    }

    public override char GetChar(int ordinal) => (char)GetValue(ordinal);

    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
    {
        throw new NotSupportedException();
    }

    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

    public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);

    public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);

    public override double GetDouble(int ordinal) => (double)GetValue(ordinal);

    public override Type GetFieldType(int ordinal)
    {
        return CurrentResultSet?.Columns[ordinal].Type ?? typeof(object);
    }

    public override float GetFloat(int ordinal) => (float)GetValue(ordinal);

    public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);

    public override short GetInt16(int ordinal) => (short)GetValue(ordinal);

    public override int GetInt32(int ordinal) => (int)GetValue(ordinal);

    public override long GetInt64(int ordinal) => (long)GetValue(ordinal);

    public override string GetName(int ordinal)
    {
        return CurrentResultSet?.Columns[ordinal].Name ?? string.Empty;
    }

    public override int GetOrdinal(string name)
    {
        var columns = CurrentResultSet?.Columns;
        if (columns == null)
        {
            throw new IndexOutOfRangeException(name);
        }

        for (var i = 0; i < columns.Count; i++)
        {
            if (string.Equals(columns[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException(name);
    }

    public override string GetString(int ordinal) => (string)GetValue(ordinal);

    public override object GetValue(int ordinal)
    {
        if (CurrentRow == null)
        {
            throw new InvalidOperationException("No current row is available.");
        }

        return CurrentRow[ordinal] ?? DBNull.Value;
    }

    public override int GetValues(object[] values)
    {
        if (CurrentRow == null)
        {
            return 0;
        }

        var count = Math.Min(values.Length, CurrentRow.Length);
        Array.Copy(CurrentRow, values, count);
        return count;
    }

    public override bool IsDBNull(int ordinal)
    {
        return GetValue(ordinal) == DBNull.Value;
    }

    public override bool NextResult()
    {
        if (_plan.ResultSets.Count == 0)
        {
            return false;
        }

        _resultSetIndex++;
        _rowIndex = -1;
        return _resultSetIndex < _plan.ResultSets.Count;
    }

    public override bool Read()
    {
        if (CurrentResultSet == null)
        {
            return false;
        }

        var nextIndex = _rowIndex + 1;
        if (nextIndex < CurrentResultSet.Rows.Count)
        {
            _rowIndex = nextIndex;
            return true;
        }

        return false;
    }

    public override IEnumerator GetEnumerator()
    {
        while (Read())
        {
            yield return this;
        }
    }
}

internal sealed class FakeDbParameterCollection : DbParameterCollection
{
    public override int Add(object value) => 0;

    public override void AddRange(Array values)
    {
    }

    public override void Clear()
    {
    }

    public override bool Contains(object value) => false;

    public override bool Contains(string value) => false;

    public override void CopyTo(Array array, int index)
    {
    }

    public override int Count => 0;

    public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

    protected override DbParameter GetParameter(int index) => throw new IndexOutOfRangeException();

    protected override DbParameter GetParameter(string parameterName) => throw new IndexOutOfRangeException();

    public override int IndexOf(object value) => -1;

    public override int IndexOf(string parameterName) => -1;

    public override void Insert(int index, object value)
    {
    }

    public override bool IsFixedSize => false;

    public override bool IsReadOnly => false;

    public override bool IsSynchronized => false;

    public override void Remove(object value)
    {
    }

    public override void RemoveAt(int index)
    {
    }

    public override void RemoveAt(string parameterName)
    {
    }

    protected override void SetParameter(int index, DbParameter value)
    {
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
    }

    public override object SyncRoot => this;
}

internal sealed class FakeCommandPlan
{
    public List<FakeResultSet> ResultSets { get; } = new List<FakeResultSet>();

    public int RecordsAffected { get; set; }
}

internal sealed class FakeResultSet
{
    public List<FakeColumn> Columns { get; } = new List<FakeColumn>();

    public List<object[]> Rows { get; } = new List<object[]>();
}

internal sealed class FakeColumn
{
    public FakeColumn(string name, Type type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; }

    public Type Type { get; }
}
#nullable restore
