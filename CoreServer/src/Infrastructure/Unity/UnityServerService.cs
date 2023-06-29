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
        //if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        /*if (true)
        {
            _logger.LogInformation("Credentials for Unity Server: id: {0}, token: {1}", arSessionId, result.Item2);
            return;
        }*/
    
        //make a post web request to localhost:8080/docker with body AR_SESSION_ID, AR_SESSION_TYPE, ACCESS_TOKEN
        var payload = "{\"AR_SESSION_ID\":\"" + arSessionId + "\",\"AR_SESSION_TYPE\":\"" + sessionType + "\",\"ACCESS_TOKEN\":\"" + result.Item2 + "\"}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("http://reithmeir.duckdns.org:8080/docker", content);

        /*var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments =
                    $"run -d -e \"AR_SESSION_ID={arSessionId}\" -e \"AR_SESSION_TYPE={sessionType}\" -e \"ACCESS_TOKEN={result.Item2}\" --name arServer-{arSessionId} selfhosted-ar-platform-backend:latest",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        _logger.LogInformation($"Unity Server started with output: {output}");*/
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