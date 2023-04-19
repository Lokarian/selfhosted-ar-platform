﻿using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreServer.WebUI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}