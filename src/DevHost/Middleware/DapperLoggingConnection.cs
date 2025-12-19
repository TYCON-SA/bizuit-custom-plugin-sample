using Bizuit.Backend.Core.Database;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace DevHost.Middleware;

/// <summary>
/// Factory that creates logging-enabled connections for SQL query debugging.
/// </summary>
public class LoggingConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    public LoggingConnectionFactory(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public IDbConnection CreateConnection()
    {
        return new DapperLoggingConnection(_connectionString, _logger);
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var conn = new DapperLoggingConnection(_connectionString, _logger);
        await conn.OpenAsync();
        return conn;
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync()
    {
        return await CreateConnectionAsync();
    }
}

/// <summary>
/// Wrapper around SqlConnection that logs all SQL commands executed via Dapper.
/// Logs: SQL query, parameters, execution time, and rows affected.
/// Only enabled in Development mode.
/// </summary>
public class DapperLoggingConnection : IDbConnection
{
    private readonly SqlConnection _connection;
    private readonly ILogger _logger;
    private readonly bool _enableLogging;

    public DapperLoggingConnection(string connectionString, ILogger logger, bool enableLogging = true)
    {
        _connection = new SqlConnection(connectionString);
        _logger = logger;
        _enableLogging = enableLogging;
    }

    public string ConnectionString
    {
        get => _connection.ConnectionString;
        set => _connection.ConnectionString = value;
    }

    public int ConnectionTimeout => _connection.ConnectionTimeout;
    public string Database => _connection.Database;
    public ConnectionState State => _connection.State;

    public IDbTransaction BeginTransaction() => _connection.BeginTransaction();
    public IDbTransaction BeginTransaction(IsolationLevel il) => _connection.BeginTransaction(il);
    public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);
    public void Close() => _connection.Close();

    public IDbCommand CreateCommand()
    {
        var command = _connection.CreateCommand();
        return _enableLogging
            ? new LoggingDbCommand(command, _logger)
            : command;
    }

    public void Open() => _connection.Open();

    public async Task OpenAsync()
    {
        await _connection.OpenAsync();
    }

    public void Dispose() => _connection.Dispose();
}

/// <summary>
/// Wrapper around IDbCommand that logs SQL execution details.
/// </summary>
internal class LoggingDbCommand : IDbCommand
{
    private readonly IDbCommand _command;
    private readonly ILogger _logger;

    public LoggingDbCommand(IDbCommand command, ILogger logger)
    {
        _command = command;
        _logger = logger;
    }

    public string CommandText
    {
        get => _command.CommandText;
        set => _command.CommandText = value;
    }

    public int CommandTimeout
    {
        get => _command.CommandTimeout;
        set => _command.CommandTimeout = value;
    }

    public CommandType CommandType
    {
        get => _command.CommandType;
        set => _command.CommandType = value;
    }

    public IDbConnection? Connection
    {
        get => _command.Connection;
        set => _command.Connection = value;
    }

    public IDataParameterCollection Parameters => _command.Parameters;

    public IDbTransaction? Transaction
    {
        get => _command.Transaction;
        set => _command.Transaction = value;
    }

    public UpdateRowSource UpdatedRowSource
    {
        get => _command.UpdatedRowSource;
        set => _command.UpdatedRowSource = value;
    }

    public void Cancel() => _command.Cancel();

    public IDbDataParameter CreateParameter() => _command.CreateParameter();

    public int ExecuteNonQuery()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = _command.ExecuteNonQuery();
            sw.Stop();
            LogQuery(sw.ElapsedMilliseconds, result);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogQueryError(sw.ElapsedMilliseconds, ex);
            throw;
        }
    }

    public IDataReader ExecuteReader() => ExecuteDbDataReader(CommandBehavior.Default);

    public IDataReader ExecuteReader(CommandBehavior behavior) => ExecuteDbDataReader(behavior);

    private IDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var reader = _command.ExecuteReader(behavior);
            sw.Stop();
            LogQuery(sw.ElapsedMilliseconds, null);
            return reader;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogQueryError(sw.ElapsedMilliseconds, ex);
            throw;
        }
    }

    public object? ExecuteScalar()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = _command.ExecuteScalar();
            sw.Stop();
            LogQuery(sw.ElapsedMilliseconds, null, result);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogQueryError(sw.ElapsedMilliseconds, ex);
            throw;
        }
    }

    public void Prepare() => _command.Prepare();

    public void Dispose() => _command.Dispose();

    private void LogQuery(long elapsedMs, int? rowsAffected, object? scalarResult = null)
    {
        var paramLog = FormatParameters();
        var resultLog = rowsAffected.HasValue
            ? $"RowsAffected={rowsAffected.Value}"
            : scalarResult != null
                ? $"ScalarResult={scalarResult}"
                : "Query executed";

        _logger.LogInformation(
            "[SQL] {ElapsedMs}ms | {Result}\n{CommandText}\n{Parameters}",
            elapsedMs,
            resultLog,
            _command.CommandText,
            paramLog);
    }

    private void LogQueryError(long elapsedMs, Exception ex)
    {
        var paramLog = FormatParameters();
        _logger.LogError(
            ex,
            "[SQL ERROR] {ElapsedMs}ms | {Error}\n{CommandText}\n{Parameters}",
            elapsedMs,
            ex.Message,
            _command.CommandText,
            paramLog);
    }

    private string FormatParameters()
    {
        if (_command.Parameters.Count == 0)
            return "No parameters";

        var parameters = new List<string>();
        foreach (IDbDataParameter param in _command.Parameters)
        {
            var value = param.Value == null || param.Value == DBNull.Value
                ? "NULL"
                : param.Value.ToString();
            parameters.Add($"  {param.ParameterName} = {value} ({param.DbType})");
        }

        return string.Join("\n", parameters);
    }
}
