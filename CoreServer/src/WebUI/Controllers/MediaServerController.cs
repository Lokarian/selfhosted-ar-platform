using System.Buffers;
using System.Text;
using CoreServer.Application.Video.Queries;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace WebUI.Controllers;

public class MediaServerController : ApiControllerBase
{
    
    
    //AuthenticateUser post
    [HttpPost]
    public async Task<ActionResult> AuthenticateUser([FromBody]UserCanAccessStreamQuery query)
    {
    return Ok(await Mediator.Send(query));
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