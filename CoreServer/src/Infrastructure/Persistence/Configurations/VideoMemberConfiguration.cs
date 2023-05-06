using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreServer.Infrastructure.Persistence.Configurations;

public class VideoMemberConfiguration : IEntityTypeConfiguration<VideoMember>
{
    
    public void Configure(EntityTypeBuilder<VideoMember> builder)
    {
        builder.Navigation(x=>x.BaseMember).AutoInclude();
    }
}