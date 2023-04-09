using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MediatR;

public class JWTTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JWTTokenService(IConfiguration config)
    {
        _config = config;
    }

    public Task<string> CreateTokenAsync(AppUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), };
        var token = new JwtSecurityToken(_config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            claims,
            signingCredentials: credentials);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(tokenString);
    }
}