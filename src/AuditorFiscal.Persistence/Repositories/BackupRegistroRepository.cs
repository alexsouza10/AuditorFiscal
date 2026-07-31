using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditorFiscal.Persistence.Repositories;

public class BackupRegistroRepository(AuditorFiscalDbContext contexto)
    : Repository<BackupRegistro>(contexto), IBackupRegistroRepository
{
    public async Task<BackupRegistro?> ObterMaisRecenteAsync(CancellationToken ct = default) =>
        await DbSet.OrderByDescending(x => x.CriadoEm).FirstOrDefaultAsync(ct);
}
