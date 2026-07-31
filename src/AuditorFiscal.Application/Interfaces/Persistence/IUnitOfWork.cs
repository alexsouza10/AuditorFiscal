namespace AuditorFiscal.Application.Interfaces.Persistence;

public interface IUnitOfWork
{
    IOrdemServicoRepository OrdensServico { get; }
    ITipoAuditoriaRepository TiposAuditoria { get; }
    ITagRepository Tags { get; }
    ILogInternoRepository Logs { get; }
    IBackupRegistroRepository Backups { get; }

    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}
