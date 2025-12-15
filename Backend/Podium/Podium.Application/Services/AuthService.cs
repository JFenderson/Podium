using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Podium.Application.DTOs.Auth;
using Podium.Application.DTOs;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Podium.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        RoleManager<IdentityRole> roleManager,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _configuration = configuration;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return new AuthResult { Success = false, Errors = new List<string> { "Email already in use." } };
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return new AuthResult { Success = false, Errors = result.Errors.Select(e => e.Description).ToList() };
        }

        if (await _roleManager.RoleExistsAsync(dto.Role))
        {
            await _userManager.AddToRoleAsync(user, dto.Role);
        }
        else
        {
            await _userManager.AddToRoleAsync(user, Roles.Student);
        }

        try
        {
            switch (dto.Role)
            {
                case Roles.Student:
                    var student = new Student
                    {
                        ApplicationUserId = user.Id,
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        Email = dto.Email,
                        Instrument = dto.Instrument ?? "Unknown",
                        GraduationYear = dto.GraduationYear ?? DateTime.Now.Year + 1,
                        HighSchool = dto.HighSchool,
                        PhoneNumber = dto.PhoneNumber
                    };
                    await _unitOfWork.Students.AddAsync(student);
                    break;

                case Roles.Guardian:
                    var guardian = new Guardian
                    {
                        ApplicationUserId = user.Id,
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        Email = dto.Email,
                        PhoneNumber = dto.PhoneNumber,
                    };
                    await _unitOfWork.Guardians.AddAsync(guardian);
                    break;

                case Roles.Recruiter:
                case Roles.Director:
                    if (!dto.BandId.HasValue) throw new Exception("Band selection is required for Staff.");
                    var staff = new BandStaff
                    {
                        ApplicationUserId = user.Id,
                        BandId = dto.BandId.Value,
                        Title = dto.StaffTitle ?? dto.Role,
                        IsActive = true,
                        CanViewStudents = true,
                        CanContact = true,
                        CanSendOffers = dto.Role == Roles.Director
                    };
                    await _unitOfWork.BandStaff.AddAsync(staff);
                    break;
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(user);
            return new AuthResult { Success = false, Errors = new List<string> { $"Failed to create profile: {ex.Message}" } };
        }

        return await GenerateAuthResultAsync(user);
    }

    public async Task<AuthResult> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
        {
            return new AuthResult { Success = false, Errors = new List<string> { "Invalid email or password" } };
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
        {
            return new AuthResult { Success = false, Errors = new List<string> { "Invalid email or password" } };
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await GenerateAuthResultAsync(user);
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetQueryable()
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            return new AuthResult { Success = false, Errors = new List<string> { "Invalid or expired refresh token" } };
        }

        var user = await _userManager.FindByIdAsync(storedToken.ApplicationUserId);
        if (user == null || !user.IsActive)
        {
            return new AuthResult { Success = false, Errors = new List<string> { "User not found or inactive" } };
        }

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);

        var authResult = await GenerateAuthResultAsync(user);
        var newStoredToken = await _unitOfWork.RefreshTokens.GetQueryable()
            .FirstOrDefaultAsync(rt => rt.Token == authResult.RefreshToken);

        if (newStoredToken != null)
        {
            storedToken.ReplacedByToken = newStoredToken.Token;
        }

        await _unitOfWork.SaveChangesAsync();
        return authResult;
    }

    public async Task<bool> RevokeTokenAsync(RefreshTokenRequestDto dto)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetQueryable()
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (storedToken == null || !storedToken.IsActive) return false;

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    private async Task<AuthResult> GenerateAuthResultAsync(ApplicationUser user)
    {
        var accessToken = await GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            ApplicationUserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
            UserId = user.Id,
            Email = user.Email
        };
    }

    private async Task<string> GenerateAccessToken(ApplicationUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT Secret missing"));
        var userRoles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        return result.Succeeded;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordConfirmDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return true;
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        return true;
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private int GetAccessTokenExpirationMinutes()
    {
        return int.Parse(_configuration["JWT:ExpirationMinutes"] ?? "60");
    }
}