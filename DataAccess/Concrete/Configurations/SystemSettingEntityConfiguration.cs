using Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SystemSettingEntityConfiguration : BaseConfiguration<SystemSetting>
    {
        public override void Configure(EntityTypeBuilder<SystemSetting> builder)
        {
            builder.ToTable("SystemSettings");

            builder.Property(x => x.Key)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Value)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(x => x.Category)
                .HasMaxLength(100)
                .IsRequired(false);

            // Key unique olmalı
            builder.HasIndex(x => x.Key)
                .IsUnique();

            base.Configure(builder);
        }
    }
}

