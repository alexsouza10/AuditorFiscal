using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditorFiscal.Persistence.Configurations;

public class FotoConfiguration : IEntityTypeConfiguration<Foto>
{
    public void Configure(EntityTypeBuilder<Foto> builder)
    {
        builder.ToTable("Fotos");

        builder.Property(x => x.NomeOriginal).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CaminhoArmazenamento).HasMaxLength(500).IsRequired();
        builder.Property(x => x.HashSha256).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => x.OrdemServicoId);
    }
}
