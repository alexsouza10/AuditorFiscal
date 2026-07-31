using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Persistence.Repositories;

namespace AuditorFiscal.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AuditorFiscalDbContext _contexto;

    public IOrdemServicoRepository OrdensServico { get; }
    public ITipoAuditoriaRepository TiposAuditoria { get; }
    public ITagRepository Tags { get; }
    public ILogInternoRepository Logs { get; }
    public IBackupRegistroRepository Backups { get; }

    public UnitOfWork(AuditorFiscalDbContext contexto)
    {
        _contexto = contexto;
        OrdensServico = new OrdemServicoRepository(contexto);
        TiposAuditoria = new TipoAuditoriaRepository(contexto);
        Tags = new TagRepository(contexto);
        Logs = new LogInternoRepository(contexto);
        Backups = new BackupRegistroRepository(contexto);
    }

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) =>
        _contexto.SaveChangesAsync(ct);
}
