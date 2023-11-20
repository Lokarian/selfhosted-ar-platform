using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Entities.Video;
using CoreServer.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreServer.Infrastructure.Persistence;

public class ApplicationDbContextInitialiser
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<AppIdentityUser> _userManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context, UserManager<AppIdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.IsNpgsql())
            {
                await _context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        IdentityRole administratorRole = new IdentityRole("Administrator");

        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await _roleManager.CreateAsync(administratorRole);
        }

        // Default users
        AppUser appUser = new AppUser { UserName = "Administrator", Email = "administrator@localhost" };
        _context.AppUsers.Add(appUser);
        AppIdentityUser administrator = new AppIdentityUser
        {
            UserName = "Administrator", Email = "administrator@localhost", AppUser = appUser
        };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
            }
        }

        // Default data
        // Seed, if necessary
        if (!_context.BaseSessions.Any())
        {
            var baseSession=_context.BaseSessions.Add(new BaseSession()
            {
                Name = "DemoARSession",
                Id = Guid.Parse("c3b66fb7-7322-46be-8c19-020b64aa89ea"),
                CreatedAt = DateTime.Now,
            });
            var sessionMember = _context.SessionMembers.Add(new SessionMember()
            {
                Id = new Guid(),
                Session = baseSession.Entity,
                User = appUser
            });
            var arSession = _context.ArSessions.Add(new ArSession()
            {
                BaseSession = baseSession.Entity ,
                Members = new List<ArMember>(),
                SessionType = ArSessionType.RemoteAssist,
                ServerState = ArServerState.Stopped,
            });
            var videoSession = _context.VideoSessions.Add(new VideoSession()
            {
                BaseSession = baseSession.Entity,
                Members = new List<VideoMember>(),
            });
            var chatSession = _context.ChatSessions.Add(new ChatSession()
            {
                BaseSession = baseSession.Entity,
                Members = new List<ChatMember>(),
            });
            var chatMember = _context.ChatMembers.Add(new ChatMember()
            {
                Session = chatSession.Entity,
                BaseMember = sessionMember.Entity,
            });

            await _context.SaveChangesAsync();
        }
    }
}