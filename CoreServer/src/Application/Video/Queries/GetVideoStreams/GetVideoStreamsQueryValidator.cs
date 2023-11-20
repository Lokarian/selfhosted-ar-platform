using CoreServer.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Queries.GetVideoStreams;

public class GetVideoStreamsQueryValidator : AbstractValidator<GetVideoStreamsQuery>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public GetVideoStreamsQueryValidator(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
        RuleFor(x => x.VideoSessionId).NotEmpty()
            .MustAsync(IsParticipant).WithMessage("You are not a participant of this video session");
    }

    private async Task<bool> IsParticipant(Guid videoSessionId, CancellationToken cancellationToken)
    {
        var videoSession = await _context.VideoSessions
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.BaseSessionId == videoSessionId, cancellationToken);
        if (videoSession == null)
        {
            return false;
        }
        return videoSession.Members.Any(x => x.BaseMember.UserId == _currentUserService.User!.Id);
    }
}