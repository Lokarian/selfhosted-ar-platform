using CoreServer.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Common.Validators;

public class SessionContextValidator<T> : UserContextValidator<T>
{
    public SessionContextValidator(IApplicationDbContext context,ICurrentUserService currentUserService) : base(
        currentUserService, context)
    {
    }

    protected async Task<bool> MustBeMemberOfSession(Guid sessionId, CancellationToken arg3)
    {
        var userId = this._currentUserService.User?.Id;
        if (userId == null)
        {
            return false;
        }

        return await _context.UserSessions.AnyAsync(x =>
            x.Id == sessionId && x.Members.Any(y => y.UserId == userId && y.DeletedAt == null), cancellationToken: arg3);
    }
}