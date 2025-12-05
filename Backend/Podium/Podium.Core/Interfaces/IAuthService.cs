using DocumentManagement.Core.Entities;

namespace DocumentManagement.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName);
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public List<string> Errors { get; set; } = new();
}