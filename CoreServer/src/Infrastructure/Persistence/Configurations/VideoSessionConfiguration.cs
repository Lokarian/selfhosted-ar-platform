using CoreServer.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreServer.Infrastructure.Persistence.Configurations;

public class VideoSessionConfiguration : IEntityTypeConfiguration<VideoSession>
{
    public void Configure(EntityTypeBuilder<VideoSession> builder)
    {
        builder.HasKey(x => x.BaseSessionId);
        builder.HasOne(x => x.BaseSession).WithOne(x => x.VideoSession)
            .HasForeignKey<VideoSession>(x => x.BaseSessionId);
        builder.Navigation(x => x.BaseSession).AutoInclude();
    }
}