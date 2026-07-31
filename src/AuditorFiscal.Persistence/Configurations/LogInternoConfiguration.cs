using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditorFiscal.Persistence.Configurations;

public class LogInternoConfiguration : IEntityTypeConfiguration<LogInterno>
{
    public void Configure(EntityTypeBuilder<LogInterno> builder)
    {
        builder.ToTable("LogsInternos");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Acao).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Detalhes).HasMaxLength(1000);

        builder.HasIndex(x => x.OcorridoEm);
    }
}
