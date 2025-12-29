using Bizuit.Backend.Core.Database;
using Microsoft.AspNetCore.Http;
using MyPlugin.Features.AuditLogs.Models;

namespace MyPlugin.Features.AuditLogs;

/// <summary>
/// Repository for AuditLogs using SafeQueryBuilder.
/// SQL Injection is IMPOSSIBLE.
///
/// This repository is used with [NoTransaction] endpoints
/// for fire-and-forget logging operations.
/// </summary>
public class AuditLogsRepository : SafeRepository<AuditLog>
{
    protected override string TableName => "AuditLogs";

    public AuditLogsRepository(IConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    /// <summary>
    /// Get recent audit logs.
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int limit = 100)
    {
        var query = Query()
            .OrderByDescending("CreatedAt")
            .Take(limit);

        return await ExecuteAsync(query);
    }

    /// <summary>
    /// Search audit logs by action or entity.
    /// </summary>
    public async Task<IEnumerable<AuditLog>> SearchAsync(
        string? action,
        string? entityType,
        string? username,
        DateTime? fromDate,
        DateTime? toDate,
        int limit = 100)
    {
        var query = Query();

        if (!string.IsNullOrEmpty(action))
        {
            query.WhereLike("Action", action);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query.WhereEquals("EntityType", entityType);
        }

        if (!string.IsNullOrEmpty(username))
        {
            query.WhereEquals("Username", username);
        }

        if (fromDate.HasValue)
        {
            query.WhereGreaterOrEqual("CreatedAt", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query.WhereLessOrEqual("CreatedAt", toDate.Value);
        }

        query.OrderByDescending("CreatedAt").Take(limit);

        return await ExecuteAsync(query);
    }

    /// <summary>
    /// Create a new audit log entry.
    /// This is typically called from a [NoTransaction] endpoint.
    /// </summary>
    public async Task<int> CreateAsync(CreateAuditLogRequest request, string? username)
    {
        var insert = Insert()
            .Set("Action", request.Action)
            .Set("EntityType", request.EntityType)
            .Set("EntityId", request.EntityId)
            .Set("Username", username)
            .Set("Details", request.Details)
            .Set("CreatedAt", DateTime.UtcNow);

        return await ExecuteWithIdentityAsync(insert);
    }
}
