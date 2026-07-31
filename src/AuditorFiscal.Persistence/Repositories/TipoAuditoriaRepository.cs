using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditorFiscal.Persistence.Repositories;

public class TipoAuditoriaRepository(AuditorFiscalDbContext contexto)
    : Repository<TipoAuditoria>(contexto), ITipoAuditoriaRepository
{
    public async Task<IReadOnlyList<TipoAuditoria>> ListarAtivosAsync(CancellationToken ct = default) =>
        await DbSet.Where(x => x.Ativo).OrderBy(x => x.Nome).ToListAsync(ct);
}
