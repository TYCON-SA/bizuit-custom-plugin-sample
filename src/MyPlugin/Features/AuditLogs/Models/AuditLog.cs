namespace MyPlugin.Features.AuditLogs.Models;

/// <summary>
/// Audit log entity model.
/// Used for fire-and-forget logging operations.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public required string Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Username { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to create an audit log entry.
/// </summary>
public class CreateAuditLogRequest
{
    public required string Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
}
