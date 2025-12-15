using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Podium.Application.DTOs.AuditLog;
using Podium.Core.Entities;
using Podium.Core.Interfaces; // Updated to Core.Interfaces
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuditService> _logger;

        public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task LogActionAsync(string userId, string actionType, string description, object? metadata = null)
        {
            var auditLog = new AuditLog
            {
                ApplicationUserId = userId,
                ActionType = actionType,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            await CheckForSecurityAnomaliesAsync(userId);
        }

        public async Task LogSecurityEventAsync(string userId, string actionType, string description, string severity = "Medium", object? metadata = null)
        {
            var auditLog = new AuditLog
            {
                ApplicationUserId = userId,
                ActionType = actionType,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                IsSecurityEvent = true,
                Severity = severity,
                MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogWarning("SECURITY EVENT: {ActionType} by {UserId} - {Description}", actionType, userId, description);
        }

        public async Task<List<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filters)
        {
            var query = _unitOfWork.AuditLogs.GetQueryable();

            var userIdFilter = filters.ApplicationUserId ?? filters.UserId;
            if (!string.IsNullOrEmpty(userIdFilter))
                query = query.Where(al => al.ApplicationUserId == userIdFilter);

            if (!string.IsNullOrEmpty(filters.ActionType))
                query = query.Where(al => al.ActionType == filters.ActionType);

            if (filters.StartDate.HasValue)
                query = query.Where(al => al.CreatedAt >= filters.StartDate.Value);

            if (filters.EndDate.HasValue)
                query = query.Where(al => al.CreatedAt <= filters.EndDate.Value);

            if (filters.SecurityEventsOnly)
                query = query.Where(al => al.IsSecurityEvent);

            var rawLogs = await query
                .OrderByDescending(al => al.CreatedAt)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToListAsync();

            var logs = rawLogs.Select(al => new AuditLogDto
            {
                VideoId = al.Id,
                ApplicationUserId = al.ApplicationUserId,
                UserId = al.ApplicationUserId,
                ActionType = al.ActionType,
                Description = al.Description,
                Timestamp = al.CreatedAt,
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

        private async Task CheckForSecurityAnomaliesAsync(string userId)
        {
            var recentAttempts = await _unitOfWork.AuditLogs.GetQueryable()
                .Where(al =>
                    al.ApplicationUserId == userId &&
                    al.ActionType == "UnauthorizedAccess" &&
                    al.CreatedAt >= DateTime.UtcNow.AddMinutes(-5))
                .CountAsync();

            if (recentAttempts >= 5)
            {
                _logger.LogCritical("SECURITY ALERT: User {UserId} has {AttemptCount} unauthorized access attempts in last 5 minutes", userId, recentAttempts);
            }
        }
    }
}