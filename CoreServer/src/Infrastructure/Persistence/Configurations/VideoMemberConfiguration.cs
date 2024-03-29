﻿using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreServer.Infrastructure.Persistence.Configurations;

public class VideoMemberConfiguration : IEntityTypeConfiguration<VideoMember>
{
    
    public void Configure(EntityTypeBuilder<VideoMember> builder)
    {
        builder.HasKey(x=>x.Id);
        builder.HasOne(x=>x.BaseMember).WithMany().HasForeignKey(x=>x.BaseMemberId);
        builder.Navigation(x=>x.BaseMember).AutoInclude();
        builder.HasOne(x=>x.UserConnection).WithMany().HasForeignKey(x=>x.UserConnectionId);
        
    }
}