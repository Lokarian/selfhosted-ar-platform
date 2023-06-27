using System.Threading.Channels;
using CoreServer.Application.AR.Commands.JoinArSession;
using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.User.Commands.CreateUserConnection;
using CoreServer.Application.User.Commands.UpdateUserConnection;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Events.Ar;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Infrastructure.Unity;

[Authorize]
public class UnityBrokerHub : Hub
{
    private readonly IStreamDistributorService<byte[]> _streamDistributorService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;


    public UnityBrokerHub(IStreamDistributorService<byte[]> streamDistributorService,
        ICurrentUserService currentUserService, IApplicationDbContext context, IMediator mediator)
    {
        _streamDistributorService = streamDistributorService;
        _currentUserService = currentUserService;
        _context = context;
        _mediator = mediator;
    }

    public async Task<string> CreateArMember(string sessionId, int arUserRole)
    {
        Console.WriteLine(
            $"User {Context.UserIdentifier} on Client {Context.ConnectionId} connected with {arUserRole}");
        var userId = Guid.Parse(Context.UserIdentifier!);
        var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            throw new NotFoundException(Context.UserIdentifier!);
        }

        this._currentUserService.User = user;
        var connection = await _mediator.Send(new CreateUserConnectionCommand()
        {
            UserId = Guid.Parse(Context.UserIdentifier!), ConnectionId = Context.ConnectionId
        });
        this._currentUserService.Connection = connection;
        var member =
            await _mediator.Send(new JoinArSessionCommand()
            {
                ArSessionId = Guid.Parse(sessionId), Role = (ArUserRole)arUserRole
            });
        await Groups.AddToGroupAsync(Context.ConnectionId, member.Id.ToString());
        return member.Id.ToString();
    }

    public async Task RegisterAsServer(string serverId)
    {
        Console.WriteLine($"Server {serverId} registered");
        await Groups.AddToGroupAsync(Context.ConnectionId, serverId);
        var user= _context.AppUsers.FirstOrDefault(x => x.Id == Guid.Parse(Context.UserIdentifier!));
        if (user?.AccountType != AppUserAccountType.Service)
        {
            throw new UnauthorizedAccessException();
        }
        var session = _context.ArSessions.AsTracking().FirstOrDefault(x => x.BaseSessionId == Guid.Parse(serverId));
        if (session == null)
        {
            throw new NotFoundException(serverId);
        }
        session.ServerState=ArServerState.Running;
        session.AddDomainEvent(new ArSessionUpdatedEvent(session));
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        await _context.SaveChangesAsync(cancellationTokenSource.Token);
    }

    public void NotifyServerOfClient(string serverId, string memberId)
    {
        Console.WriteLine($"Server {serverId} notified of client {memberId}");
        Clients.Group(serverId).SendAsync("NewClientConnection", memberId);
    }

    public ChannelReader<byte[]> ServerGetUserStream(string serverId, string memberId,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<byte[]>();
        _streamDistributorService.Subscribe($"{serverId}_{memberId}", Guid.Parse(Context.UserIdentifier!),
            channel.Writer,
            cancellationToken);
        return channel.Reader;
    }

    public async Task ServerSendStreamToMember(IAsyncEnumerable<byte[]> stream, string memberId)
    {
        var channel = Channel.CreateUnbounded<byte[]>();
        var streamId =
            await _streamDistributorService.RegisterStream(Guid.Parse(Context.UserIdentifier!), channel.Reader,
                memberId);
        try
        {
            await foreach (var item in stream)
            {
                await channel.Writer.WriteAsync(item);
            }
        }
        catch (Exception e)
        {
            if (e.GetType() != typeof(OperationCanceledException))
            {
                throw;
            }
        }
        finally
        {
            await _streamDistributorService.RemoveStream(streamId);
        }
    }

    public async Task ClientSendToServer(IAsyncEnumerable<byte[]> stream, string serverId, string memberId)
    {
        var channel = Channel.CreateUnbounded<byte[]>();
        var streamId =
            await _streamDistributorService.RegisterStream(Guid.Parse(Context.UserIdentifier!), channel.Reader,
                $"{serverId}_{memberId}");
        try
        {
            await foreach (var item in stream)
            {
                await channel.Writer.WriteAsync(item);
            }
        }
        catch (Exception e)
        {
            if (e.GetType() != typeof(OperationCanceledException))
            {
                throw;
            }
        }
        finally
        {
            await _streamDistributorService.RemoveStream(streamId);
        }
    }


    public ChannelReader<byte[]> ClientGetOwnStream(string memberId, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<byte[]>();
        _streamDistributorService.Subscribe(memberId, Guid.Parse(Context.UserIdentifier!), channel.Writer,
            cancellationToken);
        return channel.Reader;
    }

    public void NotifyClientOfSuccessfulConnection(string memberId)
    {
        Console.WriteLine($"Notifying Client {memberId} of successful connection");
        Clients.Groups(memberId).SendAsync("ConnectionEstablished");
    }


    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        var member =
            await _context.ArMembers.FirstOrDefaultAsync(m => m.UserConnection.ConnectionId == Context.ConnectionId);
        if (member != null)
        {
            Console.WriteLine($"Notifying Server {member.SessionId} of {member.Id} disconnection");
            await Clients.Group(member.SessionId.ToString()).SendAsync("ClientDisconnected", member.Id.ToString());
        }
        var appUser=await _context.AppUsers.FirstOrDefaultAsync(x => x.Id == Guid.Parse(Context.UserIdentifier!));
        if (appUser?.AccountType == AppUserAccountType.Service)
        {
            //take everything between first and last "-"
            var sessionId=Guid.Parse(appUser.UserName.Split("-")[1..^1].Aggregate((x, y) => x + "-" + y));
            var session = await _context.ArSessions.FirstOrDefaultAsync(x => x.BaseSessionId == sessionId);
            if (session != null)
            {
                session.ServerState = ArServerState.Stopped;
                session.AddDomainEvent(new ArSessionUpdatedEvent(session));
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                await _context.SaveChangesAsync(cancellationTokenSource.Token);
            }
        }

        await _mediator.Send(new DisconnectUserConnectionCommand() { ConnectionId = Context.ConnectionId });
    }
}