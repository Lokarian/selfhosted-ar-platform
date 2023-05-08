using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Video.Commands.StopVideoStream;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CoreServer.Infrastructure.RPC;

[Authorize]
public class SignalRHub : Hub
{
    private readonly ILogger<SignalRHub> _logger;
    private readonly IUserConnectionStore _userConnectionStore;
    private readonly IStreamDistributorService<object> _streamDistributorService;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public SignalRHub(ILogger<SignalRHub> logger, IUserConnectionStore userConnectionStore,
        IStreamDistributorService<object> streamDistributorService, IMediator mediator,
        ICurrentUserService currentUserService, IApplicationDbContext context)
    {
        _logger = logger;
        _userConnectionStore = userConnectionStore;
        _streamDistributorService = streamDistributorService;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _context = context;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation($"User ${Context.UserIdentifier} on Client {Context.ConnectionId} connected");
        _userConnectionStore.AddConnection(Guid.Parse(Context.UserIdentifier!), Context.ConnectionId);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation($"User ${Context.UserIdentifier} on Client {Context.ConnectionId} disconnected");
        _userConnectionStore.RemoveConnection(Guid.Parse(Context.UserIdentifier!), Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public Task RegisterService(string serviceName)
    {
        _logger.LogInformation(
            $"User ${Context.UserIdentifier} on Client {Context.ConnectionId} registered service {serviceName}");
        _userConnectionStore.AddServiceToConnection(Context.ConnectionId, serviceName);
        return Task.CompletedTask;
    }

    public async Task ReceiveVideoStream(ChannelReader<byte[]> stream)
    {
        //store stream in _streams

        await foreach (var item in stream.ReadAllAsync())
        {
            //send to all clients
            Console.WriteLine($"{item.Length} bytes to all clients");
            await Clients.All.SendAsync("RpcVideoService/ClientGetVideoStream", item);
        }
    }

    public async Task PublishStream(IAsyncEnumerable<object> stream, Guid id)
    {
        //create channelreader from stream
        var channel = Channel.CreateUnbounded<object>();
        await _streamDistributorService.RegisterStream(Guid.Parse(Context.UserIdentifier!), channel.Reader, id);
        await foreach (var item in stream)
        {
            await channel.Writer.WriteAsync(item);
        }
    }

    public ChannelReader<object> SubscribeToStream(
        Guid id,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<object>();
        _streamDistributorService.Subscribe(id, Guid.Parse(Context.UserIdentifier!), channel.Writer, cancellationToken);
        return channel.Reader;
    }

    //upload VideoStream to server
    public async Task UploadVideoStream(ChannelReader<byte[]> stream, Guid id, string streamPW)
    {
        Process? ffmpeg = null;
        try
        {
            //start ffmpeg process
            ffmpeg = new Process
            {
                StartInfo =
                {
                    FileName = "ffmpeg",
                    Arguments =
                        $"-i - -c:v libx264 -preset veryfast -tune zerolatency -profile:v baseline -level 3.0 -pix_fmt yuv420p -c:a copy -f rtsp -rtsp_transport tcp rtsp://{Context.UserIdentifier}:{streamPW}@localhost:8554/{id}",
                    //$"-f webm -i - -c:v libx264 -preset ultrafast -tune zerolatency -f rtsp rtsp://{Context.UserIdentifier}:{streamPW}@localhost:8554/{id}",

                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true
                }
            };
            ffmpeg.Start();
            //loop through stream and write to ffmpeg process
            await foreach (var item in stream.ReadAllAsync())
            {
                if (ffmpeg.HasExited)
                {
                    break;
                }

                await ffmpeg.StandardInput.BaseStream.WriteAsync(item, 0, item.Length);
            }
        }
        catch (Exception _)
        {
            //ignore
        }
        finally
        {
            //stop ffmpeg process
            ffmpeg?.Kill();
            //mark videostream as stopped
            this._currentUserService.User =
                await this._context.AppUsers.FindAsync(Guid.Parse(this.Context.UserIdentifier));
            await this._mediator.Send(new StopVideoStreamCommand { VideoStreamId = id });
        }
    }
}