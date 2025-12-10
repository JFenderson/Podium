using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Auth
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string Token { get; set; } = string.Empty; // The expired access token

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
