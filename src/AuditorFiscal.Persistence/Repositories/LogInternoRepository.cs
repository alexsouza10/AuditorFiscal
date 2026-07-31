using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditorFiscal.Persistence.Repositories;

public class LogInternoRepository(AuditorFiscalDbContext contexto)
    : Repository<LogInterno>(contexto), ILogInternoRepository
{
    public async Task<IReadOnlyList<LogInterno>> ListarRecentesAsync(int quantidade, CancellationToken ct = default) =>
        await DbSet.OrderByDescending(x => x.OcorridoEm).Take(quantidade).ToListAsync(ct);
}
