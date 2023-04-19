using CoreServer.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreServer.Infrastructure.Persistence.Configurations;

public class ChatMemberConfiguration : IEntityTypeConfiguration<ChatMember>
{
    public void Configure(EntityTypeBuilder<ChatMember> builder)
    {
        builder.HasKey(e => new { e.SessionId, e.UserId });
        builder.HasOne(e => e.Session).WithMany(e => e.Members).HasForeignKey(e => e.SessionId);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
    }
}