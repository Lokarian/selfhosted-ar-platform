﻿using CoreServer.Application.Common.Interfaces;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;

namespace CoreServer.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger _logger;

    public LoggingBehaviour(ILogger<TRequest> logger, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task Process(TRequest request, CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        string userId = _currentUserService.User?.Id.ToString() ?? string.Empty;
        string userName = _currentUserService.User?.UserName ?? string.Empty;


        _logger.LogInformation("CoreServer Request: {Name} {@UserId} {@UserName} {@Request}",
            requestName, userId, userName, request);
    }
}