using CoreServer.Application.Files.Commands;
using CoreServer.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;
using NSwag;
using NSwag.Annotations;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace CoreServer.WebUI.Controllers;

[Authorize]
public class UserFilesController : ApiControllerBase
{
    public UserFilesController()
    {
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> Get(Guid id)
    {
        return Ok();
    }


    [HttpPost]
    public async Task<ActionResult> Upload(FileType fileType, IFormFile file)
    {
        Mediator.Send(new SaveUserFileCommand
        {
            FileName = file.FileName,
            MimeType = file.ContentType,
            FileType = fileType,
            FileStream = file.OpenReadStream()
        });
        return Ok();
    }
}
