using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreServer.Infrastructure.Persistence.Configurations;

public class ChatMemberConfiguration : IEntityTypeConfiguration<ChatMember>
{
    
    public void Configure(EntityTypeBuilder<ChatMember> builder)
    {
        builder.HasKey(x=>x.BaseMemberId);
        builder.Navigation(x=>x.BaseMember).AutoInclude();
    }
}