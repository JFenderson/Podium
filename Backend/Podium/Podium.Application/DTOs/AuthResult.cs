using System;
using System.Collections.Generic;

namespace Podium.Application.DTOs
{
    /// <summary>
    /// Result of authentication operations
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? AccessToken { get; set; } // Alias for Token
        public string? RefreshToken { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }

        public static AuthResult SuccessResult(string token, string refreshToken, string userId, string email, string role, DateTime expiresAt)
        {
            return new AuthResult
            {
                Success = true,
                Token = token,
                AccessToken = token, // Set both for compatibility
                RefreshToken = refreshToken,
                UserId = userId,
                Email = email,
                Role = role,
                ExpiresAt = expiresAt
            };
        }

        public static AuthResult FailureResult(string message, List<string>? errors = null)
        {
            return new AuthResult
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}