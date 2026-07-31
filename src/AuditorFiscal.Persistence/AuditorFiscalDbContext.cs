using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditorFiscal.Persistence;

public class AuditorFiscalDbContext(DbContextOptions<AuditorFiscalDbContext> options) : DbContext(options)
{
    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<TipoAuditoria> TiposAuditoria => Set<TipoAuditoria>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<LogInterno> Logs => Set<LogInterno>();
    public DbSet<BackupRegistro> Backups => Set<BackupRegistro>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Foto e Anexo compartilham campos via herança em C# (DRY), mas são conceitos
        // independentes sem relação alguma entre si — TPC gera uma tabela própria e
        // completa para cada um, sem tabela base nem discriminador.
        modelBuilder.Entity<ArquivoArmazenado>().HasKey(x => x.Id);
        modelBuilder.Entity<ArquivoArmazenado>().UseTpcMappingStrategy();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditorFiscalDbContext).Assembly);

        ConfigurarChavesGeradasPeloDominio(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Os Ids são gerados no construtor das entidades, não pelo banco. Sem declarar isso,
    /// o EF assume que um Guid já preenchido significa "linha existente" e marca filhos
    /// novos (ex.: eventos de timeline) como Modified em vez de Added, gerando UPDATEs
    /// que não afetam nenhuma linha.
    /// </summary>
    private static void ConfigurarChavesGeradasPeloDominio(ModelBuilder modelBuilder)
    {
        foreach (var tipoEntidade in modelBuilder.Model.GetEntityTypes())
        {
            var chave = tipoEntidade.FindPrimaryKey();
            if (chave is null)
                continue;

            foreach (var propriedade in chave.Properties.Where(p => p.ClrType == typeof(Guid)))
                propriedade.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
        }
    }
}
