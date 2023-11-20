using CoreServer.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreServer.Infrastructure.Persistence.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.HasKey(x => x.BaseSessionId);
        builder.HasOne(x => x.BaseSession).WithOne(x => x.ChatSession)
            .HasForeignKey<ChatSession>(x => x.BaseSessionId);
        builder.Navigation(x => x.BaseSession).AutoInclude();
    }
}