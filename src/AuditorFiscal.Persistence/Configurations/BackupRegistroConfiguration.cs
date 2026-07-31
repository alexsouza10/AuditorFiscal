using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditorFiscal.Persistence.Configurations;

public class BackupRegistroConfiguration : IEntityTypeConfiguration<BackupRegistro>
{
    public void Configure(EntityTypeBuilder<BackupRegistro> builder)
    {
        builder.ToTable("BackupsRealizados");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CaminhoArquivo).HasMaxLength(500).IsRequired();
        builder.Property(x => x.HashSha256).HasMaxLength(64).IsRequired();
    }
}
