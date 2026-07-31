using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditorFiscal.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Cor).HasMaxLength(20).IsRequired();

        builder.Navigation(x => x.OrdensServico).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Nome).IsUnique();
    }
}
