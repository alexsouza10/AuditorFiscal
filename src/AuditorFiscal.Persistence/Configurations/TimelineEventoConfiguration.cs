using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditorFiscal.Persistence.Configurations;

public class TimelineEventoConfiguration : IEntityTypeConfiguration<TimelineEvento>
{
    public void Configure(EntityTypeBuilder<TimelineEvento> builder)
    {
        builder.ToTable("TimelineEventos");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Descricao).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.OrdemServicoId);
    }
}
