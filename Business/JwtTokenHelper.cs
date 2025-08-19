using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Itarix.Api.Business
{
    public static class JwtTokenHelper
    {
        /// <summary>
        /// Generates a signed JWT access token with enhanced security claims.
        /// </summary>
        // Itarix.Api.Business/JwtTokenHelper.cs
        public static string GenerateToken(
            int userId,
            string username,
            string email,              // <--- add this
            string role,
            string jwtKey,
            string jwtIssuer,
            string jwtAudience,
            int expiryMinutes = 360)
        {
            var now = DateTime.UtcNow;

            var claims = new[]
            {
        // JWT standard claims
        new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()), // or username if you prefer
        new Claim(JwtRegisteredClaimNames.UniqueName, username),
        new Claim(JwtRegisteredClaimNames.Email, email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat,
                  new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                  ClaimValueTypes.Integer64),

        // .NET-friendly claims (so ClaimTypes.* lookups work)
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Role, role ?? "user"),

        // optional custom
        new Claim("userId", userId.ToString())
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(expiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        /// <summary>
        /// Validates a JWT token and returns the principal if valid.
        /// </summary>
        public static ClaimsPrincipal ValidateToken(
            string token,
            string jwtKey,
            string jwtIssuer,
            string jwtAudience,
            bool validateLifetime = true)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.FromMinutes(2),

                // make ClaimTypes.NameIdentifier / ClaimTypes.Role resolve correctly
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role
            };

            return tokenHandler.ValidateToken(token, validationParams, out _);
        }

    }
}
