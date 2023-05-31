using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreServer.Infrastructure.Persistence.Configurations;

public class ArSessionConfiguration : IEntityTypeConfiguration<ArSession>
{
    public void Configure(EntityTypeBuilder<ArSession> builder)
    {
        builder.HasKey(x => x.BaseSessionId);
        builder.HasOne(x => x.BaseSession).WithOne(x => x.ArSession)
            .HasForeignKey<ArSession>(x => x.BaseSessionId);
        builder.Navigation(x => x.BaseSession).AutoInclude();
    }
}