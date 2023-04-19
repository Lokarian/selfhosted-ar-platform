using System.Security.Claims;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Models;
using CoreServer.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserClaimsPrincipalFactory<AppIdentityUser> _userClaimsPrincipalFactory;
    private readonly UserManager<AppIdentityUser> _userManager;

    public IdentityService(
        UserManager<AppIdentityUser> userManager,
        IUserClaimsPrincipalFactory<AppIdentityUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(AppUser appUser, string password)
    {
        AppIdentityUser user = new AppIdentityUser { UserName = appUser.UserName, AppUser = appUser };

        IdentityResult result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public Task<(Result Result, AppUser? user)> LoginAsync(string userName, string password)
    {
        AppIdentityUser? user = _userManager.Users
            .Include(u => u.AppUser)
            .SingleOrDefault(u => u.UserName == userName);

        if (user == null)
        {
            return Task.FromResult((Result.Failure(new[] { "User does not exist." }), (AppUser?)null));
        }

        Task<bool> result = _userManager.CheckPasswordAsync(user, password);

        return Task.FromResult(result.Result
            ? (Result.Success(), user.AppUser)
            : (Result.Failure(new[] { "Invalid credentials." }), null));
    }

    public async Task<bool> IsInRoleAsync(Guid userId, string role)
    {
        AppIdentityUser? user = _userManager.Users.SingleOrDefault(u => u.AppUserId == userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(Guid userId, string policyName)
    {
        AppIdentityUser? user = _userManager.Users.SingleOrDefault(u => u.AppUserId == userId);

        if (user == null)
        {
            return false;
        }

        ClaimsPrincipal principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        AuthorizationResult result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        AppIdentityUser? user = _userManager.Users.SingleOrDefault(u => u.Id == userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(AppIdentityUser user)
    {
        IdentityResult result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }
}