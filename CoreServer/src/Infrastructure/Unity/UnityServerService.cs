using System.Diagnostics;
using System.Text;
using CoreServer.Application.AR.Commands.CreateArServerUser;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.AR;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreServer.Infrastructure.Unity;

public class UnityServerService : IUnityServerService
{
    private readonly ILogger<UnityServerService> _logger;
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;

    public UnityServerService(ILogger<UnityServerService> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        _httpClient = new HttpClient();
    }

    public async Task StartServer(Guid arSessionId, ArSessionType sessionType)
    {
        _logger.LogInformation("Starting Unity Server");
        var result = await _mediator.Send(new CreateArServerUserCommand(arSessionId));
        //if development than return
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            _logger.LogInformation("Credentials for Unity Server: id: {0}, token: {1}", arSessionId, result.Item2);
            return;
        }

        var imageName = Environment.GetEnvironmentVariable("UNITY_SERVER_IMAGE_NAME");
        var volume = Environment.GetEnvironmentVariable("UNITY_SERVER_VOLUME");
        var paramString =
            $"run -d --rm -v {volume}:/remoteassist -e \"AR_SESSION_ID={arSessionId}\" -e \"AR_SESSION_TYPE={sessionType}\" -e \"ACCESS_TOKEN={result.Item2}\" --name arServer-{arSessionId} {imageName}";
        _logger.LogInformation($"Starting Unity Server with params: {paramString}");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = paramString,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        var outputBuilder = new StringBuilder();
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                // Log the output with a prefix
                _logger.LogInformation($"Process log: {e.Data}");
                outputBuilder.AppendLine(e.Data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        var output = outputBuilder.ToString();
        _logger.LogInformation($"Unity Server started with output: {output}");
    }

    public Task ShutdownServer(Guid arSessionId)
    {
        _logger.LogInformation("Shutting down Unity Server");

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            _logger.LogInformation("Unity Server stopped");
            return Task.CompletedTask;
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"rm -f arServer-{arSessionId}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        _logger.LogInformation($"Unity Server stopped with output: {output}");
        return Task.CompletedTask;
    }
}
