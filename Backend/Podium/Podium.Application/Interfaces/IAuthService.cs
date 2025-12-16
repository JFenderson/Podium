using Podium.Application.DTOs;
using Podium.Application.DTOs.Auth;
using Podium.Core.Entities;
using System.Threading.Tasks;

namespace Podium.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterDto dto);
    Task<AuthResult> LoginAsync(LoginDto dto);
    Task<AuthResult> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<bool> RevokeTokenAsync(RefreshTokenRequestDto dto);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    Task<bool> ResetPasswordAsync(ResetPasswordConfirmDto dto);
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<AuthResult> ConfirmEmailAsync(string userId, string token);
}