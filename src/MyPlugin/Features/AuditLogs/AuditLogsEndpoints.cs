using Bizuit.Backend.Abstractions;
using Bizuit.Backend.Core.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyPlugin.Features.AuditLogs.Models;

namespace MyPlugin.Features.AuditLogs;

/// <summary>
/// Minimal API endpoints for AuditLogs.
///
/// [NoTransaction] DEMO:
/// By default, POST/PUT/DELETE requests are wrapped in a transaction.
/// Use .NoTransaction() extension to opt-out for:
/// - Fire-and-forget logging operations
/// - High-performance endpoints where consistency is not critical
/// - Audit logs that should persist even if the main operation fails
///
/// Routes are prefixed with /api/plugins/{name}/{version}/
/// Example: POST /api/plugins/myplugin/v1/audit-logs
/// </summary>
public static class AuditLogsEndpoints
{
    public static void Map(IPluginEndpointBuilder endpoints)
    {
        // ============================================
        // PUBLIC ENDPOINTS (read-only, no transaction)
        // ============================================

        // GET /audit-logs - Get recent logs
        endpoints.MapGet("audit-logs", GetRecent);

        // GET /audit-logs/search - Search logs with filters
        endpoints.MapGet("audit-logs/search", Search);

        // ============================================
        // [NoTransaction] ENDPOINTS
        // ============================================

        // POST /audit-logs - Create log entry WITHOUT transaction
        // This is useful for:
        // 1. Fire-and-forget logging
        // 2. Audit entries that should persist even if main operation fails
        // 3. High-performance logging without transaction overhead
        endpoints.MapPost("audit-logs", Create)
            .NoTransaction();  // <- Key: Opt-out of automatic transaction!

        // POST /audit-logs/batch - Create multiple log entries WITHOUT transaction
        endpoints.MapPost("audit-logs/batch", CreateBatch)
            .NoTransaction();
    }

    // --- Read Endpoints (no transaction for GET by default) ---

    private static async Task<IResult> GetRecent(
        AuditLogsRepository repository,
        [FromQuery] int? limit = 100)
    {
        var logs = await repository.GetRecentAsync(limit ?? 100);
        return Results.Ok(logs);
    }

    private static async Task<IResult> Search(
        AuditLogsRepository repository,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? username = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? limit = 100)
    {
        var logs = await repository.SearchAsync(
            action, entityType, username, fromDate, toDate, limit ?? 100);
        return Results.Ok(logs);
    }

    // --- NoTransaction Endpoints ---

    /// <summary>
    /// Create a single audit log entry.
    ///
    /// WHY NoTransaction?
    /// - Audit logs should persist even if the calling operation fails
    /// - No rollback needed for logging operations
    /// - Better performance (no transaction overhead)
    /// </summary>
    private static async Task<IResult> Create(
        CreateAuditLogRequest request,
        AuditLogsRepository repository,
        BizuitUserContext? user)  // Optional: may be null for anonymous requests
    {
        try
        {
            var logId = await repository.CreateAsync(request, user?.Username);
            return Results.Created($"/audit-logs/{logId}", new
            {
                logId,
                message = "Audit log created (no transaction)"
            });
        }
        catch (Exception ex)
        {
            // Even on error, we return OK to not interrupt the caller
            // This is fire-and-forget logging
            return Results.Ok(new
            {
                error = ex.Message,
                logged = false
            });
        }
    }

    /// <summary>
    /// Create multiple audit log entries in batch.
    /// Each entry is inserted independently (no all-or-nothing).
    ///
    /// WHY NoTransaction?
    /// - Partial success is acceptable for batch logging
    /// - Better performance for bulk inserts
    /// - Each log entry is independent
    /// </summary>
    private static async Task<IResult> CreateBatch(
        List<CreateAuditLogRequest> requests,
        AuditLogsRepository repository,
        BizuitUserContext? user)
    {
        var results = new List<object>();
        var username = user?.Username;

        foreach (var request in requests)
        {
            try
            {
                var logId = await repository.CreateAsync(request, username);
                results.Add(new { logId, success = true });
            }
            catch (Exception ex)
            {
                results.Add(new { error = ex.Message, success = false });
            }
        }

        return Results.Ok(new
        {
            total = requests.Count,
            successful = results.Count(r => ((dynamic)r).success),
            results
        });
    }
}
