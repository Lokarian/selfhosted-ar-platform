using CoreServer.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Commands.StopVideoStream;

public class StopVideoStreamCommandValidator : AbstractValidator<StopVideoStreamCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public StopVideoStreamCommandValidator(ICurrentUserService currentUserService, IApplicationDbContext context)
    {
        _currentUserService = currentUserService;
        _context = context;
        RuleFor(x => x.VideoStreamId).NotEmpty().WithMessage("Video stream id must not be empty")
            .MustAsync(MustBeOwnVideoStream).WithMessage("This is not your video stream id");
    }

    private async Task<bool> MustBeOwnVideoStream(Guid videoStreamId, CancellationToken cancellationToken)
    {
        var videoStream = await _context.VideoStreams.Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == videoStreamId, cancellationToken);
        if (videoStream == null)
        {
            return false;
        }

        return videoStream.Owner.BaseMember.UserId == _currentUserService.User!.Id;
    }
}