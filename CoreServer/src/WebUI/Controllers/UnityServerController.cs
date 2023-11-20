using System.Buffers;
using System.Text;
using CoreServer.Application.AR.Queries.UserCanAccessUnityServer;
using CoreServer.Application.Video.Queries;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace WebUI.Controllers;

public class UnityServerController : ApiControllerBase
{
    //AuthenticateUser post
    [HttpPost]
    public async Task<ActionResult> AuthenticateUser([FromBody] UserCanAccessUnityServerQuery query)
    {
        var canAccess = await Mediator.Send(query);
        if (canAccess)
        {
            return Ok();
        }
        else
        {
            return Unauthorized();
        }
    }
}