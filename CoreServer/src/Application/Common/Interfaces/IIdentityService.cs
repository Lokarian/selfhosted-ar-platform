using CoreServer.Application.Common.Models;
using CoreServer.Domain.Entities;

namespace CoreServer.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> IsInRoleAsync(Guid userId, string role);

    Task<bool> AuthorizeAsync(Guid userId, string policyName);

    Task<(Result Result, String UserId)> CreateUserAsync(AppUser appUser, string password);
    
    Task<(Result Result, AppUser? user)> LoginAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);
}
