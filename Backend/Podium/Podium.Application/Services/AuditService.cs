using Microsoft.EntityFrameworkCore; 
using Microsoft.Extensions.Logging;
using Podium.Application.DTOs.AuditLog;
using Podium.Core.Entities;
using Podium.Infrastructure.Data;
using Podium.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var auditLog = new AuditLog
            {
                ApplicationUserId = userId,
                ActionType = actionType,
                Description = description,
                Timestamp = DateTime.UtcNow,
                MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Check for anomalous patterns
            await CheckForSecurityAnomaliesAsync(userId);
        }

        /// <summary>
        /// Log security-related events with optional metadata.
        /// </summary>
        public async Task LogSecurityEventAsync(string userId, string actionType, string description, string severity = "Medium", object? metadata = null)
        {
            var auditLog = new AuditLog
            {
                ApplicationUserId = userId,
                ActionType = actionType,
                Description = description,
                Timestamp = DateTime.UtcNow,
                IsSecurityEvent = true,
                Severity = severity,
                MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "SECURITY EVENT: {ActionType} by {UserId} - {Description}",
                actionType, userId, description);
        }

        /// <summary>
        /// Retrieve audit logs with filtering.
        /// Used for compliance reporting and security investigations.
        /// </summary>
        public async Task<List<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filters)
        {
            var query = _context.AuditLogs.AsQueryable();

            // Support both UserId and ApplicationUserId for backwards compatibility
            var userIdFilter = filters.ApplicationUserId ?? filters.UserId;
            if (!string.IsNullOrEmpty(userIdFilter))
                query = query.Where(al => al.ApplicationUserId == userIdFilter);

            if (!string.IsNullOrEmpty(filters.ActionType))
                query = query.Where(al => al.ActionType == filters.ActionType);

            if (filters.StartDate.HasValue)
                query = query.Where(al => al.Timestamp >= filters.StartDate.Value);

            if (filters.EndDate.HasValue)
                query = query.Where(al => al.Timestamp <= filters.EndDate.Value);

            if (filters.SecurityEventsOnly)
                query = query.Where(al => al.IsSecurityEvent);

            var totalCount = await query.CountAsync();

            // Phase 1: Load raw data from database
            var rawLogs = await query
                .OrderByDescending(al => al.Timestamp)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToListAsync();

            // Phase 2: Map to DTOs in memory (where JsonSerializer works)
            var logs = rawLogs.Select(al => new AuditLogDto
            {
                VideoId = al.AuditLogId,
                ApplicationUserId = al.ApplicationUserId,
                UserId = al.ApplicationUserId, // Alias
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
            }).ToList();

            return logs;
        }

        public async Task LogUnauthorizedAccessAsync(string userId, string resource, int resourceId)
        {
            await LogSecurityEventAsync(
                userId,
                "UnauthorizedAccess",
                $"Attempted unauthorized access to {resource} (ID: {resourceId})",
                severity: "High",
                metadata: new { Resource = resource, ResourceId = resourceId }
            );
        }

        /// <summary>
        /// Check for anomalous security patterns that may indicate an attack.
        /// Example: Multiple failed access attempts in short time period.
        /// </summary>
        private async Task CheckForSecurityAnomaliesAsync(string userId)
        {
            var recentAttempts = await _context.AuditLogs
                .Where(al =>
                    al.ApplicationUserId == userId &&
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
