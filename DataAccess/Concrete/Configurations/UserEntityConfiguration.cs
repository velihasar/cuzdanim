using Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class UserEntityConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.UserId);
            builder.Property(x => x.UserName).HasMaxLength(50).IsRequired();
            builder.Property(x => x.FullName).HasMaxLength(100);
            builder.Property(x => x.Email).HasMaxLength(100);
            builder.Property(x => x.PendingEmail).HasMaxLength(100);
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.BirthDate);
            builder.Property(x => x.Gender);
            builder.Property(x => x.RecordDate);
            builder.Property(x => x.Address).HasMaxLength(200);
            builder.Property(x => x.MobilePhones).HasMaxLength(30);
            builder.Property(x => x.Notes).HasMaxLength(500);
            builder.Property(x => x.LastLoginDate);
            builder.Property(x => x.GoogleId).HasMaxLength(255);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.UserName).IsUnique();
            builder.HasIndex(x => x.Email);
			builder.HasIndex(x => x.MobilePhones);
            builder.HasIndex(x => x.GoogleId).IsUnique();
        }
    }
}