using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spendly.Domain.Categories;
using Spendly.Infrastructure.Persistence.Converters;

namespace Spendly.Infrastructure.Persistence.Configuration;

internal sealed class CategoryConfiguration
    : IEntityTypeConfiguration<Category>
{
    private const string CategoryTypeCheckConstraintSql =
        "type IN (1, 2)";

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable(
            "categories",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_categories_type_defined",
                CategoryTypeCheckConstraintSql));

        builder
            .HasKey(category => category.Id)
            .HasName("pk_categories");

        builder
            .Property(category => category.Id)
            .HasConversion(new CategoryIdConverter())
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
            .HasConversion(new CategoryTypeConverter())
            .HasColumnName("type")
            .HasColumnType("smallint")
            .IsRequired();

        builder
            .Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
