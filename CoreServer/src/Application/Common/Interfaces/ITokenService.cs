using CoreServer.Application.Common.Models;
using CoreServer.Domain.Entities;

namespace CoreServer.Application.Common.Interfaces;

public interface ITokenService
{
    Task<string> CreateTokenAsync(AppUser user);
}