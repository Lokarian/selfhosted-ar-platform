﻿using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Queries.GetChatMembers;

public class GetChatMembersQueryValidator : AbstractValidator<GetChatMembersQuery>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetChatMembersQueryValidator(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("BaseSession Id must not be empty")
            .MustAsync(UserIsChatMember).WithMessage("User is not a member of this session");
    }

    private async Task<bool> UserIsChatMember(Guid sessionId, CancellationToken cancellationToken)
    {
        ChatMember? chatMember = await _context.ChatMembers.FirstOrDefaultAsync(
            x => x.SessionId == sessionId && x.BaseMember.UserId == _currentUserService.User!.Id,
            cancellationToken);
        return chatMember != null;
    }
}