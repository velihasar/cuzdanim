using Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class IncomeCategoryEntityConfiguration : IEntityTypeConfiguration<IncomeCategory>
    {
        public void Configure(EntityTypeBuilder<IncomeCategory> builder)
        {
            builder.ToTable("IncomeCategories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired(false);

            // UserId nullable olmalı - null ise global kategori
            builder.Property(x => x.UserId)
                .IsRequired(false);

            // Foreign key relationship - User silinirse kategori de silinsin (Cascade)
            builder.HasOne(x => x.User)
                .WithMany(u => u.IncomeCategories)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

