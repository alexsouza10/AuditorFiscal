using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditorFiscal.Persistence.Configurations;

public class TipoAuditoriaConfiguration : IEntityTypeConfiguration<TipoAuditoria>
{
    public void Configure(EntityTypeBuilder<TipoAuditoria> builder)
    {
        builder.ToTable("TiposAuditoria");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(100).IsRequired();

        builder.HasData(
            new { Id = TipoAuditoriaSeed.AuditoriaFiscal, Nome = "Auditoria Fiscal", Ativo = true, CriadoEm = TipoAuditoriaSeed.MomentoSeed, AtualizadoEm = TipoAuditoriaSeed.MomentoSeed },
            new { Id = TipoAuditoriaSeed.DiligenciaFiscal, Nome = "Diligência Fiscal", Ativo = true, CriadoEm = TipoAuditoriaSeed.MomentoSeed, AtualizadoEm = TipoAuditoriaSeed.MomentoSeed },
            new { Id = TipoAuditoriaSeed.FiscalizacaoIcms, Nome = "Fiscalização de ICMS", Ativo = true, CriadoEm = TipoAuditoriaSeed.MomentoSeed, AtualizadoEm = TipoAuditoriaSeed.MomentoSeed },
            new { Id = TipoAuditoriaSeed.FiscalizacaoIss, Nome = "Fiscalização de ISS", Ativo = true, CriadoEm = TipoAuditoriaSeed.MomentoSeed, AtualizadoEm = TipoAuditoriaSeed.MomentoSeed },
            new { Id = TipoAuditoriaSeed.AuditoriaContabilFiscal, Nome = "Auditoria Contábil-Fiscal", Ativo = true, CriadoEm = TipoAuditoriaSeed.MomentoSeed, AtualizadoEm = TipoAuditoriaSeed.MomentoSeed },
            new { Id = TipoAuditoriaSeed.PlantaoFiscal, Nome = "Plantão Fiscal", Ativo = true, CriadoEm = TipoAuditoriaSeed.MomentoSeed, AtualizadoEm = TipoAuditoriaSeed.MomentoSeed });
    }
}

public static class TipoAuditoriaSeed
{
    public static readonly Guid AuditoriaFiscal = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DiligenciaFiscal = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid FiscalizacaoIcms = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid FiscalizacaoIss = Guid.Parse("00000000-0000-0000-0000-000000000004");
    public static readonly Guid AuditoriaContabilFiscal = Guid.Parse("00000000-0000-0000-0000-000000000005");
    public static readonly Guid PlantaoFiscal = Guid.Parse("00000000-0000-0000-0000-000000000006");

    public static readonly DateTimeOffset MomentoSeed = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
