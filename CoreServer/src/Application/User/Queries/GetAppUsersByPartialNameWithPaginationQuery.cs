using AutoMapper;
using AutoMapper.QueryableExtensions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Mappings;
using CoreServer.Application.Common.Models;
using CoreServer.Domain.Entities;
using MediatR;

namespace CoreServer.Application.User.Queries;

public record GetAppUsersByPartialNameWithPaginationQuery : IRequest<PaginatedList<AppUserDto>>
{
    public string? PartialName { get; init; } = "";
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetAppUsersByPartialNameWithPaginationQueryHandler : IRequestHandler<GetAppUsersByPartialNameWithPaginationQuery, PaginatedList<AppUserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAppUsersByPartialNameWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<AppUserDto>> Handle(GetAppUsersByPartialNameWithPaginationQuery request, CancellationToken cancellationToken)
    {
        return await _context.AppUsers
            .Where(x=>x.AccountType==AppUserAccountType.User)
            .Where(x => request.PartialName==null||x.UserName.Contains(request.PartialName))
            .OrderBy(x => x.UserName)
            .ProjectTo<AppUserDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}