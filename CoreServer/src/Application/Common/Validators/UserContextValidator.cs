using CoreServer.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Common.Validators;

public class UserContextValidator<T>: AbstractValidator<T>
{
    
    protected readonly ICurrentUserService _currentUserService;
    protected readonly IApplicationDbContext _context;

    public UserContextValidator(ICurrentUserService currentUserService, IApplicationDbContext context)
    {
        _currentUserService = currentUserService;
        _context = context;
    }
    
}