using Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class AssetTypeEntityConfiguration : IEntityTypeConfiguration<AssetType>
    {
        public void Configure(EntityTypeBuilder<AssetType> builder)
        {
            builder.ToTable("AssetTypes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired(false);

            builder.Property(x => x.ConvertedAmountType)
                .IsRequired();

            builder.Property(x => x.TlValue)
                .IsRequired();

            builder.Property(x => x.ApiUrlKey)
                .HasMaxLength(100)
                .IsRequired(false);

            // UserId nullable olmalı - null ise global tip
            builder.Property(x => x.UserId)
                .IsRequired(false);

            // Foreign key relationship - nullable olmalı
            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull); // User silinirse null yap
        }
    }
}

