using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Persistence;

public interface IBackupRegistroRepository : IRepository<BackupRegistro>
{
    Task<BackupRegistro?> ObterMaisRecenteAsync(CancellationToken ct = default);
}
