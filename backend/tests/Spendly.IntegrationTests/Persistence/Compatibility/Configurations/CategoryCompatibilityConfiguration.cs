using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spendly.Domain.Categories;

namespace Spendly.IntegrationTests.Persistence.Compatibility.Configurations;

internal sealed class CategoryCompatibilityConfiguration
    : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder
            .HasKey(category => category.Id)
            .HasName("pk_categories");

        builder
            .Property(category => category.Id)
            .HasConversion(CompatibilityValueConverters.CategoryIdToGuid)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder
            .Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(Category.MaxNameLength)
            .IsRequired();

        builder
            .Property(category => category.Type)
            .HasColumnName("type")
            .HasColumnType("integer")
            .IsRequired();

        builder
            .Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
