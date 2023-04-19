using CoreServer.Application.UserFiles.Commands;
using CoreServer.Application.UserFiles.Queries;
using CoreServer.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

[Authorize]
public class UserFilesController : ApiControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult> Get(Guid id)
    {
        UserFileWithFilestream file = await Mediator.Send(new GetUserFileQuery { Id = id });
        FileStreamResult fileStreamResult = new FileStreamResult(file.FileStream, file.UserFile.MimeType)
        {
            FileDownloadName = file.UserFile.FileName
        };
        HttpContext.Response.RegisterForDispose(file);
        return fileStreamResult;
    }

    [HttpPost]
    public async Task<ActionResult<UserFile>> Upload(FileType fileType, IFormFile file)
    {
        UserFile userFile = await Mediator.Send(new SaveUserFileCommand
        {
            FileName = file.FileName,
            MimeType = file.ContentType,
            FileType = fileType,
            FileStream = file.OpenReadStream()
        });
        return Ok(userFile);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteUserFileCommand { Id = id });
        return Ok();
    }
}