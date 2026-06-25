using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameVault.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace GameVault.Api.Services;

public class JwtTokenService(IConfiguration configuration)
{
    public string CreateToken(AppUser user)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException("JWT key is missing.");
        var expirationMinutes = jwtSection.GetValue("ExpirationMinutes", 120);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
