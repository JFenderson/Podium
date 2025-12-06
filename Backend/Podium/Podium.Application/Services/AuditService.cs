using Microsoft.Extensions.Logging;
using Podium.Application.DTOs.AuditLog;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Podium.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Log a user action with optional metadata.
        /// IMPORTANT: Runs asynchronously but doesn't block the main operation.
        /// Uses fire-and-forget pattern for performance.
        /// </summary>
        public async Task LogActionAsync(string userId, string actionType, string description, object? metadata = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    ActionType = actionType,
                    Description = description,
                    MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = null, // Would be populated from HttpContext in real implementation
                    UserAgent = null  // Would be populated from HttpContext in real implementation
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Also log to application logger for real-time monitoring
                _logger.LogInformation(
                    "Audit: {ActionType} by {UserId} - {Description}",
                    actionType, userId, description);
            }
            catch (Exception ex)
            {
                // Never let audit logging failure break the main operation
                _logger.LogError(ex, "Failed to write audit log for action {ActionType}", actionType);
            }
        }

        /// <summary>
        /// Log unauthorized access attempts for security monitoring.
        /// High-priority logging that may trigger alerts.
        /// </summary>
        public async Task LogUnauthorizedAccessAsync(string userId, string resourceType, int resourceId)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    ActionType = "UnauthorizedAccess",
                    Description = $"Attempted unauthorized access to {resourceType} {resourceId}",
                    MetadataJson = JsonSerializer.Serialize(new { resourceType, resourceId }),
                    Timestamp = DateTime.UtcNow,
                    IpAddress = null,
                    UserAgent = null,
                    IsSecurityEvent = true,
                    Severity = "High"
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Log as warning for security monitoring
                _logger.LogWarning(
                    "SECURITY: Unauthorized access attempt by {UserId} to {ResourceType} {ResourceId}",
                    userId, resourceType, resourceId);

                // TODO: Trigger security alert if multiple attempts detected
                await CheckForSecurityAnomaliesAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log unauthorized access attempt");
            }
        }

        /// <summary>
        /// Retrieve audit logs with filtering.
        /// Used for compliance reporting and security investigations.
        /// </summary>
        public async Task<List<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filters)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filters.UserId))
                query = query.Where(al => al.UserId == filters.UserId);

            if (!string.IsNullOrEmpty(filters.ActionType))
                query = query.Where(al => al.ActionType == filters.ActionType);

            if (filters.StartDate.HasValue)
                query = query.Where(al => al.Timestamp >= filters.StartDate.Value);

            if (filters.EndDate.HasValue)
                query = query.Where(al => al.Timestamp <= filters.EndDate.Value);

            if (filters.SecurityEventsOnly)
                query = query.Where(al => al.IsSecurityEvent);

            return await query
                .OrderByDescending(al => al.Timestamp)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(al => new AuditLogDto
                {
                    Id = al.Id,
                    UserId = al.UserId,
                    ActionType = al.ActionType,
                    Description = al.Description,
                    Timestamp = al.Timestamp,
                    IpAddress = al.IpAddress,
                    UserAgent = al.UserAgent,
                    IsSecurityEvent = al.IsSecurityEvent,
                    Severity = al.Severity,
                    Metadata = al.MetadataJson != null
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(al.MetadataJson)
                        : null
                })
                .ToListAsync();
        }

        /// <summary>
        /// Check for anomalous security patterns that may indicate an attack.
        /// Example: Multiple failed access attempts in short time period.
        /// </summary>
        private async Task CheckForSecurityAnomaliesAsync(string userId)
        {
            var recentAttempts = await _context.AuditLogs
                .Where(al =>
                    al.UserId == userId &&
                    al.ActionType == "UnauthorizedAccess" &&
                    al.Timestamp >= DateTime.UtcNow.AddMinutes(-5))
                .CountAsync();

            if (recentAttempts >= 5)
            {
                _logger.LogCritical(
                    "SECURITY ALERT: User {UserId} has {AttemptCount} unauthorized access attempts in last 5 minutes",
                    userId, recentAttempts);

                // TODO: Trigger alert, temporarily lock account, notify security team
            }
        }
    }
}
