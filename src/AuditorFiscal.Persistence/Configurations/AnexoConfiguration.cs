using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditorFiscal.Persistence.Configurations;

public class AnexoConfiguration : IEntityTypeConfiguration<Anexo>
{
    public void Configure(EntityTypeBuilder<Anexo> builder)
    {
        builder.ToTable("Anexos");

        builder.Property(x => x.NomeOriginal).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CaminhoArmazenamento).HasMaxLength(500).IsRequired();
        builder.Property(x => x.HashSha256).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => x.OrdemServicoId);
    }
}
