using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MyPlugin.Shared;

/// <summary>
/// Simple logger that writes to BZExtPluginLogs table.
/// Used for diagnostics and tracking background worker execution.
///
/// IMPORTANT: This class demonstrates the factory delegate pattern for IConfiguration.
/// See README.md section "IConfiguration is NOT available via DI" for details.
///
/// Registration in ConfigureServices (CORRECT):
/// <code>
/// services.AddSingleton&lt;PluginLogger&gt;(sp =&gt;
/// {
///     return new PluginLogger(configuration);  // Pass configuration from scope
/// });
/// </code>
///
/// DO NOT register like this (WILL FAIL):
/// <code>
/// services.AddSingleton&lt;PluginLogger&gt;();  // DI can't resolve IConfiguration
/// </code>
/// </summary>
public class PluginLogger
{
    private readonly string _connectionString;
    private readonly int _pluginId;

    public PluginLogger(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string not configured");

        // Get PluginId from configuration (set by Backend Host)
        _pluginId = configuration.GetValue<int>("System:PluginId", 0);

        Console.WriteLine($"[PluginLogger] Initialized with PluginId: {_pluginId}");
    }

    public async Task LogInformationAsync(string message, string? correlationId = null)
    {
        await LogAsync("Information", message, null, correlationId);
    }

    public async Task LogWarningAsync(string message, string? correlationId = null)
    {
        await LogAsync("Warning", message, null, correlationId);
    }

    public async Task LogErrorAsync(string message, Exception? exception = null, string? correlationId = null)
    {
        var exceptionText = exception != null
            ? $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
            : null;

        await LogAsync("Error", message, exceptionText, correlationId);
    }

    private async Task LogAsync(string logLevel, string message, string? exception, string? correlationId)
    {
        if (_pluginId == 0)
        {
            // If PluginId is not configured, skip database logging
            // This is normal in DevHost environment
            Console.WriteLine($"[PluginLogger] {logLevel}: {message}");
            return;
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO BZExtPluginLogs (PluginId, LogLevel, Message, Exception, CorrelationId)
                VALUES (@PluginId, @LogLevel, @Message, @Exception, @CorrelationId)";

            await connection.ExecuteAsync(sql, new
            {
                PluginId = _pluginId,
                LogLevel = logLevel,
                Message = message,
                Exception = exception,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            // Fallback to console if database logging fails
            Console.WriteLine($"[PluginLogger] Failed to write log to database: {ex.Message}");
            Console.WriteLine($"[PluginLogger] Original message - {logLevel}: {message}");
        }
    }
}
