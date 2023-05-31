using CoreServer.Domain.Entities.AR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreServer.Infrastructure.Persistence.Configurations;

public class ArMemberConfiguration : IEntityTypeConfiguration<ArMember>
{
    
    public void Configure(EntityTypeBuilder<ArMember> builder)
    {
        builder.HasKey(x=>x.Id);
        builder.HasOne(x=>x.BaseMember).WithMany().HasForeignKey(x=>x.BaseMemberId);
        builder.Navigation(x=>x.BaseMember).AutoInclude();
        builder.HasOne(x=>x.UserConnection).WithMany().HasForeignKey(x=>x.UserConnectionId);
        
    }
}